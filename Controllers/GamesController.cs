using GamPI.Data;
using GamPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GamPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly ADC _context;
        private readonly HttpClient _http = new HttpClient();
        private readonly IDistributedCache _cache;

        private static readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        public GamesController(ADC context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet("cache-test")]
        public async Task<IActionResult> CacheTest()
        {
            await _cache.SetStringAsync("test-key", "Redis is working!", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });

            var value = await _cache.GetStringAsync("test-key");
            return Ok(new { cached = value });
        }

        [HttpGet("search/{name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            string cacheKey = $"search:{name.ToLowerInvariant()}";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return Ok(JsonSerializer.Deserialize<List<Game>>(cached));

            var cachedGames = _context.Games
                .Where(g => g.Title.Contains(name))
                .ToList();

            if (cachedGames.Any())
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cachedGames), _cacheOptions);
                return Ok(cachedGames);
            }

            var regex = new Regex(name, RegexOptions.IgnoreCase);
            List<Game> results = new List<Game>();

            var steamJson = await _http.GetStringAsync(
                "https://api.steampowered.com/IStoreService/GetAppList/v1/?key=2F12D5CFE014F7E8A9FBE732B8751A8A"
            );

            using (var doc = JsonDocument.Parse(steamJson))
            {
                var apps = doc.RootElement
                    .GetProperty("response")
                    .GetProperty("apps");

                foreach (var app in apps.EnumerateArray())
                {
                    var appid = app.GetProperty("appid").GetInt32();
                    var title = app.GetProperty("name").GetString();

                    if (string.IsNullOrWhiteSpace(title) || !regex.IsMatch(title))
                        continue;

                    string appCacheKey = $"steam:app:{appid}";
                    Game game;

                    var cachedApp = await _cache.GetStringAsync(appCacheKey);
                    if (cachedApp != null)
                    {
                        game = JsonSerializer.Deserialize<Game>(cachedApp)!;
                    }
                    else
                    {
                        var detailsJson = await _http.GetStringAsync(
                            $"https://store.steampowered.com/api/appdetails?appids={appid}"
                        );

                        using var detailsDoc = JsonDocument.Parse(detailsJson);
                        var root = detailsDoc.RootElement.GetProperty(appid.ToString());

                        if (!root.GetProperty("success").GetBoolean())
                            continue;

                        var data = root.GetProperty("data");

                        string description = data.TryGetProperty("short_description", out var desc)
                            ? desc.GetString() : "";

                        string developer = "Unknown";
                        if (data.TryGetProperty("developers", out var devs))
                            developer = devs[0].GetString();

                        string genre = "Unknown";
                        if (data.TryGetProperty("genres", out var genres))
                            genre = genres[0].GetProperty("description").GetString();

                        string img = null;
                        if (data.TryGetProperty("header_image", out var image))
                            img = image.GetString();

                        game = new Game
                        {
                            Title = title,
                            Platforms = "Steam",
                            Genre = genre,
                            Description = description,
                            Developer = developer,
                            ImgURL = img,
                            Rating = 0,
                            ReleaseDate = DateTime.Now
                        };

                        await _cache.SetStringAsync(appCacheKey, JsonSerializer.Serialize(game), _cacheOptions);
                    }

                    results.Add(game);

                    if (results.Count >= 10)
                        break;
                }
            }

            if (!results.Any())
                return NotFound("No such game found.");

            foreach (var game in results)
            {
                if (!_context.Games.Any(g => g.Title == game.Title))
                    _context.Games.Add(game);
            }

            await _context.SaveChangesAsync();
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(results), _cacheOptions);

            return Ok(results);
        }

        [HttpGet("id/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            string cacheKey = $"game:id:{id}";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return Ok(JsonSerializer.Deserialize<Game>(cached));

            var game = await _context.Games.FindAsync(id);

            if (game == null)
                return NotFound();

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(game), _cacheOptions);

            return Ok(game);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1)
        {
            const int pageSize = 20;

            if (page < 1)
                return BadRequest(new { message = "Page number must be 1 or greater." });

            string cacheKey = $"games:all:page:{page}";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return Ok(JsonSerializer.Deserialize<object>(cached));

            var query = _context.Games.OrderBy(g => g.Id);
            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalCount == 0)
                return NotFound(new { message = "No games found in database." });

            if (page > totalPages)
                return BadRequest(new { message = $"Page {page} exceeds total pages ({totalPages})." });

            var games = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new { page, pageSize, totalCount, totalPages, data = games };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                }
            );

            return Ok(result);
        }

        [HttpPost("add/{gameId}")]
        [Authorize] 
        public async Task<IActionResult> AddFavourite(int gameId)
        {
            var username = User.Identity?.Name;
            if (username == null)
                return Unauthorized();
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return NotFound("User not found.");
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                return NotFound("Game not found.");
            if (user.FavoriteGames.Any(g => g.Id == gameId))
                return BadRequest("Game already in favorites.");
            user.FavoriteGames.Add(game);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Game added to favorites." });
        }
    }
}
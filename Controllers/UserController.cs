using GamPI.Data;
using GamPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GamPI.Controllers
{
    [ApiController]
    [Route("/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ADC _context;

        public UserController(ADC context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var username = User.Identity?.Name;

            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Favourites = user.FavoriteGames
            });
        }
    }

    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ADC _context;

        public AuthController(ADC context)
        {
            _context = context;
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("super_secret_jwt_key_for_gampi_api_2026_12345")
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "gampi-api",
                audience: "gampi-api",
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/register")]
        public IActionResult Create([FromBody] RegisterUserDto newUser)
        {
            if (string.IsNullOrEmpty(newUser.Username) || string.IsNullOrEmpty(newUser.Password))
            {
                return BadRequest("Invalid Input");
            }

            if (_context.Users.Any(u => u.Username == newUser.Username))
                return Conflict("Username already exists");

            if (_context.Users.Any(u => u.Email == newUser.Email))
                return Conflict("Email already exists");

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newUser.Password);

            var user = new User
            {
                Username = newUser.Username,
                Email = newUser.Email,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new User
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("/login")]
        public IActionResult Login([FromBody] LoginUserDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound("Email not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Wrong Password");
            }

            var token = GenerateJwtToken(user);
            if (token == null) {
                return StatusCode(500, "Token generation failed");
            }
            return Ok(new { token });
        }
    }
}
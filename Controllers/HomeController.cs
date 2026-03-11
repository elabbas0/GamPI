using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.Identity?.Name;
        if (userId == null)
        {
            return Unauthorized("Welcome to GamPI, please log in.");
        }

        return Ok($"Welcome {username}");
    }

}
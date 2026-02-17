using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OR.InventoryService.Api.Models;
using OR.Shared.Auth;

namespace OR.InventoryService.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(JwtSettings jwtSettings) : ControllerBase
{
    private readonly JwtSettings _jwtSettings = jwtSettings;

    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult CreateToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Username and password are required." });

        var token = JwtTokenGenerator.GenerateToken(_jwtSettings, request.Username, request.Roles ?? []);

        return Ok(new { token });
    }
}

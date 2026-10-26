using AuthHub.Api.DTOs;
using AuthHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthHub.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RegistrationService _registrationService;
        private readonly TokenService _tokenService;
        public AuthController(RegistrationService registrationService, TokenService tokenService)
        {
            _registrationService = registrationService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequestDto request)
        {
            try
            {
                var user = await _registrationService.RegisterAsync(request);
                var response = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.IsEmailVerified,
                    user.CreatedAtUtc
                };
                return StatusCode(201, response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _registrationService.LoginAsync(request);
            if (user is null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }
            var accessToken = _tokenService.GenerateAccessToken(user);
            var response = new
            {
                accessToken,
                tokenType = "Bearer",
                expiresIn = 900, // 15 minutes in seconds
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.IsEmailVerified,
                    user.CreatedAtUtc
                }
            };
         
            return Ok(response);
        }
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                message = "You are authenticated.",
                userId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value,
                fullName = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name)?.Value,
                email = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value,
                role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            });
        }

    }
}

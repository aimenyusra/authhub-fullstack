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
        private readonly RefreshTokenService _refreshTokenService;
        public AuthController(RegistrationService registrationService, TokenService tokenService, RefreshTokenService refreshTokenService)
        {
            _registrationService = registrationService;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
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
            Guid familyId = Guid.NewGuid();
            var createdRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, familyId);
            var response = new
            {
                accessToken,
                refreshToken= createdRefreshToken.RawToken,
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

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequestDto request)
        {
            var result = await _refreshTokenService.RotateRefreshTokenAsync(request.RefreshToken);
            if(!result.Succes || result.User is null)
            {
                return Unauthorized(new
                {
                    message = "Invalid or revoked refresh token.Please log in again"
                });

            }
            var newAccessToken = _tokenService.GenerateAccessToken(result.User);
            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = result.RawToken,
                tokenType = "Bearer",
                expiresIn = 900, // 15 minutes in seconds
            });
        }
    }
}

using AuthHub.Api.DTOs;
using AuthHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthHub.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RegistrationService _registrationService;
        public AuthController(RegistrationService registrationService)
        {
            _registrationService = registrationService;
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
    }
}

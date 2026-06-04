using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TeamConnect.Api.Shared.DTOs;

namespace TeamConnect.Api.Modules.Auth
{
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        private readonly JwtService _jwt;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService auth, JwtService jwt, ILogger<AuthController> logger)
        {
            _auth = auth;
            _jwt = jwt;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = await _auth.Register(dto);
            if (user == null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "User already exists",
                    Detail = "An account with that email is already registered."
                });
            }

            return Ok(new { token = _jwt.Generate(user) });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _auth.Login(dto);
                if (user == null) return Unauthorized();

                return Ok(new { token = _jwt.Generate(user) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", dto?.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, "Login failed");
            }
        }
    }
}

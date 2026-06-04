using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;

namespace TeamConnect.Api.Modules.Auth
{
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        private readonly JwtService _jwt;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IWebHostEnvironment _env;

        public AuthController(AuthService auth, JwtService jwt, ILogger<AuthController> logger, IUserRepository userRepository, IWebHostEnvironment env)
        {
            _auth = auth;
            _jwt = jwt;
            _logger = logger;
            _userRepository = userRepository;
            _env = env;
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

        // Dev-only endpoint to promote a user to Admin role for e2e test setup
        [HttpPost("dev/promote-admin")]
        public async Task<IActionResult> DevPromoteAdmin([FromBody] DevPromoteAdminDto dto)
        {
            if (!_env.IsDevelopment())
                return NotFound();

            var user = await _userRepository.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound(new { message = $"User with email '{dto.Email}' not found." });

            await _userRepository.UpdateRoleAsync(user.Id, UserRoles.Admin);
            user.Role = UserRoles.Admin;
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

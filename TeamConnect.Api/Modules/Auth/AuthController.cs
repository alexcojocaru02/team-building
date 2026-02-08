using Microsoft.AspNetCore.Mvc;
using TeamConnect.Api.Shared.DTOs;

namespace TeamConnect.Api.Modules.Auth
{
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        private readonly JwtService _jwt;

        public AuthController(AuthService auth, JwtService jwt)
        {
            _auth = auth;
            _jwt = jwt;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = await _auth.Register(dto);
            if (user == null) return BadRequest("User exists");

            return Ok(new { token = _jwt.Generate(user) });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _auth.Login(dto);
            if (user == null) return Unauthorized();

            return Ok(new { token = _jwt.Generate(user) });
        }
    }
}

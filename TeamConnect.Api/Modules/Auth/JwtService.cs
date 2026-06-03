using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.Auth
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string Generate(User user)
        {
            var keyText = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(keyText))
            {
                _logger.LogError("Jwt:Key is missing or empty");
                throw new InvalidOperationException("JWT signing key is not configured.");
            }

            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            {
                _logger.LogError("Jwt:Issuer or Jwt:Audience is missing");
                throw new InvalidOperationException("JWT issuer/audience is not configured.");
            }

            var normalizedRole = UserRoles.Normalize(user.Role);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, normalizedRole)
        };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(keyText)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

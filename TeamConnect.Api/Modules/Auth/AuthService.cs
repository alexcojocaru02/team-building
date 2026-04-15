using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Auth
{
    public class AuthService
    {
        private readonly MongoDbContext _context;

        public AuthService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<User> Register(RegisterDto dto)
        {
            var existing = await _context.Users
                .Find(u => u.Email == dto.Email)
                .FirstOrDefaultAsync();

            if (existing != null) return null;

            var user = new User
            {
                FullName = string.IsNullOrWhiteSpace(dto.FullName)
                    ? dto.Email
                    : dto.FullName.Trim(),
                Email = dto.Email,
                PasswordHash = Hash(dto.Password),
                Role = "User"
            };

            await _context.Users.InsertOneAsync(user);
            return user;
        }

        public async Task<User> Login(LoginDto dto)
        {
            var hash = Hash(dto.Password);
            return await _context.Users
                .Find(u => u.Email == dto.Email && u.PasswordHash == hash)
                .FirstOrDefaultAsync();
        }

        private string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}

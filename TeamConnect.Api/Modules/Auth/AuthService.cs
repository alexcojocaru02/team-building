using MongoDB.Driver;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Auth
{
    public class AuthService
    {
        private readonly MongoDbContext _context;
        private readonly INotificationService? _notificationService;

        public AuthService(MongoDbContext context, INotificationService? notificationService = null)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<User?> Register(RegisterDto dto)
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRoles.User
            };

            await _context.Users.InsertOneAsync(user);

            if (_notificationService != null)
            {
                await _notificationService.SendWelcomeEmailAsync(user.Id);
            }

            return user;
        }

        public async Task<User?> Login(LoginDto dto)
        {
            var user = await _context.Users
                .Find(u => u.Email == dto.Email)
                .FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            return user;
        }
    }
}

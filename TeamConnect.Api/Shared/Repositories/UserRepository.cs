using MongoDB.Driver;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Shared.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MongoDbContext _context;

        public UserRepository(MongoDbContext context)
        {
            _context = context;
        }

        public Task<User?> FindByIdAsync(string id) =>
            _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync()!;

        public Task<User?> FindByEmailAsync(string email) =>
            _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync()!;

        public async Task<List<UserSummary>> FindSummariesByIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            if (idList.Count == 0) return new List<UserSummary>();

            var users = await _context.Users
                .Find(u => idList.Contains(u.Id))
                .Project(u => new { u.Id, u.Email, u.FullName })
                .ToListAsync();

            return users.Select(u => new UserSummary(u.Id, u.FullName, u.Email ?? "Unknown")).ToList();
        }

        public async Task<List<UserListItem>> GetAllAsync()
        {
            var users = await _context.Users
                .Find(_ => true)
                .Project(u => new { u.Id, u.FullName, u.Email, u.Role })
                .ToListAsync();

            return users.Select(u => new UserListItem(u.Id, u.FullName, u.Email, u.Role)).ToList();
        }

        public Task InsertAsync(User user) =>
            _context.Users.InsertOneAsync(user);

        public async Task UpdateProfileAsync(string id, User user)
        {
            var update = Builders<User>.Update
                .Set(u => u.Bio, user.Bio)
                .Set(u => u.AvatarUrl, user.AvatarUrl)
                .Set(u => u.Department, user.Department)
                .Set(u => u.Location, user.Location)
                .Set(u => u.Timezone, user.Timezone)
                .Set(u => u.Pronouns, user.Pronouns)
                .Set(u => u.PreferredWorkStyle, user.PreferredWorkStyle)
                .Set(u => u.Icebreaker, user.Icebreaker)
                .Set(u => u.Hobbies, user.Hobbies)
                .Set(u => u.Strengths, user.Strengths)
                .Set(u => u.UpdatedAt, user.UpdatedAt);

            await _context.Users.UpdateOneAsync(u => u.Id == id, update);
        }

        public Task AddToTeamAsync(string userId, string teamId)
        {
            var update = Builders<User>.Update
                .AddToSet(u => u.TeamIds, teamId)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);
            return _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public Task RemoveFromTeamAsync(string userId, string teamId)
        {
            var update = Builders<User>.Update
                .Pull(u => u.TeamIds, teamId)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);
            return _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public Task RemoveTeamFromAllUsersAsync(string teamId)
        {
            var update = Builders<User>.Update.Pull("TeamIds", teamId);
            return _context.Users.UpdateManyAsync(
                Builders<User>.Filter.AnyEq(u => u.TeamIds, teamId),
                update);
        }

        public Task<bool> ExistsAsync(string id) =>
            _context.Users.Find(u => u.Id == id).AnyAsync();

        public Task DeleteAsync(string id) =>
            _context.Users.DeleteOneAsync(u => u.Id == id);

        public Task UpdateRoleAsync(string id, string role)
        {
            var update = Builders<User>.Update.Set(u => u.Role, role);
            return _context.Users.UpdateOneAsync(u => u.Id == id, update);
        }
    }
}

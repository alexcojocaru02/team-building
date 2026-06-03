using MongoDB.Driver;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Teams
{
    public class TeamRepository : ITeamRepository
    {
        private readonly MongoDbContext _context;

        public TeamRepository(MongoDbContext context)
        {
            _context = context;
        }

        public Task<Team?> FindByIdAsync(string id) =>
            _context.Teams.Find(t => t.Id == id).FirstOrDefaultAsync()!;

        public Task<List<Team>> GetAllAsync() =>
            _context.Teams.Find(_ => true).ToListAsync();

        public Task InsertAsync(Team team) =>
            _context.Teams.InsertOneAsync(team);

        public Task UpdateAsync(string id, string name, string? description)
        {
            var update = Builders<Team>.Update
                .Set(t => t.Name, name)
                .Set(t => t.Description, description)
                .Set(t => t.UpdatedAt, DateTime.UtcNow);
            return _context.Teams.UpdateOneAsync(t => t.Id == id, update);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _context.Teams.DeleteOneAsync(t => t.Id == id);
            return result.DeletedCount > 0;
        }

        public Task AddMemberAsync(string teamId, string userId)
        {
            var update = Builders<Team>.Update
                .AddToSet(t => t.MemberIds, userId)
                .Set(t => t.UpdatedAt, DateTime.UtcNow);
            return _context.Teams.UpdateOneAsync(t => t.Id == teamId, update);
        }

        public Task RemoveMemberAsync(string teamId, string userId)
        {
            var update = Builders<Team>.Update
                .Pull(t => t.MemberIds, userId)
                .Set(t => t.UpdatedAt, DateTime.UtcNow);
            return _context.Teams.UpdateOneAsync(t => t.Id == teamId, update);
        }
    }
}

using MongoDB.Driver;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Teams
{
    public class TeamJoinRequestRepository : ITeamJoinRequestRepository
    {
        private readonly MongoDbContext _context;

        public TeamJoinRequestRepository(MongoDbContext context)
        {
            _context = context;
        }

        public Task<TeamJoinRequest?> FindByIdAsync(string id) =>
            _context.TeamJoinRequests.Find(r => r.Id == id).FirstOrDefaultAsync()!;

        public Task<TeamJoinRequest?> FindPendingByUserAndTeamAsync(string userId, string teamId) =>
            _context.TeamJoinRequests
                .Find(r => r.UserId == userId && r.TeamId == teamId && r.Status == JoinRequestStatus.Pending)
                .FirstOrDefaultAsync()!;

        public Task<List<TeamJoinRequest>> GetPendingForTeamAsync(string teamId) =>
            _context.TeamJoinRequests
                .Find(r => r.TeamId == teamId && r.Status == JoinRequestStatus.Pending)
                .ToListAsync();

        public Task<List<TeamJoinRequest>> GetAllPendingAsync() =>
            _context.TeamJoinRequests
                .Find(r => r.Status == JoinRequestStatus.Pending)
                .ToListAsync();

        public Task InsertAsync(TeamJoinRequest request) =>
            _context.TeamJoinRequests.InsertOneAsync(request);

        public Task UpdateStatusAsync(string id, string status)
        {
            var update = Builders<TeamJoinRequest>.Update.Set(r => r.Status, status);
            return _context.TeamJoinRequests.UpdateOneAsync(r => r.Id == id, update);
        }
    }
}

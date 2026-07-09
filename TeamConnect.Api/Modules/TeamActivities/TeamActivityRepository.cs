using MongoDB.Driver;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.TeamActivities
{
    public class TeamActivityRepository : ITeamActivityRepository
    {
        private readonly MongoDbContext _context;

        public TeamActivityRepository(MongoDbContext context)
        {
            _context = context;
        }

        public Task<List<TeamActivity>> GetByTeamIdAsync(string teamId) =>
            _context.TeamActivities
                .Find(a => a.TeamId == teamId)
                .SortByDescending(a => a.CreatedAt)
                .ToListAsync();

        public Task<TeamActivity?> FindByIdAndTeamIdAsync(string activityId, string teamId) =>
            _context.TeamActivities
                .Find(a => a.Id == activityId && a.TeamId == teamId)
                .FirstOrDefaultAsync()!;

        public Task InsertAsync(TeamActivity activity) =>
            _context.TeamActivities.InsertOneAsync(activity);

        public Task<TeamActivity?> SubmitResponseAsync(FilterDefinition<TeamActivity> filter, UpdateDefinition<TeamActivity> update) =>
            _context.TeamActivities.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<TeamActivity> { ReturnDocument = ReturnDocument.After })!;

        public Task CompleteAsync(TeamActivity activity) =>
            _context.TeamActivities.ReplaceOneAsync(a => a.Id == activity.Id, activity);

        public Task DeleteByTeamIdAsync(string teamId) =>
            _context.TeamActivities.DeleteManyAsync(a => a.TeamId == teamId);
    }
}

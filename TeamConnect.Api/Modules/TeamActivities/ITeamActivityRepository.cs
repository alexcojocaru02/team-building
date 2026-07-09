using MongoDB.Driver;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.TeamActivities
{
    public interface ITeamActivityRepository
    {
        Task<List<TeamActivity>> GetByTeamIdAsync(string teamId);
        Task<TeamActivity?> FindByIdAndTeamIdAsync(string activityId, string teamId);
        Task InsertAsync(TeamActivity activity);
        Task<TeamActivity?> SubmitResponseAsync(FilterDefinition<TeamActivity> filter, UpdateDefinition<TeamActivity> update);
        Task CompleteAsync(TeamActivity activity);
        Task DeleteByTeamIdAsync(string teamId);
    }
}

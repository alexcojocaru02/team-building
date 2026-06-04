using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.Teams
{
    public interface ITeamJoinRequestRepository
    {
        Task<TeamJoinRequest?> FindByIdAsync(string id);
        Task<TeamJoinRequest?> FindPendingByUserAndTeamAsync(string userId, string teamId);
        Task<List<TeamJoinRequest>> GetPendingForTeamAsync(string teamId);
        Task<List<TeamJoinRequest>> GetAllPendingAsync();
        Task InsertAsync(TeamJoinRequest request);
        Task UpdateStatusAsync(string id, string status);
    }
}

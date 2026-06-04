using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.Teams
{
    public interface ITeamRepository
    {
        Task<Team?> FindByIdAsync(string id);
        Task<List<Team>> GetAllAsync();
        Task InsertAsync(Team team);
        Task UpdateAsync(string id, string name, string? description);
        Task<bool> DeleteAsync(string id);
        Task AddMemberAsync(string teamId, string userId);
        Task RemoveMemberAsync(string teamId, string userId);
    }
}

using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Shared.Repositories
{
    public interface IUserRepository
    {
        Task<User?> FindByIdAsync(string id);
        Task<User?> FindByEmailAsync(string email);
        Task<List<UserSummary>> FindSummariesByIdsAsync(IEnumerable<string> ids);
        Task<List<UserListItem>> GetAllAsync();
        Task InsertAsync(User user);
        Task UpdateProfileAsync(string id, User user);
        Task AddToTeamAsync(string userId, string teamId);
        Task RemoveFromTeamAsync(string userId, string teamId);
        Task RemoveTeamFromAllUsersAsync(string teamId);
        Task<bool> ExistsAsync(string id);
    }

    public sealed record UserSummary(string Id, string? FullName, string Email);
    public sealed record UserListItem(string Id, string? FullName, string? Email, string Role);
}

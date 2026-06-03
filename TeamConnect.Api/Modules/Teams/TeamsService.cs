using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Teams
{
    public class TeamsService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService? _notificationService;

        public TeamsService(ITeamRepository teamRepository, IUserRepository userRepository, INotificationService? notificationService = null)
        {
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        public async Task<TeamDetailDto> Create(CreateTeamDto dto, string ownerId)
        {
            var team = new Team
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                MemberIds = new List<string> { ownerId }
            };

            await _teamRepository.InsertAsync(team);
            await _userRepository.AddToTeamAsync(ownerId, team.Id);

            return MapToDetailDto(team);
        }

        public async Task<AddUserResult> AddUser(string teamId, string userId, string requesterId)
        {
            var team = await _teamRepository.FindByIdAsync(teamId);
            if (team == null) return AddUserResult.TeamNotFound;

            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists) return AddUserResult.UserNotFound;

            if (team.MemberIds.Contains(userId))
            {
                await _userRepository.AddToTeamAsync(userId, teamId);
                return AddUserResult.Ok;
            }

            await _teamRepository.AddMemberAsync(teamId, userId);
            await _userRepository.AddToTeamAsync(userId, teamId);

            if (_notificationService != null)
                await _notificationService.SendTeamMemberAddedEmailAsync(teamId, userId, requesterId);

            return AddUserResult.Ok;
        }

        public async Task<RemoveUserResult> RemoveUser(string teamId, string userId)
        {
            var team = await _teamRepository.FindByIdAsync(teamId);
            if (team == null) return RemoveUserResult.TeamNotFound;

            var userExists = await _userRepository.ExistsAsync(userId);

            if (team.MemberIds.Contains(userId))
            {
                await _teamRepository.RemoveMemberAsync(teamId, userId);
                if (userExists)
                    await _userRepository.RemoveFromTeamAsync(userId, teamId);
                return RemoveUserResult.Ok;
            }

            if (userExists)
                await _userRepository.RemoveFromTeamAsync(userId, teamId);

            return RemoveUserResult.Ok;
        }

        public async Task<List<TeamDetailDto>> GetAll()
        {
            var teams = await _teamRepository.GetAllAsync();
            return teams.Select(MapToDetailDto).ToList();
        }

        public async Task<TeamDetailDto?> GetById(string id)
        {
            var team = await _teamRepository.FindByIdAsync(id);
            return team == null ? null : MapToDetailDto(team);
        }

        public async Task<TeamDetailDto?> Update(string id, CreateTeamDto dto, string requesterId, string requesterRole)
        {
            var team = await _teamRepository.FindByIdAsync(id);
            if (team == null) return null;

            var isAdmin = requesterRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == requesterId;
            if (!isAdmin && !isTeamOwner) throw new UnauthorizedAccessException();

            await _teamRepository.UpdateAsync(id, dto.Name, dto.Description);

            var updated = await _teamRepository.FindByIdAsync(id);
            return MapToDetailDto(updated!);
        }

        public async Task<bool> Delete(string id)
        {
            var deleted = await _teamRepository.DeleteAsync(id);
            if (!deleted) return false;

            await _userRepository.RemoveTeamFromAllUsersAsync(id);
            return true;
        }

        public static TeamDetailDto MapToDetailDto(Team team) => new()
        {
            Id = team.Id,
            Name = team.Name,
            OwnerId = team.OwnerId,
            Description = team.Description,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt,
            MemberIds = team.MemberIds
        };
    }

    public enum AddUserResult { Ok, TeamNotFound, UserNotFound }
    public enum RemoveUserResult { Ok, TeamNotFound }
}

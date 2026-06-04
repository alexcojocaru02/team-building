using TeamConnect.Api.Modules.Auth;
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
        private readonly ITeamJoinRequestRepository? _joinRequestRepository;
        private readonly JwtService? _jwtService;
        private readonly INotificationService? _notificationService;

        public TeamsService(
            ITeamRepository teamRepository,
            IUserRepository userRepository,
            ITeamJoinRequestRepository? joinRequestRepository = null,
            JwtService? jwtService = null,
            INotificationService? notificationService = null)
        {
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _joinRequestRepository = joinRequestRepository;
            _jwtService = jwtService;
            _notificationService = notificationService;
        }

        public async Task<CreateTeamResponseDto> Create(CreateTeamDto dto, string ownerId)
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

            // Upgrade user to TeamOwner if they were a plain User
            var owner = await _userRepository.FindByIdAsync(ownerId);
            string? newToken = null;
            if (owner != null && owner.Role == UserRoles.User)
            {
                await _userRepository.UpdateRoleAsync(ownerId, UserRoles.TeamOwner);
                owner.Role = UserRoles.TeamOwner;
                if (_jwtService != null)
                    newToken = _jwtService.Generate(owner);
            }

            return new CreateTeamResponseDto
            {
                Team = MapToDetailDto(team),
                NewToken = newToken
            };
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

        // Join request methods

        public async Task<RequestJoinResult> RequestJoin(string teamId, string userId)
        {
            if (_joinRequestRepository == null) throw new InvalidOperationException("Join request repository not configured.");
            var team = await _teamRepository.FindByIdAsync(teamId);
            if (team == null) return RequestJoinResult.TeamNotFound;

            if (team.MemberIds.Contains(userId))
                return RequestJoinResult.AlreadyMember;

            var existing = await _joinRequestRepository.FindPendingByUserAndTeamAsync(userId, teamId);
            if (existing != null)
                return RequestJoinResult.AlreadyRequested;

            await _joinRequestRepository.InsertAsync(new TeamJoinRequest
            {
                TeamId = teamId,
                UserId = userId,
                Status = JoinRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            return RequestJoinResult.Ok;
        }

        public async Task<List<TeamJoinRequestDto>> GetPendingRequestsForTeam(string teamId)
        {
            if (_joinRequestRepository == null) return new List<TeamJoinRequestDto>();
            var requests = await _joinRequestRepository.GetPendingForTeamAsync(teamId);
            return await MapRequestDtos(requests);
        }

        public async Task<List<TeamJoinRequestDto>> GetAllPendingRequests()
        {
            if (_joinRequestRepository == null) return new List<TeamJoinRequestDto>();
            var requests = await _joinRequestRepository.GetAllPendingAsync();
            return await MapRequestDtos(requests);
        }

        public async Task<ApproveRequestResult> ApproveRequest(string requestId, string approverId, string approverRole)
        {
            if (_joinRequestRepository == null) return ApproveRequestResult.NotFound;
            var request = await _joinRequestRepository.FindByIdAsync(requestId);
            if (request == null) return ApproveRequestResult.NotFound;

            var team = await _teamRepository.FindByIdAsync(request.TeamId);
            if (team == null) return ApproveRequestResult.NotFound;

            var isAdmin = approverRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == approverId;
            if (!isAdmin && !isTeamOwner) return ApproveRequestResult.Forbidden;

            await _joinRequestRepository.UpdateStatusAsync(requestId, JoinRequestStatus.Approved);
            await AddUser(request.TeamId, request.UserId, approverId);

            return ApproveRequestResult.Ok;
        }

        public async Task<ApproveRequestResult> RejectRequest(string requestId, string rejecterId, string rejecterRole)
        {
            if (_joinRequestRepository == null) return ApproveRequestResult.NotFound;
            var request = await _joinRequestRepository.FindByIdAsync(requestId);
            if (request == null) return ApproveRequestResult.NotFound;

            var team = await _teamRepository.FindByIdAsync(request.TeamId);
            if (team == null) return ApproveRequestResult.NotFound;

            var isAdmin = rejecterRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == rejecterId;
            if (!isAdmin && !isTeamOwner) return ApproveRequestResult.Forbidden;

            await _joinRequestRepository.UpdateStatusAsync(requestId, JoinRequestStatus.Rejected);
            return ApproveRequestResult.Ok;
        }

        private async Task<List<TeamJoinRequestDto>> MapRequestDtos(List<TeamJoinRequest> requests)
        {
            var result = new List<TeamJoinRequestDto>();
            foreach (var r in requests)
            {
                var team = await _teamRepository.FindByIdAsync(r.TeamId);
                var user = await _userRepository.FindByIdAsync(r.UserId);
                result.Add(new TeamJoinRequestDto
                {
                    Id = r.Id,
                    TeamId = r.TeamId,
                    TeamName = team?.Name ?? r.TeamId,
                    UserId = r.UserId,
                    UserFullName = user?.FullName ?? "",
                    UserEmail = user?.Email ?? "",
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                });
            }
            return result;
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
    public enum RequestJoinResult { Ok, TeamNotFound, AlreadyMember, AlreadyRequested }
    public enum ApproveRequestResult { Ok, NotFound, Forbidden }
}

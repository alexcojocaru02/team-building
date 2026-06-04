using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.Teams
{
    [ApiController]
    [Route("api/teams")]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly TeamsService _teamsService;

        public TeamsController(TeamsService teamsService)
        {
            _teamsService = teamsService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTeamDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Team name is required.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            return Ok(await _teamsService.Create(dto, currentUserId));
        }

        [HttpPost("{teamId}/add/{userId}")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> AddUser(string teamId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await _teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");

            var isAdmin = currentUserRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == currentUserId;
            if (!isAdmin && !isTeamOwner) return Forbid();

            return await _teamsService.AddUser(teamId, userId, currentUserId) switch
            {
                AddUserResult.TeamNotFound => NotFound("Team not found"),
                AddUserResult.UserNotFound => BadRequest("User not found"),
                _ => Ok(new { message = "User added to team" })
            };
        }

        [HttpDelete("{teamId}/members/{userId}")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> RemoveUser(string teamId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await _teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");

            var isAdmin = currentUserRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == currentUserId;
            if (!isAdmin && !isTeamOwner) return Forbid();

            return await _teamsService.RemoveUser(teamId, userId) switch
            {
                RemoveUserResult.TeamNotFound => NotFound("Team not found"),
                _ => Ok(new { message = "User removed from team" })
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _teamsService.GetAll());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var team = await _teamsService.GetById(id);
            return team == null ? NotFound() : Ok(team);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> Update(string id, CreateTeamDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            try
            {
                var updated = await _teamsService.Update(id, dto, currentUserId, currentUserRole ?? string.Empty);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _teamsService.Delete(id);
            return deleted ? Ok(new { message = "Team deleted" }) : NotFound();
        }

        // Join request endpoints

        [HttpPost("{teamId}/join-requests")]
        public async Task<IActionResult> RequestJoin(string teamId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            return await _teamsService.RequestJoin(teamId, currentUserId) switch
            {
                RequestJoinResult.TeamNotFound => NotFound("Team not found"),
                RequestJoinResult.AlreadyMember => BadRequest("You are already a member of this team"),
                RequestJoinResult.AlreadyRequested => BadRequest("You already have a pending request for this team"),
                _ => Ok(new { message = "Join request submitted" })
            };
        }

        [HttpGet("{teamId}/join-requests")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> GetTeamJoinRequests(string teamId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            var team = await _teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");

            var isAdmin = currentUserRole == UserRoles.Admin;
            var isTeamOwner = team.OwnerId == currentUserId;
            if (!isAdmin && !isTeamOwner) return Forbid();

            return Ok(await _teamsService.GetPendingRequestsForTeam(teamId));
        }

        [HttpGet("join-requests")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllJoinRequests() =>
            Ok(await _teamsService.GetAllPendingRequests());

        [HttpPut("join-requests/{requestId}/approve")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> ApproveJoinRequest(string requestId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            return await _teamsService.ApproveRequest(requestId, currentUserId, currentUserRole ?? string.Empty) switch
            {
                ApproveRequestResult.NotFound => NotFound("Request not found"),
                ApproveRequestResult.Forbidden => Forbid(),
                _ => Ok(new { message = "Request approved" })
            };
        }

        [HttpPut("join-requests/{requestId}/reject")]
        [Authorize(Policy = "AdminOrTeamOwner")]
        public async Task<IActionResult> RejectJoinRequest(string requestId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            return await _teamsService.RejectRequest(requestId, currentUserId, currentUserRole ?? string.Empty) switch
            {
                ApproveRequestResult.NotFound => NotFound("Request not found"),
                ApproveRequestResult.Forbidden => Forbid(),
                _ => Ok(new { message = "Request rejected" })
            };
        }
    }
}

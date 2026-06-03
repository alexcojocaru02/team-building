using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.TeamActivities
{
    [ApiController]
    [Route("api/teams/{teamId}/activities")]
    [Authorize]
    public class TeamActivitiesController : ControllerBase
    {
        private readonly TeamActivitiesService _activitiesService;

        public TeamActivitiesController(TeamActivitiesService activitiesService)
        {
            _activitiesService = activitiesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string teamId, [FromServices] Teams.TeamsService teamsService)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            var team = await teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");
            if (!CanAccessTeam(team, currentUserId, currentUserRole)) return Forbid();

            return Ok(await _activitiesService.GetAll(teamId, currentUserId));
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(string teamId, [FromServices] Teams.TeamsService teamsService)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            var team = await teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");
            if (!CanAccessTeam(team, currentUserId, currentUserRole)) return Forbid();

            return Ok(await _activitiesService.GetSummary(teamId, team.MemberIds));
        }

        [HttpPost]
        public async Task<IActionResult> Create(string teamId, CreateTeamActivityDto dto, [FromServices] Teams.TeamsService teamsService)
        {
            if (dto == null) return BadRequest("Activity payload is required.");
            if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(dto.Description)) return BadRequest("Description is required.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var team = await teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");
            if (!CanManageTeam(team, currentUserId, currentUserRole)) return Forbid();

            var options = NormalizeOptions(dto.Options);
            if (dto.ActivityType == ActivityType.Poll && options.Count < 2)
                return BadRequest("Poll activities require at least two options.");

            if (dto.DueAt.HasValue)
            {
                if (dto.DueAt.Value.Kind == DateTimeKind.Unspecified)
                    return BadRequest("DueAt must include a timezone and be provided in UTC (ISO 8601 with 'Z').");
                if ((dto.DueAt.Value.Kind == DateTimeKind.Utc ? dto.DueAt.Value : dto.DueAt.Value.ToUniversalTime()) <= DateTime.UtcNow)
                    return BadRequest("DueAt must be in the future.");
            }

            dto.Options = options;
            return Ok(await _activitiesService.Create(teamId, dto, currentUserId));
        }

        [HttpPost("{activityId}/responses")]
        public async Task<IActionResult> Respond(string teamId, string activityId, SubmitTeamActivityResponseDto dto, [FromServices] Teams.TeamsService teamsService)
        {
            if (dto == null) return BadRequest("Response payload is required.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var team = await teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");
            if (!CanAccessTeam(team, currentUserId, currentUserRole)) return Forbid();

            var activity = await _activitiesService.FindActivity(teamId, activityId);
            if (activity == null) return NotFound("Activity not found");

            if (string.Equals(activity.Status, "Closed", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Activity is closed.");

            if (activity.ActivityType == ActivityType.Poll)
            {
                if (!dto.SelectedOptionIndex.HasValue)
                    return BadRequest("A poll option must be selected.");
                if (dto.SelectedOptionIndex.Value < 0 || dto.SelectedOptionIndex.Value >= activity.Options.Count)
                    return BadRequest("Selected option is invalid.");
            }
            else
            {
                if (dto.SelectedOptionIndex.HasValue)
                    return BadRequest("SelectedOptionIndex is only valid for poll activities.");
                if (string.IsNullOrWhiteSpace(dto.TextResponse))
                    return BadRequest("A text response is required.");
            }

            var result = await _activitiesService.SubmitResponse(teamId, activityId, dto, currentUserId);
            return result == null ? BadRequest("Activity is closed.") : Ok(result);
        }

        [HttpPost("{activityId}/complete")]
        public async Task<IActionResult> Complete(string teamId, string activityId, [FromServices] Teams.TeamsService teamsService)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var team = await teamsService.GetById(teamId);
            if (team == null) return NotFound("Team not found");
            if (!CanManageTeam(team, currentUserId, currentUserRole)) return Forbid();

            var result = await _activitiesService.Complete(teamId, activityId);
            return result == null ? NotFound("Activity not found") : Ok(result);
        }

        private static bool CanAccessTeam(TeamDetailDto team, string? userId, string? role) =>
            IsAdmin(role) || (!string.IsNullOrWhiteSpace(userId) && (team.OwnerId == userId || team.MemberIds.Contains(userId)));

        private static bool CanManageTeam(TeamDetailDto team, string? userId, string? role) =>
            IsAdmin(role) || (!string.IsNullOrWhiteSpace(userId) && team.OwnerId == userId);

        private static bool IsAdmin(string? role) =>
            string.Equals(role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);

        private static List<string> NormalizeOptions(List<string> options) =>
            options.Select(o => o?.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o!).ToList();
    }
}

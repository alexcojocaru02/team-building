using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.TeamActivities
{
    [ApiController]
    [Route("api/teams/{teamId}/activities")]
    [Authorize]
    public class TeamActivitiesController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public TeamActivitiesController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string teamId)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            var team = await GetTeamAsync(teamId);
            if (team == null)
                return NotFound("Team not found");

            if (!CanAccessTeam(team, currentUserId, currentUserRole))
                return Forbid();

            var activities = await _context.TeamActivities
                .Find(activity => activity.TeamId == teamId)
                .SortByDescending(activity => activity.CreatedAt)
                .ToListAsync();

            if (activities.Count == 0)
                return Ok(Array.Empty<TeamActivityDto>());

            return Ok(await BuildDtosAsync(activities, currentUserId));
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(string teamId)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            var team = await GetTeamAsync(teamId);
            if (team == null)
                return NotFound("Team not found");

            if (!CanAccessTeam(team, currentUserId, currentUserRole))
                return Forbid();

            var activities = await _context.TeamActivities
                .Find(activity => activity.TeamId == teamId)
                .ToListAsync();

            var summary = BuildSummary(team, activities);
            return Ok(summary);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string teamId, CreateTeamActivityDto dto)
        {
            if (dto == null)
                return BadRequest("Activity payload is required.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title is required.");

            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Description is required.");

            // `ActivityType` is an enum on the DTO and is validated by [ApiController]/[Required].

            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await GetTeamAsync(teamId);
            if (team == null)
                return NotFound("Team not found");

            if (!CanManageTeamActivities(team, currentUserId, currentUserRole))
                return Forbid();

            var parsedType = dto.ActivityType;

            var options = NormalizeOptions(dto.Options);

            if (parsedType == Shared.Models.ActivityType.Poll && options.Count < 2)
                return BadRequest("Poll activities require at least two options.");

            DateTime? dueAtUtc = null;
            if (dto.DueAt.HasValue)
            {
                if (dto.DueAt.Value.Kind == DateTimeKind.Unspecified)
                    return BadRequest("DueAt must include a timezone and be provided in UTC (ISO 8601 with 'Z').");

                dueAtUtc = dto.DueAt.Value.Kind == DateTimeKind.Utc
                    ? dto.DueAt.Value
                    : dto.DueAt.Value.ToUniversalTime();

                if (dueAtUtc.Value <= DateTime.UtcNow)
                    return BadRequest("DueAt must be in the future.");
            }

            var activity = new TeamActivity
            {
                TeamId = teamId,
                CreatedByUserId = currentUserId,
                ActivityType = parsedType,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Options = options,
                DueAt = dueAtUtc,
                Points = dto.Points > 0 ? dto.Points : 10,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            await _context.TeamActivities.InsertOneAsync(activity);

            return Ok(await BuildDtoAsync(activity, currentUserId));
        }

        [HttpPost("{activityId}/responses")]
        public async Task<IActionResult> Respond(string teamId, string activityId, SubmitTeamActivityResponseDto dto)
        {
            if (dto == null)
                return BadRequest("Response payload is required.");

            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await GetTeamAsync(teamId);
            if (team == null)
                return NotFound("Team not found");

            if (!CanAccessTeam(team, currentUserId, currentUserRole))
                return Forbid();

            var activity = await GetActivityAsync(teamId, activityId);
            if (activity == null)
                return NotFound("Activity not found");

            if (string.Equals(activity.Status, "Closed", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Activity is closed.");

            var activityType = activity.ActivityType;

            if (activityType == Shared.Models.ActivityType.Poll)
            {
                if (!dto.SelectedOptionIndex.HasValue)
                    return BadRequest("A poll option must be selected.");

                if (dto.SelectedOptionIndex.Value < 0 || dto.SelectedOptionIndex.Value >= activity.Options.Count)
                    return BadRequest("Selected option is invalid.");
            }
            else if (string.IsNullOrWhiteSpace(dto.TextResponse))
            {
                return BadRequest("A text response is required.");
            }

            var participation = new TeamActivityParticipation
            {
                UserId = currentUserId,
                TextResponse = string.IsNullOrWhiteSpace(dto.TextResponse) ? null : dto.TextResponse.Trim(),
                SelectedOptionIndex = dto.SelectedOptionIndex,
                SubmittedAt = DateTime.UtcNow
            };

            var updateFilter = Builders<TeamActivity>.Filter.And(
                Builders<TeamActivity>.Filter.Eq(existingActivity => existingActivity.Id, activity.Id),
                Builders<TeamActivity>.Filter.Eq(existingActivity => existingActivity.TeamId, teamId),
                Builders<TeamActivity>.Filter.Ne(existingActivity => existingActivity.Status, "Closed"));

            BsonValue userIdFilterValue = ObjectId.TryParse(currentUserId, out var parsedUserObjectId)
                ? parsedUserObjectId
                : currentUserId;

            var filteredParticipations = new BsonDocument("$filter", new BsonDocument
            {
                { "input", "$Participations" },
                { "as", "existingParticipation" },
                { "cond", new BsonDocument("$ne", new BsonArray { "$$existingParticipation.UserId", userIdFilterValue }) }
            });

            var updateDefinition = Builders<TeamActivity>.Update.Pipeline(
                new[]
                {
                    new BsonDocument("$set", new BsonDocument("Participations",
                        new BsonDocument("$concatArrays", new BsonArray
                        {
                            filteredParticipations,
                            new BsonArray { participation.ToBsonDocument() }
                        })))
                });

            var updatedActivity = await _context.TeamActivities.FindOneAndUpdateAsync(
                updateFilter,
                updateDefinition,
                new FindOneAndUpdateOptions<TeamActivity>
                {
                    ReturnDocument = ReturnDocument.After
                });

            if (updatedActivity == null)
                return BadRequest("Activity is closed.");

            return Ok(await BuildDtoAsync(updatedActivity, currentUserId));
        }

        [HttpPost("{activityId}/complete")]
        public async Task<IActionResult> Complete(string teamId, string activityId)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var team = await GetTeamAsync(teamId);
            if (team == null)
                return NotFound("Team not found");

            if (!CanManageTeamActivities(team, currentUserId, currentUserRole))
                return Forbid();

            var activity = await GetActivityAsync(teamId, activityId);
            if (activity == null)
                return NotFound("Activity not found");

            activity.Status = "Closed";
            activity.CompletedAt = DateTime.UtcNow;

            await _context.TeamActivities.ReplaceOneAsync(existingActivity => existingActivity.Id == activity.Id, activity);

            return Ok(await BuildDtoAsync(activity, currentUserId));
        }

        private async Task<Team?> GetTeamAsync(string teamId)
        {
            return await _context.Teams.Find(team => team.Id == teamId).FirstOrDefaultAsync();
        }

        private async Task<TeamActivity?> GetActivityAsync(string teamId, string activityId)
        {
            return await _context.TeamActivities
                .Find(activity => activity.Id == activityId && activity.TeamId == teamId)
                .FirstOrDefaultAsync();
        }

        private bool CanAccessTeam(Team team, string? currentUserId, string? currentUserRole)
        {
            return IsAdmin(currentUserRole)
                || (!string.IsNullOrWhiteSpace(currentUserId) && (team.OwnerId == currentUserId || team.MemberIds.Contains(currentUserId)));
        }

        private bool CanManageTeamActivities(Team team, string? currentUserId, string? currentUserRole)
        {
            return IsAdmin(currentUserRole)
                || (!string.IsNullOrWhiteSpace(currentUserId) && team.OwnerId == currentUserId);
        }

        private static bool IsAdmin(string? currentUserRole)
        {
            return string.Equals(currentUserRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role);
        }

        private async Task<List<TeamActivityDto>> BuildDtosAsync(List<TeamActivity> activities, string? currentUserId)
        {
            var authorIds = activities.Select(activity => activity.CreatedByUserId).Distinct().ToList();
            authorIds.AddRange(activities.SelectMany(activity => activity.Participations.Select(participation => participation.UserId)));

            var usersById = await GetUserSummariesAsync(authorIds.Distinct().ToList());
            return activities.Select(activity => MapActivity(activity, usersById, currentUserId)).ToList();
        }

        private async Task<TeamActivityDto> BuildDtoAsync(TeamActivity activity, string? currentUserId)
        {
            var userIds = new List<string> { activity.CreatedByUserId };
            userIds.AddRange(activity.Participations.Select(participation => participation.UserId));

            var usersById = await GetUserSummariesAsync(userIds.Distinct().ToList());
            return MapActivity(activity, usersById, currentUserId);
        }

        private TeamActivityDto MapActivity(TeamActivity activity, IReadOnlyDictionary<string, UserSummary> usersById, string? currentUserId)
        {
            var recentResponses = activity.Participations
                .OrderByDescending(participation => participation.SubmittedAt)
                .Take(3)
                .Select(participation => MapResponse(participation, usersById))
                .ToList();

            var currentUserResponse = activity.Participations.FirstOrDefault(participation => participation.UserId == currentUserId);
            var creator = GetUserSummary(activity.CreatedByUserId, usersById);

            return new TeamActivityDto
            {
                Id = activity.Id,
                TeamId = activity.TeamId,
                CreatedByUserId = activity.CreatedByUserId,
                CreatedByUserFullName = creator.FullName,
                CreatedByUserEmail = creator.Email,
                ActivityType = activity.ActivityType.ToString(),
                Title = activity.Title,
                Description = activity.Description,
                Options = activity.Options,
                Points = activity.Points,
                DueAt = activity.DueAt,
                Status = activity.Status,
                CreatedAt = activity.CreatedAt,
                CompletedAt = activity.CompletedAt,
                ResponsesCount = activity.Participations.Count,
                ParticipantCount = activity.Participations.Select(participation => participation.UserId).Distinct().Count(),
                HasCurrentUserResponded = currentUserResponse != null,
                CurrentUserTextResponse = currentUserResponse?.TextResponse,
                CurrentUserSelectedOptionIndex = currentUserResponse?.SelectedOptionIndex,
                RecentResponses = recentResponses
            };
        }

        private TeamActivityResponseDto MapResponse(TeamActivityParticipation participation, IReadOnlyDictionary<string, UserSummary> usersById)
        {
            var userSummary = GetUserSummary(participation.UserId, usersById);

            return new TeamActivityResponseDto
            {
                UserId = participation.UserId,
                UserFullName = userSummary.FullName,
                UserEmail = userSummary.Email,
                TextResponse = participation.TextResponse,
                SelectedOptionIndex = participation.SelectedOptionIndex,
                SubmittedAt = participation.SubmittedAt
            };
        }

        private TeamActivitySummaryDto BuildSummary(Team team, List<TeamActivity> activities)
        {
            var totalResponses = activities.Sum(activity => activity.Participations.Count);
            var participantCount = activities
                .SelectMany(activity => activity.Participations.Select(participation => participation.UserId))
                .Distinct()
                .Count();
            var teamMemberCount = team.MemberIds.Distinct().Count();

            return new TeamActivitySummaryDto
            {
                TeamId = team.Id,
                TotalActivities = activities.Count,
                OpenActivities = activities.Count(activity => string.Equals(activity.Status, "Open", StringComparison.OrdinalIgnoreCase)),
                ClosedActivities = activities.Count(activity => string.Equals(activity.Status, "Closed", StringComparison.OrdinalIgnoreCase)),
                TotalResponses = totalResponses,
                ParticipantCount = participantCount,
                TeamMemberCount = teamMemberCount,
                ParticipationRate = teamMemberCount > 0 ? Math.Round((double)participantCount / teamMemberCount * 100, 1) : 0,
                RecentActivitiesCount = activities.Count(activity => activity.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            };
        }

        private async Task<Dictionary<string, UserSummary>> GetUserSummariesAsync(List<string> userIds)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<string, UserSummary>();
            }

            var users = await _context.Users
                .Find(user => userIds.Contains(user.Id))
                .Project(user => new { user.Id, user.Email, user.FullName })
                .ToListAsync();

            return users.ToDictionary(
                user => user.Id,
                user => new UserSummary(user.FullName, user.Email ?? "Unknown"));
        }

        private static UserSummary GetUserSummary(string userId, IReadOnlyDictionary<string, UserSummary> usersById)
        {
            return usersById.TryGetValue(userId, out var user)
                ? user
                : new UserSummary(null, "Unknown");
        }

        

        private static List<string> NormalizeOptions(List<string> options)
        {
            return options
                .Select(option => option?.Trim())
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .Select(option => option!)
                .ToList();
        }

        private sealed record UserSummary(string? FullName, string Email);
    }
}
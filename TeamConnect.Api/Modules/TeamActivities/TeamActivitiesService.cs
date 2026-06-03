using MongoDB.Bson;
using MongoDB.Driver;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.TeamActivities
{
    public class TeamActivitiesService
    {
        private readonly ITeamActivityRepository _activityRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService? _notificationService;

        public TeamActivitiesService(ITeamActivityRepository activityRepository, IUserRepository userRepository, INotificationService? notificationService = null)
        {
            _activityRepository = activityRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        public async Task<List<TeamActivityDto>> GetAll(string teamId, string? currentUserId)
        {
            var activities = await _activityRepository.GetByTeamIdAsync(teamId);
            if (activities.Count == 0) return new List<TeamActivityDto>();
            return await BuildDtosAsync(activities, currentUserId);
        }

        public async Task<TeamActivityDto> Create(string teamId, CreateTeamActivityDto dto, string creatorId)
        {
            var options = NormalizeOptions(dto.Options);

            DateTime? dueAtUtc = null;
            if (dto.DueAt.HasValue)
            {
                dueAtUtc = dto.DueAt.Value.Kind == DateTimeKind.Utc
                    ? dto.DueAt.Value
                    : dto.DueAt.Value.ToUniversalTime();
            }

            var activity = new TeamActivity
            {
                TeamId = teamId,
                CreatedByUserId = creatorId,
                ActivityType = dto.ActivityType,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Options = options,
                DueAt = dueAtUtc,
                Points = dto.Points > 0 ? dto.Points : 10,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            await _activityRepository.InsertAsync(activity);

            if (_notificationService != null)
                await _notificationService.SendTeamActivityCreatedEmailAsync(teamId, activity.Id, creatorId);

            return await BuildDtoAsync(activity, creatorId);
        }

        public async Task<TeamActivityDto?> SubmitResponse(string teamId, string activityId, SubmitTeamActivityResponseDto dto, string userId)
        {
            var activity = await _activityRepository.FindByIdAndTeamIdAsync(activityId, teamId);
            if (activity == null) return null;

            var participation = new TeamActivityParticipation
            {
                UserId = userId,
                TextResponse = string.IsNullOrWhiteSpace(dto.TextResponse) ? null : dto.TextResponse.Trim(),
                SelectedOptionIndex = dto.SelectedOptionIndex,
                SubmittedAt = DateTime.UtcNow
            };

            BsonValue userIdBson = ObjectId.TryParse(userId, out var parsed) ? parsed : userId;

            var filteredParticipations = new BsonDocument("$filter", new BsonDocument
            {
                { "input", new BsonDocument("$ifNull", new BsonArray { "$Participations", new BsonArray() }) },
                { "as", "p" },
                { "cond", new BsonDocument("$ne", new BsonArray { "$$p.UserId", userIdBson }) }
            });

            var filter = Builders<TeamActivity>.Filter.And(
                Builders<TeamActivity>.Filter.Eq(a => a.Id, activity.Id),
                Builders<TeamActivity>.Filter.Eq(a => a.TeamId, teamId),
                Builders<TeamActivity>.Filter.Ne(a => a.Status, "Closed"));

            var update = Builders<TeamActivity>.Update.Pipeline(new[]
            {
                new BsonDocument("$set", new BsonDocument("Participations",
                    new BsonDocument("$concatArrays", new BsonArray
                    {
                        filteredParticipations,
                        new BsonArray { participation.ToBsonDocument() }
                    })))
            });

            var updated = await _activityRepository.SubmitResponseAsync(filter, update);
            if (updated == null) return null;

            return await BuildDtoAsync(updated, userId);
        }

        public async Task<TeamActivityDto?> Complete(string teamId, string activityId)
        {
            var activity = await _activityRepository.FindByIdAndTeamIdAsync(activityId, teamId);
            if (activity == null) return null;

            activity.Status = "Closed";
            activity.CompletedAt = DateTime.UtcNow;

            await _activityRepository.CompleteAsync(activity);
            return await BuildDtoAsync(activity, null);
        }

        public Task<TeamActivity?> FindActivity(string teamId, string activityId) =>
            _activityRepository.FindByIdAndTeamIdAsync(activityId, teamId);

        public async Task<TeamActivitySummaryDto> GetSummary(string teamId, List<string> memberIds)
        {
            var activities = await _activityRepository.GetByTeamIdAsync(teamId);
            var totalResponses = activities.Sum(a => a.Participations.Count);
            var participantCount = activities
                .SelectMany(a => a.Participations.Select(p => p.UserId))
                .Distinct().Count();
            var teamMemberCount = memberIds.Distinct().Count();

            return new TeamActivitySummaryDto
            {
                TeamId = teamId,
                TotalActivities = activities.Count,
                OpenActivities = activities.Count(a => string.Equals(a.Status, "Open", StringComparison.OrdinalIgnoreCase)),
                ClosedActivities = activities.Count(a => string.Equals(a.Status, "Closed", StringComparison.OrdinalIgnoreCase)),
                TotalResponses = totalResponses,
                ParticipantCount = participantCount,
                TeamMemberCount = teamMemberCount,
                ParticipationRate = teamMemberCount > 0 ? Math.Round((double)participantCount / teamMemberCount * 100, 1) : 0,
                RecentActivitiesCount = activities.Count(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            };
        }

        private async Task<List<TeamActivityDto>> BuildDtosAsync(List<TeamActivity> activities, string? currentUserId)
        {
            var userIds = activities.Select(a => a.CreatedByUserId)
                .Concat(activities.SelectMany(a => a.Participations.Select(p => p.UserId)))
                .Distinct();

            var usersById = (await _userRepository.FindSummariesByIdsAsync(userIds)).ToDictionary(u => u.Id);
            return activities.Select(a => MapActivity(a, usersById, currentUserId)).ToList();
        }

        private async Task<TeamActivityDto> BuildDtoAsync(TeamActivity activity, string? currentUserId)
        {
            var userIds = new[] { activity.CreatedByUserId }
                .Concat(activity.Participations.Select(p => p.UserId))
                .Distinct();

            var usersById = (await _userRepository.FindSummariesByIdsAsync(userIds)).ToDictionary(u => u.Id);
            return MapActivity(activity, usersById, currentUserId);
        }

        private static TeamActivityDto MapActivity(TeamActivity activity, Dictionary<string, UserSummary> usersById, string? currentUserId)
        {
            usersById.TryGetValue(activity.CreatedByUserId, out var creator);
            var currentUserResponse = activity.Participations.FirstOrDefault(p => p.UserId == currentUserId);

            return new TeamActivityDto
            {
                Id = activity.Id,
                TeamId = activity.TeamId,
                CreatedByUserId = activity.CreatedByUserId,
                CreatedByUserFullName = creator?.FullName,
                CreatedByUserEmail = creator?.Email ?? "Unknown",
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
                ParticipantCount = activity.Participations.Select(p => p.UserId).Distinct().Count(),
                HasCurrentUserResponded = currentUserResponse != null,
                CurrentUserTextResponse = currentUserResponse?.TextResponse,
                CurrentUserSelectedOptionIndex = currentUserResponse?.SelectedOptionIndex,
                RecentResponses = activity.Participations
                    .OrderByDescending(p => p.SubmittedAt)
                    .Take(3)
                    .Select(p =>
                    {
                        usersById.TryGetValue(p.UserId, out var user);
                        return new TeamActivityResponseDto
                        {
                            UserId = p.UserId,
                            UserFullName = user?.FullName,
                            UserEmail = user?.Email ?? "Unknown",
                            TextResponse = p.TextResponse,
                            SelectedOptionIndex = p.SelectedOptionIndex,
                            SubmittedAt = p.SubmittedAt
                        };
                    })
                    .ToList()
            };
        }

        private static List<string> NormalizeOptions(List<string> options) =>
            options.Select(o => o?.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o!).ToList();
    }
}

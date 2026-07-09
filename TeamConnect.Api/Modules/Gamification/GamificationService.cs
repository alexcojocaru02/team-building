using TeamConnect.Api.Modules.TeamActivities;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Repositories;
using FeedbackModel = TeamConnect.Api.Shared.Models.Feedback;
using IFeedbackRepository = TeamConnect.Api.Modules.Feedback.IFeedbackRepository;

namespace TeamConnect.Api.Modules.Gamification
{
    public class GamificationService
    {
        private const int PointsPerFeedbackGiven = 5;
        private const int PointsPerPositiveFeedbackReceived = 10;

        private readonly ITeamActivityRepository _teamActivityRepository;
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IUserRepository _userRepository;

        public GamificationService(
            ITeamActivityRepository teamActivityRepository,
            IFeedbackRepository feedbackRepository,
            IUserRepository userRepository)
        {
            _teamActivityRepository = teamActivityRepository;
            _feedbackRepository = feedbackRepository;
            _userRepository = userRepository;
        }

        public async Task<LeaderboardDto> GetLeaderboard(string teamId, List<string> memberIds)
        {
            var memberSet = memberIds.ToHashSet();

            var activities = await _teamActivityRepository.GetByTeamIdAsync(teamId);
            var activityPoints = new Dictionary<string, int>();
            foreach (var activity in activities)
            {
                foreach (var participation in activity.Participations)
                {
                    if (!memberSet.Contains(participation.UserId)) continue;
                    activityPoints.TryGetValue(participation.UserId, out var current);
                    activityPoints[participation.UserId] = current + activity.Points;
                }
            }

            var allFeedback = await _feedbackRepository.GetAllAsync();
            var teamFeedback = allFeedback
                .Where(f => memberSet.Contains(f.FromUserId) && memberSet.Contains(f.ToUserId))
                .ToList();

            var feedbackGiven = CountByUser(teamFeedback, f => f.FromUserId);
            var feedbackReceived = CountByUser(teamFeedback, f => f.ToUserId);
            var positiveFeedbackReceived = CountByUser(
                teamFeedback.Where(f => f.Tone == Shared.Models.FeedbackTone.Positive).ToList(),
                f => f.ToUserId);

            var users = await _userRepository.FindSummariesByIdsAsync(memberIds);

            var entries = users.Select(u =>
            {
                activityPoints.TryGetValue(u.Id, out var earnedActivityPoints);
                feedbackGiven.TryGetValue(u.Id, out var givenCount);
                feedbackReceived.TryGetValue(u.Id, out var receivedCount);
                positiveFeedbackReceived.TryGetValue(u.Id, out var positiveReceivedCount);

                return new LeaderboardEntryDto
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    ActivityPoints = earnedActivityPoints,
                    FeedbackGiven = givenCount,
                    FeedbackReceived = receivedCount,
                    TotalPoints = earnedActivityPoints
                        + givenCount * PointsPerFeedbackGiven
                        + positiveReceivedCount * PointsPerPositiveFeedbackReceived
                };
            })
            .OrderByDescending(e => e.TotalPoints)
            .ToList();

            for (var i = 0; i < entries.Count; i++)
            {
                entries[i].Rank = i + 1;
            }

            return new LeaderboardDto
            {
                TeamId = teamId,
                Entries = entries
            };
        }

        private static Dictionary<string, int> CountByUser(List<FeedbackModel> feedback, Func<FeedbackModel, string> selector)
        {
            return feedback
                .GroupBy(selector)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}

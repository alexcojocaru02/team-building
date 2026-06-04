using TeamConnect.Api.Modules.Feedback;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Repositories;

namespace TeamConnect.Api.Modules.Dashboard
{
    public class DashboardService
    {
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IUserRepository _userRepository;

        public DashboardService(IFeedbackRepository feedbackRepository, IUserRepository userRepository)
        {
            _feedbackRepository = feedbackRepository;
            _userRepository = userRepository;
        }

        public async Task<CohesionDashboardDto> GetCohesion(List<string> memberIds)
        {
            var memberSet = memberIds.ToHashSet();
            var feedbacks = await _feedbackRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();

            var teamFeedbacks = feedbacks
                .Where(f => memberSet.Contains(f.FromUserId) && memberSet.Contains(f.ToUserId))
                .ToList();

            var teamUsers = users.Where(u => memberSet.Contains(u.Id)).ToList();

            var userStats = teamUsers.Select(u => new UserFeedbackStatsDto
            {
                UserId = u.Id,
                Email = u.Email ?? string.Empty,
                FeedbackReceived = teamFeedbacks.Count(f => f.ToUserId == u.Id)
            })
            .OrderByDescending(u => u.FeedbackReceived)
            .ToList();

            return new CohesionDashboardDto
            {
                TotalFeedbacks = teamFeedbacks.Count,
                Users = userStats
            };
        }
    }
}

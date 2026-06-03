using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Modules.Feedback;
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

        public async Task<CohesionDashboardDto> GetCohesion()
        {
            var feedbacks = await _feedbackRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();

            var userStats = users.Select(u => new UserFeedbackStatsDto
            {
                UserId = u.Id,
                Email = u.Email ?? string.Empty,
                FeedbackReceived = feedbacks.Count(f => f.ToUserId == u.Id)
            })
            .OrderByDescending(u => u.FeedbackReceived)
            .ToList();

            return new CohesionDashboardDto
            {
                TotalFeedbacks = feedbacks.Count,
                Users = userStats
            };
        }
    }
}

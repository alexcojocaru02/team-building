using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Repositories;
using TeamConnect.Api.Shared.Services;
using FeedbackModel = TeamConnect.Api.Shared.Models.Feedback;

namespace TeamConnect.Api.Modules.Feedback
{
    public class FeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService? _notificationService;

        public FeedbackService(IFeedbackRepository feedbackRepository, IUserRepository userRepository, INotificationService? notificationService = null)
        {
            _feedbackRepository = feedbackRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        public async Task<bool> AreTeammatesAsync(string userId1, string userId2)
        {
            var user1 = await _userRepository.FindByIdAsync(userId1);
            var user2 = await _userRepository.FindByIdAsync(userId2);
            if (user1 == null || user2 == null) return false;
            return user1.TeamIds.Intersect(user2.TeamIds).Any();
        }

        public async Task<FeedbackResponseDto?> Send(CreateFeedbackDto dto, string fromUserId)
        {
            var feedback = new FeedbackModel
            {
                FromUserId = fromUserId,
                ToUserId = dto.ToUserId,
                Message = dto.Message,
                Category = dto.Category,
                Tone = dto.Tone,
                CreatedAt = DateTime.UtcNow
            };

            var summaries = (await _userRepository.FindSummariesByIdsAsync([fromUserId, dto.ToUserId]))
                .ToDictionary(u => u.Id);

            await _feedbackRepository.InsertAsync(feedback);

            if (_notificationService != null)
                await _notificationService.SendFeedbackReceivedEmailAsync(fromUserId, dto.ToUserId, dto.Message);

            return MapToDto(feedback, summaries);
        }

        public async Task<List<FeedbackResponseDto>> GetReceived(string userId)
        {
            var feedbacks = await _feedbackRepository.GetReceivedByUserAsync(userId);
            var userIds = feedbacks.SelectMany(f => new[] { f.FromUserId, f.ToUserId }).Distinct();
            var summaries = (await _userRepository.FindSummariesByIdsAsync(userIds)).ToDictionary(u => u.Id);
            return feedbacks.Select(f => MapToDto(f, summaries)).ToList();
        }

        private static FeedbackResponseDto MapToDto(FeedbackModel f, Dictionary<string, UserSummary> summaries)
        {
            summaries.TryGetValue(f.FromUserId, out var from);
            summaries.TryGetValue(f.ToUserId, out var to);
            return new FeedbackResponseDto
            {
                Id = f.Id,
                FromUserId = f.FromUserId,
                ToUserId = f.ToUserId,
                Message = f.Message,
                Category = f.Category,
                Tone = f.Tone,
                CreatedAt = f.CreatedAt,
                FromUserFullName = from?.FullName,
                FromUserEmail = from?.Email ?? "Unknown",
                ToUserFullName = to?.FullName,
                ToUserEmail = to?.Email ?? "Unknown"
            };
        }
    }
}

using FeedbackModel = TeamConnect.Api.Shared.Models.Feedback;

namespace TeamConnect.Api.Modules.Feedback
{
    public interface IFeedbackRepository
    {
        Task InsertAsync(FeedbackModel feedback);
        Task<List<FeedbackModel>> GetReceivedByUserAsync(string userId);
        Task<List<FeedbackModel>> GetAllAsync();
    }
}

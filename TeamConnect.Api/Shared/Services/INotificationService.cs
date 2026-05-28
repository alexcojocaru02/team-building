namespace TeamConnect.Api.Shared.Services
{
    public interface INotificationService
    {
        Task SendWelcomeEmailAsync(string userId);
        Task SendTeamMemberAddedEmailAsync(string teamId, string userId, string addedByUserId);
        Task SendFeedbackReceivedEmailAsync(string fromUserId, string toUserId, string message);
        Task SendTeamActivityCreatedEmailAsync(string teamId, string activityId, string createdByUserId);
    }
}
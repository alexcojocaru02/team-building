namespace TeamConnect.Api.Shared.DTOs
{
    public class CohesionDashboardDto
    {
        public int TotalFeedbacks { get; set; }
        public List<UserFeedbackStatsDto> Users { get; set; }
    }

    public class UserFeedbackStatsDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public int FeedbackReceived { get; set; }
    }

}

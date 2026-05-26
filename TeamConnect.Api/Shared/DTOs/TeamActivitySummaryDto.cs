namespace TeamConnect.Api.Shared.DTOs
{
    public class TeamActivitySummaryDto
    {
        public string TeamId { get; set; } = string.Empty;
        public int TotalActivities { get; set; }
        public int OpenActivities { get; set; }
        public int ClosedActivities { get; set; }
        public int TotalResponses { get; set; }
        public int ParticipantCount { get; set; }
        public int TeamMemberCount { get; set; }
        public double ParticipationRate { get; set; }
        public int RecentActivitiesCount { get; set; }
    }
}
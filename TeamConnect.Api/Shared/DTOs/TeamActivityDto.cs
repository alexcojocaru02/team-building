namespace TeamConnect.Api.Shared.DTOs
{
    public class TeamActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string CreatedByUserId { get; set; } = string.Empty;
        public string? CreatedByUserFullName { get; set; }
        public string? CreatedByUserEmail { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int Points { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ScheduledEndAt { get; set; }
        public string? MeetingLink { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int ResponsesCount { get; set; }
        public int ParticipantCount { get; set; }
        public bool HasCurrentUserResponded { get; set; }
        public string? CurrentUserTextResponse { get; set; }
        public int? CurrentUserSelectedOptionIndex { get; set; }
        public string? CurrentUserRsvpStatus { get; set; }
        public int AcceptedCount { get; set; }
        public int DeclinedCount { get; set; }
        public List<TeamActivityResponseDto> RecentResponses { get; set; } = new();
    }
}
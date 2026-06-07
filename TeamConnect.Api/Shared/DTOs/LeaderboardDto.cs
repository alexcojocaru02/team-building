namespace TeamConnect.Api.Shared.DTOs
{
    public class LeaderboardDto
    {
        public string TeamId { get; set; } = string.Empty;
        public List<LeaderboardEntryDto> Entries { get; set; } = new();
    }

    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public int ActivityPoints { get; set; }
        public int FeedbackGiven { get; set; }
        public int FeedbackReceived { get; set; }
        public int TotalPoints { get; set; }
    }
}

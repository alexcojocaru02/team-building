namespace TeamConnect.Api.Shared.DTOs
{
    public class TeamJoinRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTeamResponseDto
    {
        public TeamDetailDto Team { get; set; } = null!;
        public string? NewToken { get; set; }
    }
}

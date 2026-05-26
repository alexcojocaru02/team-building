namespace TeamConnect.Api.Shared.DTOs
{
    public class TeamActivityResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string? TextResponse { get; set; }
        public int? SelectedOptionIndex { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
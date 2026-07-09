namespace TeamConnect.Api.Shared.DTOs
{
    public class SubmitTeamActivityResponseDto
    {
        public string? TextResponse { get; set; }
        public int? SelectedOptionIndex { get; set; }
        public Models.RsvpStatus? RsvpStatus { get; set; }
    }
}
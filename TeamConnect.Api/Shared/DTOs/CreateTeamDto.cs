namespace TeamConnect.Api.Shared.DTOs
{
    public class CreateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

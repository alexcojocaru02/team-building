namespace TeamConnect.Api.Shared.DTOs
{
    public class TeamDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OwnerId { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> MemberIds { get; set; } = new();
    }
}

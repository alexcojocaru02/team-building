namespace TeamConnect.Api.Shared.DTOs
{
    public class FeedPostCommentDto
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorId { get; set; }
        public string AuthorFullName { get; set; }
        public string AuthorEmail { get; set; }
    }
}

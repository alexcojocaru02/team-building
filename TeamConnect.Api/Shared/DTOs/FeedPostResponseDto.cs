namespace TeamConnect.Api.Shared.DTOs
{
    public class FeedPostResponseDto
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public string AuthorId { get; set; }
        public string AuthorFullName { get; set; }
        public string AuthorEmail { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool LikedByCurrentUser { get; set; }
        public List<FeedPostCommentDto> RecentComments { get; set; } = new();
    }

}

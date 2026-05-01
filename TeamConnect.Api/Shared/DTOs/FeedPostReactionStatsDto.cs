namespace TeamConnect.Api.Shared.DTOs
{
    public class FeedPostReactionStatsDto
    {
        public string PostId { get; set; }
        public int LikesCount { get; set; }
        public bool LikedByCurrentUser { get; set; }
    }
}

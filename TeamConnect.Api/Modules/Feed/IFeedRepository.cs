using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Modules.Feed
{
    public interface IFeedRepository
    {
        Task<List<FeedPost>> GetAllPostsAsync();
        Task<FeedPost?> FindPostByIdAsync(string postId);
        Task InsertPostAsync(FeedPost post);
        Task<List<FeedPostLike>> GetLikesByPostIdsAsync(IEnumerable<string> postIds);
        Task<List<FeedPostLike>> GetLikesByPostIdAsync(string postId);
        Task<FeedPostLike?> FindLikeAsync(string postId, string userId);
        Task InsertLikeAsync(FeedPostLike like);
        Task DeleteLikeAsync(string postId, string userId);
        Task<List<FeedPostComment>> GetCommentsByPostIdsAsync(IEnumerable<string> postIds);
        Task<List<FeedPostComment>> GetCommentsByPostIdAsync(string postId);
        Task<List<FeedPostComment>> GetRecentCommentsAsync(string postId, int limit);
        Task<long> CountCommentsAsync(string postId);
        Task InsertCommentAsync(FeedPostComment comment);
    }
}

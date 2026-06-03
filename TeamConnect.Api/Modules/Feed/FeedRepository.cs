using MongoDB.Driver;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Feed
{
    public class FeedRepository : IFeedRepository
    {
        private readonly MongoDbContext _context;

        public FeedRepository(MongoDbContext context)
        {
            _context = context;
        }

        public Task<List<FeedPost>> GetAllPostsAsync() =>
            _context.FeedPosts.Find(_ => true).SortByDescending(p => p.CreatedAt).ToListAsync();

        public Task<FeedPost?> FindPostByIdAsync(string postId) =>
            _context.FeedPosts.Find(p => p.Id == postId).FirstOrDefaultAsync()!;

        public Task InsertPostAsync(FeedPost post) =>
            _context.FeedPosts.InsertOneAsync(post);

        public async Task<List<FeedPostLike>> GetLikesByPostIdsAsync(IEnumerable<string> postIds)
        {
            var ids = postIds.ToList();
            return await _context.FeedPostLikes.Find(l => ids.Contains(l.FeedPostId)).ToListAsync();
        }

        public Task<List<FeedPostLike>> GetLikesByPostIdAsync(string postId) =>
            _context.FeedPostLikes.Find(l => l.FeedPostId == postId).ToListAsync();

        public Task<FeedPostLike?> FindLikeAsync(string postId, string userId) =>
            _context.FeedPostLikes.Find(l => l.FeedPostId == postId && l.UserId == userId).FirstOrDefaultAsync()!;

        public Task InsertLikeAsync(FeedPostLike like) =>
            _context.FeedPostLikes.InsertOneAsync(like);

        public Task DeleteLikeAsync(string postId, string userId) =>
            _context.FeedPostLikes.DeleteOneAsync(l => l.FeedPostId == postId && l.UserId == userId);

        public async Task<List<FeedPostComment>> GetCommentsByPostIdsAsync(IEnumerable<string> postIds)
        {
            var ids = postIds.ToList();
            return await _context.FeedPostComments
                .Find(c => ids.Contains(c.FeedPostId))
                .SortByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public Task<List<FeedPostComment>> GetCommentsByPostIdAsync(string postId) =>
            _context.FeedPostComments.Find(c => c.FeedPostId == postId).SortByDescending(c => c.CreatedAt).ToListAsync();

        public Task<List<FeedPostComment>> GetRecentCommentsAsync(string postId, int limit) =>
            _context.FeedPostComments.Find(c => c.FeedPostId == postId).SortByDescending(c => c.CreatedAt).Limit(limit).ToListAsync();

        public Task<long> CountCommentsAsync(string postId) =>
            _context.FeedPostComments.CountDocumentsAsync(c => c.FeedPostId == postId);

        public Task InsertCommentAsync(FeedPostComment comment) =>
            _context.FeedPostComments.InsertOneAsync(comment);
    }
}

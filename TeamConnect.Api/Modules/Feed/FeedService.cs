using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;

namespace TeamConnect.Api.Modules.Feed
{
    public class FeedService
    {
        private readonly IFeedRepository _feedRepository;
        private readonly IUserRepository _userRepository;

        public FeedService(IFeedRepository feedRepository, IUserRepository userRepository)
        {
            _feedRepository = feedRepository;
            _userRepository = userRepository;
        }

        public async Task<FeedPostResponseDto> CreatePost(string content, string authorId)
        {
            var post = new FeedPost
            {
                AuthorId = authorId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _feedRepository.InsertPostAsync(post);
            return await BuildSinglePostResponseAsync(post, authorId);
        }

        public async Task<List<FeedPostResponseDto>> GetAll(string? currentUserId)
        {
            var posts = await _feedRepository.GetAllPostsAsync();
            if (posts.Count == 0) return new List<FeedPostResponseDto>();

            var postIds = posts.Select(p => p.Id).ToList();
            var likes = await _feedRepository.GetLikesByPostIdsAsync(postIds);
            var comments = await _feedRepository.GetCommentsByPostIdsAsync(postIds);

            var authorIds = posts.Select(p => p.AuthorId).Concat(comments.Select(c => c.UserId)).Distinct();
            var authorSummaries = (await _userRepository.FindSummariesByIdsAsync(authorIds))
                .ToDictionary(u => u.Id);

            var likesByPostId = likes.GroupBy(l => l.FeedPostId).ToDictionary(g => g.Key, g => g.ToList());
            var commentsByPostId = comments.GroupBy(c => c.FeedPostId).ToDictionary(g => g.Key, g => g.ToList());

            return posts.Select(post => BuildFeedPostResponse(
                post,
                authorSummaries,
                likesByPostId.TryGetValue(post.Id, out var postLikes) ? postLikes : new List<FeedPostLike>(),
                commentsByPostId.TryGetValue(post.Id, out var postComments) ? postComments : new List<FeedPostComment>(),
                commentsByPostId.TryGetValue(post.Id, out var countComments) ? countComments.Count : 0,
                currentUserId
            )).ToList();
        }

        public async Task<FeedPostReactionStatsDto?> Like(string postId, string userId)
        {
            var post = await _feedRepository.FindPostByIdAsync(postId);
            if (post == null) return null;

            var existingLike = await _feedRepository.FindLikeAsync(postId, userId);
            if (existingLike is null)
                await _feedRepository.InsertLikeAsync(new FeedPostLike { FeedPostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow });

            return await GetReactionStatsAsync(postId, userId);
        }

        public async Task<FeedPostReactionStatsDto?> Unlike(string postId, string userId)
        {
            var post = await _feedRepository.FindPostByIdAsync(postId);
            if (post == null) return null;

            await _feedRepository.DeleteLikeAsync(postId, userId);
            return await GetReactionStatsAsync(postId, userId);
        }

        public async Task<List<FeedPostCommentDto>?> GetComments(string postId)
        {
            var post = await _feedRepository.FindPostByIdAsync(postId);
            if (post == null) return null;

            var comments = await _feedRepository.GetCommentsByPostIdAsync(postId);
            var authorSummaries = (await _userRepository.FindSummariesByIdsAsync(comments.Select(c => c.UserId).Distinct()))
                .ToDictionary(u => u.Id);

            return comments.OrderBy(c => c.CreatedAt).Select(c => MapComment(c, authorSummaries)).ToList();
        }

        public async Task<bool> DeletePost(string postId, string requesterId, string requesterRole)
        {
            var post = await _feedRepository.FindPostByIdAsync(postId);
            if (post == null) return false;

            if (post.AuthorId != requesterId && requesterRole != UserRoles.Admin)
                throw new UnauthorizedAccessException();

            await _feedRepository.DeletePostAsync(postId);
            return true;
        }

        public async Task<FeedPostCommentDto?> AddComment(string postId, string content, string userId)
        {
            var post = await _feedRepository.FindPostByIdAsync(postId);
            if (post == null) return null;

            var comment = new FeedPostComment
            {
                FeedPostId = postId,
                UserId = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _feedRepository.InsertCommentAsync(comment);

            var summaries = await _userRepository.FindSummariesByIdsAsync([userId]);
            var authorSummaries = summaries.ToDictionary(u => u.Id);
            return MapComment(comment, authorSummaries);
        }

        private async Task<FeedPostResponseDto> BuildSinglePostResponseAsync(FeedPost post, string? currentUserId)
        {
            var likes = await _feedRepository.GetLikesByPostIdAsync(post.Id);
            var comments = await _feedRepository.GetRecentCommentsAsync(post.Id, 3);
            var commentsCount = (int)await _feedRepository.CountCommentsAsync(post.Id);

            var authorIds = new[] { post.AuthorId }.Concat(comments.Select(c => c.UserId)).Distinct();
            var authorSummaries = (await _userRepository.FindSummariesByIdsAsync(authorIds)).ToDictionary(u => u.Id);

            return BuildFeedPostResponse(post, authorSummaries, likes, comments, commentsCount, currentUserId);
        }

        private async Task<FeedPostReactionStatsDto> GetReactionStatsAsync(string postId, string? currentUserId)
        {
            var likes = await _feedRepository.GetLikesByPostIdAsync(postId);
            return new FeedPostReactionStatsDto
            {
                PostId = postId,
                LikesCount = likes.Count,
                LikedByCurrentUser = !string.IsNullOrWhiteSpace(currentUserId) && likes.Any(l => l.UserId == currentUserId)
            };
        }

        private static FeedPostResponseDto BuildFeedPostResponse(
            FeedPost post,
            Dictionary<string, UserSummary> authorSummaries,
            List<FeedPostLike> likes,
            List<FeedPostComment> comments,
            int commentsCount,
            string? currentUserId)
        {
            var author = authorSummaries.TryGetValue(post.AuthorId, out var a) ? a : new UserSummary(post.AuthorId, null, "Unknown");
            var recentComments = comments.OrderBy(c => c.CreatedAt).Select(c => MapComment(c, authorSummaries)).ToList();

            return new FeedPostResponseDto
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                AuthorId = post.AuthorId,
                AuthorFullName = author.FullName ?? string.Empty,
                AuthorEmail = author.Email,
                LikesCount = likes.Count,
                CommentsCount = commentsCount,
                LikedByCurrentUser = !string.IsNullOrWhiteSpace(currentUserId) && likes.Any(l => l.UserId == currentUserId),
                RecentComments = recentComments
            };
        }

        private static FeedPostCommentDto MapComment(FeedPostComment comment, Dictionary<string, UserSummary> authorSummaries)
        {
            var author = authorSummaries.TryGetValue(comment.UserId, out var a) ? a : new UserSummary(comment.UserId, null, "Unknown");
            return new FeedPostCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                AuthorId = comment.UserId,
                AuthorFullName = author.FullName ?? string.Empty,
                AuthorEmail = author.Email
            };
        }
    }
}

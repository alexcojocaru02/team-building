using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

[ApiController]
[Route("api/feed")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly MongoDbContext _context;

    public FeedController(MongoDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFeedPostDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest("Content is required.");
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var author = await GetUserSummaryAsync(userId);
        var post = new FeedPost
        {
            AuthorId = userId,
            Content = dto.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.FeedPosts.InsertOneAsync(post);

        return Ok(await BuildFeedPostResponseAsync(post, userId, author));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var currentUserId = GetCurrentUserId();
        var posts = await _context.FeedPosts
            .Find(_ => true)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();

        if (posts.Count == 0)
        {
            return Ok(Array.Empty<FeedPostResponseDto>());
        }

        var postIds = posts.Select(p => p.Id).ToList();
        var authorIds = posts.Select(p => p.AuthorId).ToList();

        var likes = await _context.FeedPostLikes
            .Find(l => postIds.Contains(l.FeedPostId))
            .ToListAsync();

        var comments = await _context.FeedPostComments
            .Find(c => postIds.Contains(c.FeedPostId))
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();

        authorIds.AddRange(comments.Select(c => c.UserId));
        var authorSummaries = await GetUserSummariesAsync(authorIds.Distinct().ToList());

        var likesByPostId = likes
            .GroupBy(l => l.FeedPostId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var commentsByPostId = comments
            .GroupBy(c => c.FeedPostId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = posts.Select(post => BuildFeedPostResponse(
            post,
            authorSummaries,
            likesByPostId.TryGetValue(post.Id, out var postLikes) ? postLikes : new List<FeedPostLike>(),
            commentsByPostId.TryGetValue(post.Id, out var postComments) ? postComments : new List<FeedPostComment>(),
            commentsByPostId.TryGetValue(post.Id, out var postCommentsCount) ? postCommentsCount.Count : 0,
            currentUserId
        ));

        return Ok(result);
    }

    [HttpPost("{postId}/like")]
    public async Task<IActionResult> Like(string postId)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized();
        }

        var post = await GetPostAsync(postId);
        if (post is null)
        {
            return NotFound();
        }

        var existingLike = await _context.FeedPostLikes
            .Find(l => l.FeedPostId == postId && l.UserId == currentUserId)
            .FirstOrDefaultAsync();

        if (existingLike is null)
        {
            await _context.FeedPostLikes.InsertOneAsync(new FeedPostLike
            {
                FeedPostId = postId,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Ok(await GetReactionStatsAsync(postId, currentUserId));
    }

    [HttpDelete("{postId}/like")]
    public async Task<IActionResult> Unlike(string postId)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized();
        }

        var post = await GetPostAsync(postId);
        if (post is null)
        {
            return NotFound();
        }

        await _context.FeedPostLikes.DeleteOneAsync(l => l.FeedPostId == postId && l.UserId == currentUserId);
        return Ok(await GetReactionStatsAsync(postId, currentUserId));
    }

    [HttpGet("{postId}/comments")]
    public async Task<IActionResult> GetComments(string postId)
    {
        var post = await GetPostAsync(postId);
        if (post is null)
        {
            return NotFound();
        }

        var comments = await _context.FeedPostComments
            .Find(c => c.FeedPostId == postId)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();

        var authorSummaries = await GetUserSummariesAsync(comments.Select(c => c.UserId).Distinct().ToList());

        var result = comments
            .OrderBy(c => c.CreatedAt)
            .Select(comment => MapComment(comment, authorSummaries))
            .ToList();

        return Ok(result);
    }

    [HttpPost("{postId}/comments")]
    public async Task<IActionResult> AddComment(string postId, CreateFeedPostCommentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest("Content is required.");
        }

        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized();
        }

        var post = await GetPostAsync(postId);
        if (post is null)
        {
            return NotFound();
        }

        var comment = new FeedPostComment
        {
            FeedPostId = postId,
            UserId = currentUserId,
            Content = dto.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.FeedPostComments.InsertOneAsync(comment);

        var author = await GetUserSummaryAsync(currentUserId);
        var authorSummaries = new Dictionary<string, AuthorSummary>
        {
            [currentUserId] = author
        };

        return Ok(MapComment(comment, authorSummaries));
    }

    private async Task<FeedPostResponseDto> BuildFeedPostResponseAsync(FeedPost post, string? currentUserId, AuthorSummary? authorOverride = null)
    {
        var likes = await _context.FeedPostLikes
            .Find(l => l.FeedPostId == post.Id)
            .ToListAsync();

        var comments = await _context.FeedPostComments
            .Find(c => c.FeedPostId == post.Id)
            .SortByDescending(c => c.CreatedAt)
            .Limit(3)
            .ToListAsync();

        var commentsCount = (int)await _context.FeedPostComments
            .CountDocumentsAsync(c => c.FeedPostId == post.Id);

        var authorIds = new List<string> { post.AuthorId };
        authorIds.AddRange(comments.Select(c => c.UserId));
        var authorSummaries = await GetUserSummariesAsync(authorIds.Distinct().ToList());

        return BuildFeedPostResponse(post, authorSummaries, likes, comments, commentsCount, currentUserId, authorOverride);
    }

    private FeedPostResponseDto BuildFeedPostResponse(
        FeedPost post,
        Dictionary<string, AuthorSummary> authorSummaries,
        List<FeedPostLike> likes,
        List<FeedPostComment> comments,
        int commentsCount,
        string? currentUserId,
        AuthorSummary? authorOverride = null)
    {
        var author = authorOverride ?? GetAuthorSummary(post.AuthorId, authorSummaries);
        var recentComments = comments
            .OrderBy(c => c.CreatedAt)
            .Select(comment => MapComment(comment, authorSummaries))
            .ToList();

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

    private async Task<FeedPostReactionStatsDto> GetReactionStatsAsync(string postId, string? currentUserId)
    {
        var likes = await _context.FeedPostLikes
            .Find(l => l.FeedPostId == postId)
            .ToListAsync();

        return new FeedPostReactionStatsDto
        {
            PostId = postId,
            LikesCount = likes.Count,
            LikedByCurrentUser = !string.IsNullOrWhiteSpace(currentUserId) && likes.Any(l => l.UserId == currentUserId)
        };
    }

    private async Task<FeedPost?> GetPostAsync(string postId)
    {
        return await _context.FeedPosts
            .Find(p => p.Id == postId)
            .FirstOrDefaultAsync();
    }

    private async Task<AuthorSummary> GetUserSummaryAsync(string userId)
    {
        var author = await _context.Users
            .Find(u => u.Id == userId)
            .Project(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync();

        return new AuthorSummary(author?.FullName, author?.Email ?? "Unknown");
    }

    private async Task<Dictionary<string, AuthorSummary>> GetUserSummariesAsync(List<string> userIds)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<string, AuthorSummary>();
        }

        var users = await _context.Users
            .Find(u => userIds.Contains(u.Id))
            .Project(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync();

        return users.ToDictionary(
            user => user.Id,
            user => new AuthorSummary(user.FullName, user.Email ?? "Unknown")
        );
    }

    private static AuthorSummary GetAuthorSummary(string userId, IReadOnlyDictionary<string, AuthorSummary> authorSummaries)
    {
        return authorSummaries.TryGetValue(userId, out var author)
            ? author
            : new AuthorSummary(null, "Unknown");
    }

    private static FeedPostCommentDto MapComment(FeedPostComment comment, IReadOnlyDictionary<string, AuthorSummary> authorSummaries)
    {
        var author = GetAuthorSummary(comment.UserId, authorSummaries);
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

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private sealed record AuthorSummary(string? FullName, string Email);
}

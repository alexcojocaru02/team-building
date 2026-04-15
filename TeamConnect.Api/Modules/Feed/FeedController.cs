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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var author = await _context.Users
            .Find(u => u.Id == userId)
            .Project(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync();

        var post = new FeedPost
        {
            AuthorId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _context.FeedPosts.InsertOneAsync(post);

        var result = new FeedPostResponseDto
        {
            Id = post.Id,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            AuthorId = post.AuthorId,
            AuthorFullName = author?.FullName,
            AuthorEmail = author?.Email ?? "Unknown"
        };

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var posts = await _context.FeedPosts
            .Find(_ => true)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();

        var authorIds = posts
            .Select(p => p.AuthorId)
            .Distinct()
            .ToList();

        var users = await _context.Users
            .Find(u => authorIds.Contains(u.Id))
            .Project(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync();

        var authorsById = users.ToDictionary(u => u.Id);

        var result = posts.Select(p =>
        {
            var hasAuthor = authorsById.TryGetValue(p.AuthorId, out var author);

            return new FeedPostResponseDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                AuthorId = p.AuthorId,
                AuthorFullName = hasAuthor ? author?.FullName : null,
                AuthorEmail = hasAuthor ? author.Email : "Unknown"
            };
        });

        return Ok(result);
    }
}

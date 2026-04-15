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

        var post = new FeedPost
        {
            AuthorId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _context.FeedPosts.InsertOneAsync(post);

        var author = await _context.Users
            .Find(u => u.Id == userId)
            .Project(u => new { u.Email })
            .FirstOrDefaultAsync();

        var result = new FeedPostResponseDto
        {
            Id = post.Id,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            AuthorId = post.AuthorId,
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
            .Project(u => new { u.Id, u.Email })
            .ToListAsync();

        var authorEmailsById = users.ToDictionary(u => u.Id, u => u.Email);

        var result = posts.Select(p =>
        {
            var hasAuthorEmail = authorEmailsById.TryGetValue(p.AuthorId, out var authorEmail);

            return new FeedPostResponseDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                AuthorId = p.AuthorId,
                AuthorEmail = hasAuthorEmail ? authorEmail : "Unknown"
            };
        });

        return Ok(result);
    }
}

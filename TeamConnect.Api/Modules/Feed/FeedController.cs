using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Modules.Feed;

[ApiController]
[Route("api/feed")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly FeedService _feedService;

    public FeedController(FeedService feedService)
    {
        _feedService = feedService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFeedPostDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Content is required.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        return Ok(await _feedService.CreatePost(dto.Content, userId));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _feedService.GetAll(currentUserId));
    }

    [HttpPost("{postId}/like")]
    public async Task<IActionResult> Like(string postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _feedService.Like(postId, userId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{postId}/like")]
    public async Task<IActionResult> Unlike(string postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _feedService.Unlike(postId, userId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("{postId}/comments")]
    public async Task<IActionResult> GetComments(string postId)
    {
        var result = await _feedService.GetComments(postId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{postId}")]
    public async Task<IActionResult> DeletePost(string postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            var deleted = await _feedService.DeletePost(postId, userId, userRole);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{postId}/comments")]
    public async Task<IActionResult> AddComment(string postId, CreateFeedPostCommentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Content is required.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await _feedService.AddComment(postId, dto.Content, userId);
        return result == null ? NotFound() : Ok(result);
    }
}

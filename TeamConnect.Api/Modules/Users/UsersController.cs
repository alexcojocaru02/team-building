using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Modules.Users
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public UsersController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .Find(_ => true)
                .Project(u => new { u.Id, u.FullName, u.Email, u.Role })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            var dto = MapToProfileDto(user);
            return Ok(dto);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            return await GetById(currentUserId);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(string id, UpdateProfileDto dto)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            // Only allow users to update their own profile or admins to update any profile
            if (currentUserId != id && currentUserRole != UserRoles.Admin)
                return Forbid();

            var user = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return NotFound();

            // Null means no change; empty/whitespace means clear the stored value.
            if (dto.Bio != null) user.Bio = dto.Bio.Trim();
            if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl.Trim();
            if (dto.Department != null) user.Department = dto.Department.Trim();
            if (dto.Location != null) user.Location = dto.Location.Trim();
            if (dto.Timezone != null) user.Timezone = dto.Timezone.Trim();
            if (dto.Pronouns != null) user.Pronouns = dto.Pronouns.Trim();
            if (dto.PreferredWorkStyle != null) user.PreferredWorkStyle = dto.PreferredWorkStyle.Trim();
            if (dto.Icebreaker != null) user.Icebreaker = dto.Icebreaker.Trim();

            if (dto.Hobbies != null) user.Hobbies = dto.Hobbies;
            if (dto.Strengths != null) user.Strengths = dto.Strengths;

            user.UpdatedAt = DateTime.UtcNow;

            var update = Builders<User>.Update
                .Set(u => u.Bio, user.Bio)
                .Set(u => u.AvatarUrl, user.AvatarUrl)
                .Set(u => u.Department, user.Department)
                .Set(u => u.Location, user.Location)
                .Set(u => u.Timezone, user.Timezone)
                .Set(u => u.Pronouns, user.Pronouns)
                .Set(u => u.PreferredWorkStyle, user.PreferredWorkStyle)
                .Set(u => u.Icebreaker, user.Icebreaker)
                .Set(u => u.Hobbies, user.Hobbies)
                .Set(u => u.Strengths, user.Strengths)
                .Set(u => u.UpdatedAt, user.UpdatedAt);

            await _context.Users.UpdateOneAsync(u => u.Id == id, update);

            var responseDto = MapToProfileDto(user);
            return Ok(responseDto);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            return await UpdateProfile(currentUserId, dto);
        }

        private static UserProfileDto MapToProfileDto(User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                TeamIds = user.TeamIds,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                Department = user.Department,
                Location = user.Location,
                Timezone = user.Timezone,
                Pronouns = user.Pronouns,
                PreferredWorkStyle = user.PreferredWorkStyle,
                Hobbies = user.Hobbies,
                Strengths = user.Strengths,
                Icebreaker = user.Icebreaker,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}

using TeamConnect.Api.Shared.DTOs;
using TeamConnect.Api.Shared.Models;
using TeamConnect.Api.Shared.Repositories;

namespace TeamConnect.Api.Modules.Users
{
    public class UsersService
    {
        private readonly IUserRepository _userRepository;

        public UsersService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<List<UserListItem>> GetAll() =>
            _userRepository.GetAllAsync();

        public async Task<UserProfileDto?> GetById(string id)
        {
            var user = await _userRepository.FindByIdAsync(id);
            return user == null ? null : MapToProfileDto(user);
        }

        public async Task<UserProfileDto?> UpdateProfile(string id, UpdateProfileDto dto, string requesterId, string requesterRole)
        {
            if (requesterId != id && requesterRole != UserRoles.Admin)
                throw new UnauthorizedAccessException();

            var user = await _userRepository.FindByIdAsync(id);
            if (user == null) return null;

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

            await _userRepository.UpdateProfileAsync(id, user);
            return MapToProfileDto(user);
        }

        public async Task<bool> DeleteMe(string id)
        {
            var user = await _userRepository.FindByIdAsync(id);
            if (user == null) return false;

            await _userRepository.DeleteAsync(id);
            return true;
        }

        private static UserProfileDto MapToProfileDto(User user) => new()
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

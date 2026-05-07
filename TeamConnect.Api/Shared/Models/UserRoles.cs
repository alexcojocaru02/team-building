namespace TeamConnect.Api.Shared.Models
{
    public enum UserRole
    {
        Admin,
        User,
        TeamOwner
    }

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string TeamOwner = "TeamOwner";

        public static string Normalize(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return User;

            if (role.Equals(Admin, StringComparison.OrdinalIgnoreCase)) return Admin;
            if (role.Equals(TeamOwner, StringComparison.OrdinalIgnoreCase)) return TeamOwner;
            if (role.Equals(User, StringComparison.OrdinalIgnoreCase)) return User;

            return User;
        }
    }
}
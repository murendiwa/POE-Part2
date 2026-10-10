using RaceDay.Models;

namespace RaceDay.Auth;

public static class UserSessionExtensions
{
    private const string UserIdKey = "UserId";
    private const string RoleKey = "Role";

    public static void SetUser(this ISession session, int userId, UserRole role)
    {
        session.SetInt32(UserIdKey, userId);
        session.SetString(RoleKey, role.ToString());
    }

    public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);

    public static UserRole? GetRole(this ISession session) =>
        Enum.TryParse<UserRole>(session.GetString(RoleKey), out var role) ? role : null;
}
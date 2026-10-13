using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Models;

namespace RaceDay.Auth;

/// <summary>
/// [RequireRole] = any logged-in user.
/// [RequireRole(UserRole.Organiser)] = organisers only.
/// No attribute = public endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly UserRole[] _roles;

    public RequireRoleAttribute(params UserRole[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetUserId();
        var role = session.GetRole();

        if (userId is null || role is null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in." });
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(role.Value))
        {
            context.Result = new ObjectResult(new { message = "You do not have permission to do this." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
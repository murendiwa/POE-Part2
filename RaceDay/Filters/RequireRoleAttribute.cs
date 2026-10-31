using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute(params string[] roles) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = context.HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(role))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (roles.Length > 0 && !roles.Contains(role))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
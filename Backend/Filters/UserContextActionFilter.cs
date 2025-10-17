using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Backend.Filters
{
    [ExcludeFromCodeCoverage]
    public class UserContextActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            
            // Extract userId from claims
            var userIdClaim = user.FindFirst("UserId")?.Value;
            var userId = !string.IsNullOrEmpty(userIdClaim) 
                ? Guid.Parse(userIdClaim) 
                : Guid.Empty;
            
            // Extract userRole from claims
            var userRole = user.FindFirst("Role")?.Value ?? "learner";
            
            // Store in HttpContext.Items for access in controller
            context.HttpContext.Items["UserId"] = userId;
            context.HttpContext.Items["UserRole"] = userRole;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }
}
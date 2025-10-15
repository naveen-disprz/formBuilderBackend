using Microsoft.AspNetCore.Mvc;
using System;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api")]
    public abstract class BaseApiController : ControllerBase
    {
        protected Guid CurrentUserId => 
            HttpContext.Items.TryGetValue("UserId", out var userId) 
                ? (Guid)userId 
                : Guid.Empty;
        
        protected string CurrentUserRole => 
            HttpContext.Items.TryGetValue("UserRole", out var role) 
                ? role.ToString() 
                : "learner";
    }
}
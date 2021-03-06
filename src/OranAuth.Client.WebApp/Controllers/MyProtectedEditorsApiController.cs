using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OranAuth.Services.Constants;

namespace OranAuth.Client.WebApp.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("CorsPolicy")]
    [Authorize(Policy = Roles.Editor)]
    public class MyProtectedEditorsApiController : Controller
    {
        public IActionResult Get()
        {
            return Ok(new
            {
                Id = 1,
                Title = "Hello from My Protected Editors Controller! [Authorize(Policy = CustomRoles.Editor)]",
                Username = User.Identity.Name
            });
        }
    }
}
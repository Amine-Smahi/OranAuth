using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OranAuth.Common;
using OranAuth.Services;
using OranAuth.Services.Constants;
using OranAuth.Services.Services;

namespace OranAuth.Client.WebApp.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("CorsPolicy")]
    [Authorize(Policy = Roles.Admin)]
    public class MyProtectedAdminApiController : Controller
    {
        private readonly IUsersService _usersService;

        public MyProtectedAdminApiController(IUsersService usersService)
        {
            _usersService = usersService;
            _usersService.CheckArgumentIsNull(nameof(usersService));
        }

        public async Task<IActionResult> Get()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userDataClaim = claimsIdentity.FindFirst(ClaimTypes.UserData);
            var userId = userDataClaim.Value;

            return Ok(new
            {
                Id = 1,
                Title = "Hello from My Protected Admin Api Controller! [Authorize(Policy = CustomRoles.Admin)]",
                Username = User.Identity.Name,
                UserData = userId,
                TokenSerialNumber = await _usersService.GetSerialNumberAsync(int.Parse(userId)),
                Roles = claimsIdentity.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList()
            });
        }
    }
}

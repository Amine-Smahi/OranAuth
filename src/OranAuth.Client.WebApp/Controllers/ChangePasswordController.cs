using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OranAuth.Client.WebApp.Models;
using OranAuth.Common;
using OranAuth.Services;

namespace OranAuth.Client.WebApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [EnableCors("CorsPolicy")]
    public class ChangePasswordController : Controller
    {
        private readonly IUsersService _usersService;

        public ChangePasswordController(IUsersService usersService)
        {
            _usersService = usersService;
            _usersService.CheckArgumentIsNull(nameof(usersService));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post([FromBody] ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _usersService.GetCurrentUserAsync();
            if (user == null) return BadRequest("NotFound");

            var (Succeeded, Error) =
                await _usersService.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (Succeeded) return Ok();

            return BadRequest(Error);
        }
    }
}
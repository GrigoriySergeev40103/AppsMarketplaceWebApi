using AppsMarketplaceWebApi.DTO;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tusdotnet.Stores;

namespace AppsMarketplaceWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(AppDbContext dbContext, UserManager<User> userManager) : ControllerBase
    {
        protected readonly AppDbContext _dbContext = dbContext;
        protected readonly UserManager<User> _userManager = userManager;

        [HttpGet("GetUserById")]
        public async Task<ActionResult<UserDTO>> GetUserById(string userId)
        {
            User? user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            UserDTO toReturn = new()
            {
                Id = user.Id,
                UserName = user.UserName
            };

            return Ok(toReturn);
        }
    }
}

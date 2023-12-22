using AppsMarketplaceWebApi.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppsMarketplaceWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(UserManager<User> userManager) : ControllerBase
    {
        protected readonly UserManager<User> _userManager = userManager;

		[HttpGet("GetUserById")]
        public async Task<ActionResult<UserDTO>> GetUserById(string userId, [FromServices] AppDbContext dbContext)
        {
            User? user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);

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

		[HttpGet("GetUsersByIds")]
		public async Task<ActionResult<UserDTO[]>> GetUsersByIds([FromQuery] string[] userIds, [FromServices] AppDbContext dbContext)
		{
			UserDTO[] requestedUsers = await dbContext.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).
                Select(u => new UserDTO { Id = u.Id, UserName = u.UserName}).ToArrayAsync();

            if (requestedUsers == null)
                return NotFound();

            if (requestedUsers.Length == 0)
                return NotFound();

			return Ok(requestedUsers);
		}

		[Authorize]
		[HttpGet("GetMyself")]
		public async Task<ActionResult<PersonalUserDTO>> GetMyself()
		{
			User? user = await _userManager.GetUserAsync(User);

			if (user == null)
			{
				return BadRequest();
			}

			PersonalUserDTO toReturn = new()
			{
				Id = user.Id,
				UserName = user.UserName,
                Balance = user.Balance
			};

			return Ok(toReturn);
		}

		[HttpGet("GetUserAvatar")]
        public async Task<IActionResult> GetUserAvatar(string userId, [FromServices] AppDbContext dbContext)
        {
            User? user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            bool avatarExists = System.IO.File.Exists(user.PathToAvatarPic);
            if (!avatarExists)
                NotFound();

            return PhysicalFile(user.PathToAvatarPic, "image/png");
        }

        [Authorize]
        [HttpPut("UpdateAvatar")]
        public async Task<IActionResult> UpdateAvatar([FromForm] IFormFile file)
        {
            User? toUpdate = await _userManager.GetUserAsync(User);

            if (toUpdate == null)
                return Problem("Failed to identify the user");

            string path = toUpdate.PathToAvatarPic;

            using FileStream stream = new(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return Ok();
        }
    }
}

using AppsMarketplaceDTO;
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
                DisplayName = user.DisplayName
            };

            return Ok(toReturn);
        }

		[HttpGet("GetUsersByIds")]
		public async Task<ActionResult<UserDTO[]>> GetUsersByIds([FromQuery] string[] userIds, [FromServices] AppDbContext dbContext)
		{
            if (userIds.Length == 0)
                return NotFound("You must specify a list of users id to look for.");

			UserDTO[] requestedUsers = await dbContext.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).
                Select(u => new UserDTO { Id = u.Id, DisplayName = u.DisplayName}).ToArrayAsync();

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
				UserName = user.UserName
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

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteUserById")]
        public async Task<IActionResult> DeleteUserById([FromQuery] string userId, [FromServices] AppDbContext dbContext)
        {
            User? toDelete = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (toDelete == null)
                return NotFound("Couldn't find a user with the specified id.");

            // even if throws gonna be caught down the line and logged in console(by middleware i assume?)
			System.IO.File.Delete(toDelete.PathToAvatarPic);

            dbContext.Users.Remove(toDelete);

            await dbContext.SaveChangesAsync();

			return Ok();
        }
    }
}

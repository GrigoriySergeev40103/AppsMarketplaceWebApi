using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AppsMarketplaceWebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AppsControler(AppDbContext dbContext, UserManager<User> userManager) : ControllerBase
	{
		protected readonly AppDbContext _dbContext = dbContext;
		protected readonly UserManager<User> _userManager = userManager;

		[Authorize]
		[HttpPost("AcquireAppById")]
		public async Task<HttpStatusCode> AcquireAppById(int appId)
		{
			App? app = await _dbContext.Apps.FirstOrDefaultAsync(s => s.AppId == appId);

			if (app == null)
			{
				return HttpStatusCode.NotFound;
			}

			User? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
				return HttpStatusCode.BadRequest;
            }

			AppsOwnershipInfo ownershipInfo = new()
			{
				AppId = appId,
				UserId = user.Id
			};

			await _dbContext.AppsOwnershipInfos.AddAsync(ownershipInfo);
			await _dbContext.SaveChangesAsync();

			return HttpStatusCode.OK;
		}

		[HttpGet("GetAllApps")]
		public ActionResult<IEnumerable<App>> GetAllApps()
		{
			return _dbContext.Apps;
		}

   //     [HttpGet("GetAllAppsByCategoryId")]
   //     public ActionResult<IEnumerable<App>> GetAllAppsByCategoryId(int categoryId)
   //     {
			//return _dbContext.Apps.Where(app => app.CategoryId == categoryId).AsEnumerable();
   //     }
    }
}

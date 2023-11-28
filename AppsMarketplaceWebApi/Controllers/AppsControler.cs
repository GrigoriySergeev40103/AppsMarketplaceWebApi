using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using tusdotnet.Interfaces;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;

namespace AppsMarketplaceWebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AppsControler(AppDbContext dbContext, UserManager<User> userManager, TusDiskStore tusDiskStore) : ControllerBase
	{
		protected readonly AppDbContext _dbContext = dbContext;
		protected readonly UserManager<User> _userManager = userManager;
		protected readonly TusDiskStore _tusDiskStore = tusDiskStore;

		[Authorize]
		[HttpPost("AcquireAppById")]
		public async Task<IActionResult> AcquireAppById(int appId)
		{
			App? app = await _dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

			if (app == null)
			{
				return NotFound();
			}

			User? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
				return BadRequest();
            }

			AppsOwnershipInfo ownershipInfo = new()
			{
				AppId = appId,
				UserId = user.Id
			};

			await _dbContext.AppsOwnershipInfos.AddAsync(ownershipInfo);
			await _dbContext.SaveChangesAsync();

			return Ok();
		}

		[HttpGet("GetAllApps")]
		public async IAsyncEnumerable<App> GetAllApps()
		{
			IAsyncEnumerable<App> apps = _dbContext.Apps.AsNoTracking().AsAsyncEnumerable();

			await foreach (App app in apps)
			{
				yield return app;
			}
		}

		[Authorize(Roles = "Admin")]
		[HttpDelete("DeleteAppById")]
		public async Task<IActionResult> DeleteAppById(int appId)
		{
			App? toDelete = await _dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

			if(toDelete == null)
				return NotFound();

			_dbContext.Apps.Remove(toDelete);

			var terminationStore = (ITusTerminationStore)_tusDiskStore;
			//await terminationStore.DeleteFileAsync(toDelete.);

			await _dbContext.SaveChangesAsync();
			return Ok();
		}

		//     [HttpGet("GetAllAppsByCategoryId")]
		//     public ActionResult<IEnumerable<App>> GetAllAppsByCategoryId(int categoryId)
		//     {
		//return _dbContext.Apps.Where(app => app.CategoryId == categoryId).AsEnumerable();
		//     }
	}
}

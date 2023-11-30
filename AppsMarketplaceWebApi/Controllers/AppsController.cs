using AppsMarketplaceWebApi.DTO;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using System.Net;
using tusdotnet.Interfaces;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;

namespace AppsMarketplaceWebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AppsController(AppDbContext dbContext, UserManager<User> userManager, TusDiskStore tusDiskStore) : ControllerBase
	{
		protected readonly AppDbContext _dbContext = dbContext;
		protected readonly UserManager<User> _userManager = userManager;
		protected readonly TusDiskStore _tusDiskStore = tusDiskStore;

		[Authorize]
		[HttpPost("AcquireAppById")]
		public async Task<IActionResult> AcquireAppById(string appId)
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
		public async IAsyncEnumerable<AppDTO> GetAllApps()
		{
			IAsyncEnumerable<App> apps = _dbContext.Apps.AsNoTracking().AsAsyncEnumerable();

			AppDTO toSend = new();

			await foreach (App app in apps)
			{
				toSend.AppId = app.AppId;
				toSend.UploadDate = app.UploadDate;
				toSend.DeveloperId = app.DeveloperId;
				toSend.Price = app.Price;
				toSend.CategoryId = app.CategoryId;
				toSend.Description = app.Description;
                toSend.SpecialDescription = app.SpecialDescription;
                toSend.Name = app.Name;
                toSend.Extension = app.Extension;

                yield return toSend;
			}
		}
		
		[Authorize(Roles = "Admin")]
		[HttpDelete("DeleteAppById")]
		public async Task<IActionResult> DeleteAppById(string appId)
		{
			App? toDelete = await _dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

			if(toDelete == null)
				return NotFound();

			TusDiskStore terminationStore = _tusDiskStore;

			await terminationStore.DeleteFileAsync(toDelete.AppId, default);

			_dbContext.Apps.Remove(toDelete);

			await _dbContext.SaveChangesAsync();
			return Ok();
		}


		[Authorize]
		[HttpPost("UpdateAppImage")]
		public async Task<IActionResult> UpdateAppImage(string appId, [FromForm] IFormFile file)
		{
			App? requestedApp = await _dbContext.Apps.SingleOrDefaultAsync(a => a.AppId == appId);
			if (requestedApp == null)
				return NotFound();

			User? user = await _userManager.GetUserAsync(User);

			if (user == null)
				return BadRequest();

			if (user.Id != requestedApp.DeveloperId)
				return Unauthorized();

			string path = $"C:\\dev\\AppMarket\\images\\{appId}.jpg";

			using FileStream stream = new(path, FileMode.Create);
			await file.CopyToAsync(stream);

			return Ok();
		}

		[HttpGet("GetAppImage")]
		public async Task<IActionResult> GetAppImage(string appId)
		{
			App? requestedApp = await _dbContext.Apps.SingleOrDefaultAsync(a => a.AppId == appId);
			if (requestedApp == null)
				return NotFound();

			// TO DO make a check for valid path
			return PhysicalFile(requestedApp.AppPicturePath, "image/png");
		}


		//     [HttpGet("GetAllAppsByCategoryId")]
		//     public ActionResult<IEnumerable<App>> GetAllAppsByCategoryId(int categoryId)
		//     {
		//return _dbContext.Apps.Where(app => app.CategoryId == categoryId).AsEnumerable();
		//     }
	}
}

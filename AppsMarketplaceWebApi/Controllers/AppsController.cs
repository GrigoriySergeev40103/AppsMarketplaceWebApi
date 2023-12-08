using AppsMarketplaceWebApi.DTO;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tusdotnet.Stores;

namespace AppsMarketplaceWebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AppsController(AppDbContext dbContext, UserManager<User> userManager,
		TusDiskStore tusDiskStore, IConfiguration configuration) : ControllerBase
	{
		protected readonly AppDbContext _dbContext = dbContext;
		protected readonly UserManager<User> _userManager = userManager;
		protected readonly TusDiskStore _tusDiskStore = tusDiskStore;
		protected readonly IConfiguration _configuration = configuration;

		[Authorize]
		[HttpPost("AcquireAppById")]
		public async Task<IActionResult> AcquireAppById(string appId)
		{
			App? app = await _dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

			if (app == null)
				return NotFound();

			User? user = await _userManager.GetUserAsync(User);

			if (user == null)
				return BadRequest();

			AppsOwnershipInfo ownershipInfo = new()
			{
				AppId = appId,
				UserId = user.Id
			};

			bool alreadyOwns = await _dbContext.AppsOwnershipInfos.ContainsAsync(ownershipInfo);

			if (alreadyOwns)
				return BadRequest("You already own that app");

			if (user.Balance < app.Price)
				return BadRequest("Not enough money");

			user.Balance -= app.Price;

			await _dbContext.AppsOwnershipInfos.AddAsync(ownershipInfo);
			await _dbContext.SaveChangesAsync();

			return Ok();
		}

		[Authorize]
		[HttpGet("GetMyApps")]
		public async Task<IActionResult> GetMyApps()
		{
			User? user = await _userManager.GetUserAsync(User);

			if (user == null)
				return BadRequest();

			var res = await _dbContext.AppsOwnershipInfos.AsNoTracking().Where(oi => oi.UserId == user.Id).Join
			(	
				_dbContext.Apps,
				oi => oi.AppId, app => app.AppId,
				(oi, app) => new AppDTO
				{
					AppId = app.AppId,
					UploadDate = app.UploadDate,
					DeveloperId = app.DeveloperId,
					Price = app.Price,
					CategoryId = app.CategoryId,
					Description = app.Description,
					SpecialDescription = app.SpecialDescription,
					Name = app.Name,
					Extension = app.Extension
				}
			).ToArrayAsync();

			return Ok(res);
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

		[HttpGet("GetAppById")]
		public async Task<IActionResult> GetAppById(string appId)
		{
			App? app = await _dbContext.Apps.AsNoTracking().SingleOrDefaultAsync(a => a.AppId == appId);

			if (app == null)
				return NotFound();

			AppDTO toReturn = new()
			{
				AppId = app.AppId,
				UploadDate = app.UploadDate,
				DeveloperId = app.DeveloperId,
				Price = app.Price,
				CategoryId = app.CategoryId,
				Description = app.Description,
				SpecialDescription = app.SpecialDescription,
				Name = app.Name,
				Extension = app.Extension
			};

			return Ok(toReturn);
		}

		[Authorize(Roles = "Admin")]
		[HttpDelete("DeleteAppById")]
		public async Task<IActionResult> DeleteAppById(string appId)
		{
			App? toDelete = await _dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

			if (toDelete == null)
				return NotFound();

			TusDiskStore terminationStore = _tusDiskStore;

			// should probably add exception handling here
			await terminationStore.DeleteFileAsync(toDelete.AppId, default);
			System.IO.File.Delete(toDelete.AppPicturePath);

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

			string? imagesPath = _configuration.GetSection("ImageStore").Value;
			if (imagesPath == null)
				return Problem(statusCode: 500);

			string path = imagesPath + $"\\{appId}.png";

			// Will overwrite previous App's picture
			using FileStream fstream = new(path, FileMode.Create);
			await file.CopyToAsync(fstream);

			requestedApp.AppPicturePath = path;
			await _dbContext.SaveChangesAsync();

			return Ok();
		}

		[HttpGet("GetAppImage")]
		public async Task<IActionResult> GetAppImage(string appId)
		{
			var requestedApp = await _dbContext.Apps.AsNoTracking().Select(a => new { id = a.AppId, path = a.AppPicturePath })
				.SingleOrDefaultAsync(a => a.id == appId);

			if (requestedApp == null)
				return NotFound();

			// TO DO make a check for valid path
			return PhysicalFile(requestedApp.path, "image/png");
		}
	}
}

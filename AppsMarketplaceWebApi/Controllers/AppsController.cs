using AppsMarketplaceDTO;
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
    public class AppsController(UserManager<User> userManager, TusDiskStore tusDiskStore, IConfiguration configuration) : ControllerBase
    {
        protected readonly UserManager<User> _userManager = userManager;
        protected readonly TusDiskStore _tusDiskStore = tusDiskStore;
        protected readonly IConfiguration _configuration = configuration;

        [Authorize]
        [HttpPost("AcquireAppById")]
        public async Task<IActionResult> AcquireAppById(string appId, [FromServices] AppDbContext dbContext)
        {
            App? app = await dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

            if (app == null)
                return NotFound();

            User? user = await _userManager.GetUserAsync(User);
            User? developer = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == app.DeveloperId);

            if (user == null || developer == null)
                return BadRequest();

            AppsOwnershipInfo ownershipInfo = new()
            {
                AppId = appId,
                UserId = user.Id
            };

            bool alreadyOwns = await dbContext.AppsOwnershipInfos.ContainsAsync(ownershipInfo);

            if (alreadyOwns)
                return BadRequest("You already own that app");

            if (user.Balance < app.Price)
                return BadRequest("Not enough money");

            // Not really sure if I even should be using decimal for money operations(floating point arith innaccurate nature?) but it is what it is right now
            // should make sure to not overflow too
            user.Balance -= app.Price;
            developer.Balance += app.Price;

            await dbContext.AppsOwnershipInfos.AddAsync(ownershipInfo);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpGet("DownloadAppFileById")]
        public async Task<IActionResult> DownloadAppFileById(string appFileId, [FromServices] AppDbContext dbContext)
        {
            AppFile? appFile = await dbContext.AppFiles.AsNoTracking().SingleOrDefaultAsync(af => af.AppFileId == appFileId);

            if (appFile == null)
                return NotFound("Couldn't find a file with specified id");

			User? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Forbid();

            AppsOwnershipInfo ownershipInfo = new()
            {
                AppId = appFile.AppId,
                UserId = user.Id
            };

            bool owns = await dbContext.AppsOwnershipInfos.AsNoTracking().ContainsAsync(ownershipInfo);

            if (!owns)
                return BadRequest("You do not own the requested app");

			bool appDataExists = System.IO.File.Exists(appFile.Path);
			if (!appDataExists)
				return NotFound("Couldn't find requested file on the server");

			HttpContext.Response.Headers.Append("Content-Disposition", new[] { $"attachment; filename=\"{appFile.Filename}\"" });

            return PhysicalFile(appFile.Path, "application/octet-stream");
        }

        [Authorize]
        [HttpGet("GetMyApps")]
        public async Task<IActionResult> GetMyApps([FromServices] AppDbContext dbContext)
        {
            User? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return BadRequest();

            var res = await dbContext.AppsOwnershipInfos.AsNoTracking().Where(oi => oi.UserId == user.Id).Join
            (
                dbContext.Apps,
                oi => oi.AppId, app => app.AppId,
                (oi, app) => new AppDTO
                {
                    AppId = app.AppId,
                    UploadDate = app.UploadDate,
                    DeveloperId = app.DeveloperId,
                    Price = app.Price,
                    CategoryName = app.CategoryName,
                    Description = app.Description,
                    SpecialDescription = app.SpecialDescription,
                    Name = app.Name
                }
            ).ToArrayAsync();

            return Ok(res);
        }

		[HttpGet("GetAppsRange")]
		public async IAsyncEnumerable<AppDTO> GetAppsRange(int startIndex, int count, [FromServices] AppDbContext dbContext)
		{
			IAsyncEnumerable<AppDTO> apps = dbContext.Apps.AsNoTracking().Select(a => new AppDTO()
			{
				AppId = a.AppId,
				UploadDate = a.UploadDate,
				DeveloperId = a.DeveloperId,
				Price = a.Price,
				CategoryName = a.CategoryName,
				Description = a.Description,
				SpecialDescription = a.SpecialDescription,
				Name = a.Name
			}).OrderByDescending(a => a.UploadDate).Skip(startIndex).Take(count).AsAsyncEnumerable();

			await foreach (AppDTO app in apps)
			{
				yield return app;
			}
		}

		[Authorize]
        [HttpGet("IsOwned")]
        public async Task<IActionResult> IsOwned(string appId, [FromServices] AppDbContext dbContext)
        {
            App? app = await dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

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

            bool owns = await dbContext.AppsOwnershipInfos.AsNoTracking().ContainsAsync(ownershipInfo);

            if (owns)
                return Ok();
            else
                return Unauthorized("You don't own that app");
        }


        [HttpGet("GetAllApps")]
        public async IAsyncEnumerable<AppDTO> GetAllApps([FromServices] AppDbContext dbContext)
        {
            IAsyncEnumerable<AppDTO> apps = dbContext.Apps.AsNoTracking().Select(a => new AppDTO()
            {
                AppId = a.AppId,
                UploadDate = a.UploadDate,
                DeveloperId = a.DeveloperId,
                Price = a.Price,
                CategoryName = a.CategoryName,
                Description = a.Description,
                SpecialDescription = a.SpecialDescription,
                Name = a.Name
            }).AsAsyncEnumerable();

            await foreach (AppDTO app in apps)
            {
                yield return app;
            }
        }

        [HttpGet("GetAppById")]
        public async Task<IActionResult> GetAppById(string appId, [FromServices] AppDbContext dbContext)
        {
            App? app = await dbContext.Apps.AsNoTracking().SingleOrDefaultAsync(a => a.AppId == appId);

            if (app == null)
                return NotFound();

            AppDTO toReturn = new()
            {
                AppId = app.AppId,
                UploadDate = app.UploadDate,
                DeveloperId = app.DeveloperId,
                Price = app.Price,
                CategoryName = app.CategoryName,
                Description = app.Description,
                SpecialDescription = app.SpecialDescription,
                Name = app.Name
            };

            return Ok(toReturn);
        }

		[HttpGet("GetFullAppById")]
		public async Task<IActionResult> GetFullAppById(string appId, [FromServices] AppDbContext dbContext)
		{
			App? app = await dbContext.Apps.AsNoTracking().SingleOrDefaultAsync(a => a.AppId == appId);

			if (app == null)
				return NotFound("Couldn't find app with specified id.");

            AppFileDTO[] files = await dbContext.AppFiles.AsNoTracking().Where(af => af.AppId == app.AppId).Select(af => new AppFileDTO()
            {
                AppFileId = af.AppFileId,
                AppId = af.AppId,
                Filename = af.Filename,
                UploadDate = af.UploadDate
            }).ToArrayAsync();

			FullAppDTO toReturn = new()
			{
				AppId = app.AppId,
				UploadDate = app.UploadDate,
				DeveloperId = app.DeveloperId,
				Price = app.Price,
				CategoryName = app.CategoryName,
				Description = app.Description,
				SpecialDescription = app.SpecialDescription,
				Name = app.Name,
                Files = files
			};

			return Ok(toReturn);
		}

		[Authorize(Roles = "Admin")]
        [HttpDelete("DeleteAppById")]
        public async Task<IActionResult> DeleteAppById(string appId, [FromServices] AppDbContext dbContext)
        {
            App? toDelete = await dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

            if (toDelete == null)
                return NotFound();

            TusDiskStore terminationStore = _tusDiskStore;

            // should probably add exception handling here
            await terminationStore.DeleteFileAsync(toDelete.AppId, default);
            System.IO.File.Delete(toDelete.AppPicturePath);

            dbContext.Apps.Remove(toDelete);

            await dbContext.SaveChangesAsync();
            return Ok();
        }


        [Authorize]
        [HttpPost("UpdateAppImage")]
        public async Task<IActionResult> UpdateAppImage(string appId, [FromForm] IFormFile file, [FromServices] AppDbContext dbContext)
        {
            App? requestedApp = await dbContext.Apps.SingleOrDefaultAsync(a => a.AppId == appId);
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

            // Will overwrite previous App's picture if there's one
            using FileStream fstream = new(path, FileMode.Create);
            await file.CopyToAsync(fstream);

            requestedApp.AppPicturePath = path;
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("GetAppImage")]
        public async Task<IActionResult> GetAppImage(string appId, [FromServices] AppDbContext dbContext)
        {
            var requestedApp = await dbContext.Apps.AsNoTracking().Select(a => new { id = a.AppId, path = a.AppPicturePath })
                .SingleOrDefaultAsync(a => a.id == appId);

            if (requestedApp == null)
                return NotFound();

            bool picExists = System.IO.File.Exists(requestedApp.path);

            if (!picExists)
                return NotFound("Couldn't find app's image on the server.");

            return PhysicalFile(requestedApp.path, "image/png");
        }
    }
}

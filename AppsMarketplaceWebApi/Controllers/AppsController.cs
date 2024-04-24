using AppMarketplaceDTOs;
using AppsMarketplaceDTO;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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
        [HttpPost("FavoriteAppById")]
        public async Task<IActionResult> FavoriteAppById(string appId, [FromServices] AppDbContext dbContext)
        {
            App? app = await dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

            if (app == null)
                return NotFound();

            User? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return BadRequest();

            FavoriteAppsInfo favoriteInfo = new()
            {
                AppId = appId,
                UserId = user.Id
            };

            bool alreadyFavorite = await dbContext.FavoriteAppsInfo.ContainsAsync(favoriteInfo);

            if (alreadyFavorite)
                return BadRequest("You've already favorited this app");

            await dbContext.FavoriteAppsInfo.AddAsync(favoriteInfo);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

		[Authorize]
		[HttpPost("UnfavoriteAppById")]
		public async Task<IActionResult> UnfavoriteAppById(string appId, [FromServices] AppDbContext dbContext)
		{
			App? app = await dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

			if (app == null)
				return NotFound();

			User? user = await _userManager.GetUserAsync(User);

			if (user == null)
				return BadRequest();

			FavoriteAppsInfo favoriteInfo = new()
			{
				AppId = appId,
				UserId = user.Id
			};

			bool alreadyFavorite = await dbContext.FavoriteAppsInfo.ContainsAsync(favoriteInfo);

			if (!alreadyFavorite)
				return BadRequest("You haven't favorited this app");

			dbContext.FavoriteAppsInfo.Remove(favoriteInfo);
			await dbContext.SaveChangesAsync();

			return Ok();
		}

		[HttpGet("DownloadAppFileById")]
        public async Task<IActionResult> DownloadAppFileById(string appFileId, [FromServices] AppDbContext dbContext)
        {
            var appFile = await dbContext.AppFiles.AsNoTracking().Where(af => af.AppFileId == appFileId).Join
            (
                dbContext.Apps,
                af => af.AppId, a => a.AppId,
                (af, a) => new
                {
					af.AppId,
					af.AppFileId,
                    af.Filename,
                    af.Path
                }
            ).SingleOrDefaultAsync();

            if (appFile == null)
                return NotFound("Couldn't find a file with specified id");
            
			// System.Net.WebUtility.UrlEncode so we can return non ascii characters(headers don't allow non-ascii)
			HttpContext.Response.Headers.Append("Content-Disposition", new[] { $"attachment; filename=\"{System.Net.WebUtility.UrlEncode(appFile.Filename)}\"" });

            return PhysicalFile(appFile.Path, "application/octet-stream");
        }

        [Authorize]
        [HttpGet("GetMyApps")]
        public async Task<IActionResult> GetMyApps([FromServices] AppDbContext dbContext)
        {
            User? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return BadRequest();

            var res = await dbContext.FavoriteAppsInfo.AsNoTracking().Where(oi => oi.UserId == user.Id).Join
            (
                dbContext.Apps,
                oi => oi.AppId, app => app.AppId,
                (oi, app) => new AppDTO
                {
                    AppId = app.AppId,
                    UploadDate = app.UploadDate,
                    DeveloperId = app.DeveloperId,
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
        public async Task<IActionResult> IsFavorite(string appId, [FromServices] AppDbContext dbContext)
        {
            App? app = await dbContext.Apps.SingleOrDefaultAsync(s => s.AppId == appId);

            if (app == null)
                return NotFound();

            User? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return BadRequest();

            FavoriteAppsInfo ownershipInfo = new()
            {
                AppId = appId,
                UserId = user.Id
            };

            bool owns = await dbContext.FavoriteAppsInfo.AsNoTracking().ContainsAsync(ownershipInfo);

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

			AppFile[] filesToDelete = await dbContext.AppFiles.Where(af => af.AppId == appId).ToArrayAsync();
            for (int i = 0; i < filesToDelete.Length; i++)
            {
				// should probably add exception handling here
				await terminationStore.DeleteFileAsync(filesToDelete[i].AppFileId, default);
			}

            AppPicture[] appPics = await dbContext.AppPictures.Where(ap => ap.AppId == appId).ToArrayAsync();
            for (int i = 0; i < appPics.Length; i++)
            {
                System.IO.File.Delete(appPics[i].Path);
            }
            dbContext.AppPictures.RemoveRange(appPics);

			dbContext.Apps.Remove(toDelete);

            await dbContext.SaveChangesAsync();
            return Ok();
        }

		[Authorize]
		[HttpPost("UploadAppImage")]
		public async Task<IActionResult> UploadAppImage(string appId, [FromForm] IFormFile file, [FromServices] AppDbContext dbContext)
		{
			App? requestedApp = await dbContext.Apps.SingleOrDefaultAsync(a => a.AppId == appId);
			if (requestedApp == null)
				return NotFound("Couldn't find an app with given appId.");

			User? user = await _userManager.GetUserAsync(User);

			if (user == null)
				return BadRequest("Failed to find the account of a user trying to upload.");

			if (user.Id != requestedApp.DeveloperId)
				return Unauthorized("Trying to upload an image for an app you do not own.");

			string? imagesPath = _configuration.GetSection("ImageStore").Value;
			if (imagesPath == null)
            {
                throw new Exception("Failed to find an 'ImageStore' field in the configuration file");
            }

            string appPicGuid = Guid.NewGuid().ToString();

			string path = imagesPath + $"\\{appPicGuid}.png";

			// Will overwrite previous App's picture if there's one
			using FileStream fstream = new(path, FileMode.Create);
			await file.CopyToAsync(fstream);

            AppPicture appPic = new()
            {
                AppId = appId,
                Path = path,
                PictureId = appPicGuid,
                UploadDate = DateTime.UtcNow
            };

            await dbContext.AppPictures.AddAsync(appPic);

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

			requestedApp.AppMainPicPath = path;
			await dbContext.SaveChangesAsync();

			return Ok();
		}

		[HttpGet("GetAppImage")]
		public async Task<IActionResult> GetAppImage(string appId, [FromServices] AppDbContext dbContext)
		{
			var requestedApp = await dbContext.Apps.AsNoTracking().Select(a => new { id = a.AppId, path = a.AppMainPicPath })
				.SingleOrDefaultAsync(a => a.id == appId);

			if (requestedApp == null)
				return NotFound();

			bool picExists = System.IO.File.Exists(requestedApp.path);

			if (!picExists)
				return NotFound("Couldn't find app's image on the server.");

			return PhysicalFile(requestedApp.path, "image/png");
		}

		[HttpGet("GetAppPicById")]
        public async Task<IActionResult> GetAppPicById(string appPicId, [FromServices] AppDbContext dbContext)
        {
            var requestedPic = await dbContext.AppPictures.AsNoTracking().SingleOrDefaultAsync(ap => ap.PictureId == appPicId);

            if (requestedPic == null)
            {
                return NotFound("Could not find a picture with specified picture id");
            }

            return PhysicalFile(requestedPic.Path, "image/png");
        }

		[HttpGet("GetAppPicturesIds")]
		public async Task<IActionResult> GetAppPicturesIds(string appId, [FromServices] AppDbContext dbContext)
		{
			App? requestedApp = await dbContext.Apps.AsNoTracking().SingleOrDefaultAsync(a => a.AppId == appId);

            if (requestedApp == null)
                return NotFound("Failed to find an app with given app id");

            string[] appPicsIds = await dbContext.AppPictures.AsNoTracking().Where(ap => ap.AppId == appId).OrderByDescending(ap => ap.UploadDate)
                .Select(ap => ap.PictureId).ToArrayAsync();

            return Ok(appPicsIds);
		}

		[Authorize]
        [HttpPost("PostComment")]
        public async Task<IActionResult> PostComment([FromQuery] string appId, [FromBody] string commentContent, [FromServices] AppDbContext dbContext)
        {
            if (string.IsNullOrWhiteSpace(commentContent))
                return BadRequest("The comment can not be an null/empty/only whitespace");

            App? commentedApp = await dbContext.Apps.AsNoTracking().SingleOrDefaultAsync(a => a.AppId == appId);
            if (commentedApp == null)
                return NotFound("Could not find an app that you want to leave a comment on.");

            User? commentee = await _userManager.GetUserAsync(User);
            if (commentee == null)
                return BadRequest();

            Comment newComment = new();
            newComment.CommentId = Guid.NewGuid().ToString();
            newComment.AppId = appId;
            newComment.CommentContent = commentContent;
            newComment.CommenteeId = commentee.Id;
            newComment.UploadDate = DateTime.UtcNow;

            await dbContext.Comments.AddAsync(newComment);
            await dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("GetAppComments")]
        public async IAsyncEnumerable<Comment> GetAppComments(string appId, [FromServices] AppDbContext dbContext)
        {
            var comments = dbContext.Comments.AsNoTracking().Where(c => c.AppId == appId).OrderByDescending(c => c.UploadDate).AsAsyncEnumerable();

            await foreach(Comment comment in comments)
            {
                yield return comment;
            }
		}

		[HttpGet("GetExtAppComments")]
		public async IAsyncEnumerable<ExtCommentDTO> GetExtAppComments(string appId, [FromServices] AppDbContext dbContext)
		{
			var comments = dbContext.Comments.AsNoTracking().Where(c => c.AppId == appId).OrderByDescending(c => c.UploadDate).Join
                (
                    dbContext.Users,
                    c => c.CommenteeId, u => u.Id,
                    (c, u) => new ExtCommentDTO
                    {
                        CommentId = c.CommentId,
                        CommentContent = c.CommentContent,
                        CommenteeId = c.CommenteeId,
                        CommenteeName = u.DisplayName,
                        UploadDate = c.UploadDate,
                        AppId = appId
                    }
                ).AsAsyncEnumerable();

			await foreach (ExtCommentDTO comment in comments)
			{
				yield return comment;
			}
		}
	}
}

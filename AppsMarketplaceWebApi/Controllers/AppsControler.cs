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
	public class AppsControler(AppDbContext dbContext, UserManager<IdentityUser> userManager) : ControllerBase
	{
		protected readonly AppDbContext _dbContext = dbContext;
		protected readonly UserManager<IdentityUser> _userManager = userManager;

		[Authorize]
		[HttpPost("AcquireAppById")]
		public async Task<HttpStatusCode> AcquireAppById(int appId)
		{
			App? app = await _dbContext.Apps.FirstOrDefaultAsync(s => s.AppId == appId);

			if (app == null)
			{
				return HttpStatusCode.NotFound;
			}

			IdentityUser? user = await _userManager.GetUserAsync(User);

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

			return HttpStatusCode.OK;
		}
	}
}

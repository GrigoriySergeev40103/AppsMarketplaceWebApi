﻿using AppsMarketplaceWebApi.DTO;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text;
using tusdotnet.Stores;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AppsMarketplaceWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(AppDbContext dbContext) : ControllerBase
    {
        protected readonly AppDbContext _dbContext = dbContext;

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

        [HttpGet("GetUserAvatar")]
        public async Task<IActionResult> GetUserAvatar(string userId)
        {
            User? user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            bool avatarExists = System.IO.File.Exists(user.PathToAvatarPic);
            if (!avatarExists)
                NotFound();

            return PhysicalFile(user.PathToAvatarPic, "image/png");
        }

        //[Authorize]
        //[HttpPut("UpdateUserAvatar")]
        //public async Task<IActionResult> UpdateUserAvatar(string userId, [FromForm] IFormFile file)
        //{
        //    User? requestedUser = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
        //    if (requestedUser == null)
        //        return NotFound();

        //    User? user = await _userManager.GetUserAsync(User);

        //    if (user == null)
        //        return BadRequest();

        //    if (user.Id != requestedUser.Id)
        //        return Unauthorized();

        //    string path = $"C:\\dev\\AppMarket\\images\\{userId}.jpg";

        //    using FileStream stream = new(path, FileMode.Create);
        //    await file.CopyToAsync(stream);

        //    return Ok();
        //}
    }
}

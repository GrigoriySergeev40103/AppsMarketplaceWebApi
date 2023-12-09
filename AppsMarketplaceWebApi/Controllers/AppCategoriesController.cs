using AppsMarketplaceWebApi.DTO;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AppsMarketplaceWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppCategoriesController(AppDbContext appDbContext) : ControllerBase
    {
        protected readonly AppDbContext _dbContext = appDbContext;

        [Authorize(Roles = "Admin")]
        [HttpPost("AddAppCategory")]
        public async Task<IActionResult> AddAppCategory(string categoryName)
        {
            bool alreadyExists = await _dbContext.AppCategories.AsNoTracking().ContainsAsync(new AppCategory() { CategoryName = categoryName });

            if (alreadyExists)
                return BadRequest();

            AppCategory category = new()
            {
                CategoryName = categoryName
            };

            await _dbContext.AppCategories.AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("RemoveAppCategoryByName")]
        public async Task<IActionResult> RemoveAppCategoryByName(string categoryName)
        {
            AppCategory? category = await _dbContext.AppCategories.SingleOrDefaultAsync(category => category.CategoryName == categoryName);

            if(category == null)
            {
                return NotFound();
            }

            _dbContext.AppCategories.Remove(category);

            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("GetAppCategories")]
        public async IAsyncEnumerable<AppCategory> GetAppCategories()
        {
            IAsyncEnumerable<AppCategory> categories = _dbContext.AppCategories.AsNoTracking().AsAsyncEnumerable();

            await foreach (AppCategory category in categories)
            {
                yield return category;
            }
        }
    }
}

using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppsMarketplaceWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppCategoriesController() : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost("AddAppCategory")]
        public async Task<IActionResult> AddAppCategory(string categoryName, [FromServices] AppDbContext dbContext)
        {
            bool alreadyExists = await dbContext.AppCategories.AsNoTracking().ContainsAsync(new AppCategory() { CategoryName = categoryName });

            if (alreadyExists)
                return BadRequest();

            AppCategory category = new()
            {
                CategoryName = categoryName
            };

            await dbContext.AppCategories.AddAsync(category);
            await dbContext.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("RemoveAppCategoryByName")]
        public async Task<IActionResult> RemoveAppCategoryByName(string categoryName, [FromServices] AppDbContext dbContext)
        {
            AppCategory? category = await dbContext.AppCategories.SingleOrDefaultAsync(category => category.CategoryName == categoryName);

            if(category == null)
            {
                return NotFound();
            }

            dbContext.AppCategories.Remove(category);

            await dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("GetAppCategories")]
        public async IAsyncEnumerable<AppCategory> GetAppCategories([FromServices] AppDbContext dbContext)
        {
            IAsyncEnumerable<AppCategory> categories = dbContext.AppCategories.AsNoTracking().AsAsyncEnumerable();

            await foreach (AppCategory category in categories)
            {
                yield return category;
            }
        }
    }
}

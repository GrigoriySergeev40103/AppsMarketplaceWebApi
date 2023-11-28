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
        public async Task<HttpStatusCode> AddAppCategory(string categoryName)
        {
            AppCategory category = new()
            {
                CategoryName = categoryName
            };

            await _dbContext.AppCategories.AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return HttpStatusCode.OK;
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("RemoveAppCategoryById")]
        public async Task<HttpStatusCode> RemoveAppCategoryById(int categoryId)
        {
            AppCategory? category = await _dbContext.AppCategories.SingleOrDefaultAsync(category => category.CategoryId == categoryId);

            if(category == null)
            {
                return HttpStatusCode.NotFound;
            }

            _dbContext.AppCategories.Remove(category);

            await _dbContext.SaveChangesAsync();
            return HttpStatusCode.OK;
        }

        [HttpGet("GetAppCategories")]
        public IEnumerable<AppCategory> GetAppCategories()
        {
            return _dbContext.AppCategories;
        }
    }
}

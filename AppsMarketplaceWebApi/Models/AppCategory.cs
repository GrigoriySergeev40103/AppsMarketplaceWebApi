using System.ComponentModel.DataAnnotations;

namespace AppsMarketplaceWebApi.Models
{
    public class AppCategory
    {
        [Key]
        public string CategoryName { get; set; } = null!;
    }
}

using Microsoft.AspNetCore.Identity;

namespace AppsMarketplaceWebApi.DTO
{
    public class UserDTO
    {
        public string Id { get; set; } = default!;

        public string? UserName { get; set; }
    }
}

using AppsMarketplaceWebApi;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tusdotnet;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
	.AddIdentityCookies();
builder.Services.AddAuthorizationBuilder();

string? connectionStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionStr == null)
{
	Console.WriteLine("Couldn't find a mysql database connection string in appsettings.json");
	return;
}

builder.Services.AddDbContext<AppDbContext>(
	options => options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr))
	);

builder.Services.AddIdentityCore<User>()
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddApiEndpoints();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapIdentityApi<User>();

app.Map("/promote", async (HttpContext httpContext, RoleManager<IdentityRole> roleManager, UserManager<User> userManager) =>
{
	await userManager.AddToRoleAsync((await userManager.GetUserAsync(httpContext.User))!, "Admin");
}).RequireAuthorization();

app.MapTus("/files", async httpContext => new()
{
	// This method is called on each request so different configurations can be returned per user, domain, path etc.
	// Return null to disable tusdotnet for the current request.

	// Where to store data?
	Store = new tusdotnet.Stores.TusDiskStore(@"C:\dev\AppMarket\apps"),
	Events = new()
	{
		// What to do when file is completely uploaded?
		OnFileCompleteAsync = async eventContext =>
		{
			Console.WriteLine("Tus uploaded!!!");
		}
	}
}).RequireAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
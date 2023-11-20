using AppsMarketplaceWebApi;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
	.AddEntityFrameworkStores<AppDbContext>()
	.AddApiEndpoints();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapIdentityApi<User>();

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

using AppsMarketplaceWebApi;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Text;
using tusdotnet;
using tusdotnet.Helpers;
using tusdotnet.Interfaces;
using tusdotnet.Models;

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

// So we can accept big files through tus protocol
builder.Services.Configure<KestrelServerOptions>(options =>
{
	options.Limits.MaxRequestBodySize = long.MaxValue; // if don't set default value is: 30 MB
});

// So we can accept big files through tus protocol
builder.Services.Configure<FormOptions>(x =>
{
	x.ValueLengthLimit = int.MaxValue;
	x.MultipartBodyLengthLimit = long.MaxValue; // if don't set default value is: 128 MB
	x.MultipartHeadersLengthLimit = int.MaxValue;
});

var app = builder.Build();

app.MapIdentityApi<User>();

// Make user an admin(TEMP!!!)
app.Map("/promote", async (HttpContext httpContext, RoleManager<IdentityRole> roleManager, UserManager<User> userManager) =>
{
	await userManager.AddToRoleAsync((await userManager.GetUserAsync(httpContext.User))!, "Admin");
}).RequireAuthorization();

app.MapTus("/files", async (httpContext) => new()
{
	// This method is called on each request so different configurations can be returned per user, domain, path etc.
	// Return null to disable tusdotnet for the current request.

	// Where to store data?
	Store = new tusdotnet.Stores.TusDiskStore(@"C:\dev\AppMarket\apps"),
	Events = new()
	{
		OnBeforeCreateAsync = ctx =>
		{
			if (!ctx.Metadata.ContainsKey("filename"))
			{
				ctx.FailRequest("name metadata must be specified. ");
			}
			if (!ctx.Metadata.ContainsKey("description"))
			{
				ctx.FailRequest("description metadata must be specified. ");
			}
			if (!ctx.Metadata.ContainsKey("spec_desc"))
			{
				ctx.FailRequest("special description metadata must be specified. ");
			}
			if (!ctx.Metadata.ContainsKey("price"))
			{
				ctx.FailRequest("price metadata must be specified. ");
			}
			
			return Task.CompletedTask;
		},
		OnFileCompleteAsync = async eventContext =>
		{
			ITusFile file = await eventContext.GetFileAsync();
			Dictionary<string, Metadata> metadata = await file.GetMetadataAsync(eventContext.CancellationToken);

			async Task DiscardFile()
			{
				var terminationStore = (ITusTerminationStore)eventContext.Store;
				await terminationStore.DeleteFileAsync(file.Id, eventContext.CancellationToken);
			}

			UserManager<User>? userManager = httpContext.RequestServices.GetService<UserManager<User>>();
			if(userManager == null)
			{
				Console.WriteLine("File was uploaded succesfully but couldn't retrieve user manager to identify uploader");
				await DiscardFile();
				return;
			}

			App toAdd = new();
			User? developer = await userManager.GetUserAsync(eventContext.HttpContext.User);
			if(developer == null)
			{
				Console.WriteLine("File was uploaded succesfully but couldn't retrieve user of the uploaded file");
				await DiscardFile();
				return;
			}
			toAdd.DeveloperId = developer.Id;

			Metadata? valueData;
			string metadataKey = "filename";
			bool hasData = metadata.TryGetValue(metadataKey, out valueData);
			if (hasData)
				toAdd.Name = valueData!.GetString(Encoding.UTF8);
			else
			{
				Console.WriteLine($"File was uploaded succesfully but couldn't retrieve filename(by key {metadataKey}) from the metadata");
				await DiscardFile();
				return;
			}

			metadataKey = "description";
			hasData = metadata.TryGetValue(metadataKey, out valueData);
			if (hasData)
				toAdd.Description = valueData!.GetString(Encoding.UTF8);
			else
			{
				Console.WriteLine($"File was uploaded succesfully but couldn't retrieve description(by key {metadataKey}) from the metadata");
				await DiscardFile();
				return;
			}

			metadataKey = "spec_desc";
			hasData = metadata.TryGetValue(metadataKey, out valueData);
			if (hasData)
				toAdd.SpecialDescription = valueData!.GetString(Encoding.UTF8);
			else
			{
				Console.WriteLine("File was uploaded succesfully but couldn't retrieve " +
					$"special description(by key {metadataKey}) from the metadata");
				await DiscardFile();
				return;
			}

			metadataKey = "price";
			hasData = metadata.TryGetValue(metadataKey, out valueData);
			if (hasData)
			{
				bool parsed = int.TryParse(valueData!.GetString(Encoding.UTF8), out int value);
				if(parsed)
					toAdd.Price = value;
				else
				{
					Console.WriteLine($"File was uploaded succesfully but couldn't parse price(by key {metadataKey}) from the metadata");
					await DiscardFile();
					return;
				}
			}
			else
			{
				Console.WriteLine($"File was uploaded succesfully but couldn't retrieve price(by key {metadataKey}) from the metadata");
				await DiscardFile();
				return;
			}

			metadataKey = "category_id";
			hasData = metadata.TryGetValue(metadataKey, out valueData);
			if (hasData)
			{
				toAdd.CategoryId = int.Parse(valueData!.GetString(Encoding.UTF8));
			}
			else
			{
				Console.WriteLine($"File was uploaded succesfully but couldn't retrieve category id(by key {metadataKey}) from the metadata");
				await DiscardFile();
				return;
			}

			toAdd.UploadDate = DateTime.UtcNow;

			using Stream content = await file.GetContentAsync(eventContext.CancellationToken);

			using (var fileStream = File.Create($"C:\\dev\\AppMarket\\apps\\{toAdd.Name}"))
			{
				content.Seek(0, SeekOrigin.Begin);
				content.CopyTo(fileStream);
			}

			toAdd.Path = $"C:\\dev\\AppMarket\\apps\\{file.Id}";

			AppDbContext? dbContext = httpContext.RequestServices.GetService<AppDbContext>();
			if (dbContext == null)
			{
				Console.WriteLine("ERROR: failed to get database context to add file. Discarding file...");
				await DiscardFile();
				return;
			}

 			await dbContext.Apps.AddAsync(toAdd);
			await dbContext.SaveChangesAsync();
		}
	}
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// so we can call it from localhost? (without it couldn't fetch data from blazor app running on localhost)
app.UseCors(x => x
   .AllowAnyMethod()
   .AllowAnyHeader()
   .SetIsOriginAllowed(origin => true) // allow any origin  
   .AllowCredentials()
   // So tus also works
   .WithExposedHeaders("Upload-Offset", "Location", "Upload-Length", "Tus-Version",
   "Tus-Resumable", "Tus-Max-Size", "Tus-Extension", "Upload-Metadata", "Upload-Defer-Length",
   "Upload-Concat", "Location", "Upload-Offset", "Upload-Length"));

app.MapControllers();

app.Run();
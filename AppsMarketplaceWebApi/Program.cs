using AppsMarketplaceWebApi;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Buffers;
using System.Net.Mail;
using System.Net;
using System.Text;
using tusdotnet;
using tusdotnet.Helpers;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Stores;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// ------------------------------Configure Tus storage------------------------------//
IConfigurationSection tusStoreSection = builder.Configuration.GetSection("TusStoreLocation");
string? tusStorePath = tusStoreSection.Value;

if (tusStorePath == null)
{
	Console.WriteLine("Couldn't find the location for tus protocol to store files in. " +
		"Check if your configuration contains key 'TusStoreLocation' with a value representing location for tus protocol(for example 'C:/tus')");
	return;
}

TusDiskStore tusStore = new(tusStorePath);
builder.Services.AddSingleton(tusStore);
// ------------------------------Configure Tus storage------------------------------//

// ------------------------------Configure MySqlDb------------------------------//
string? connectionStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionStr == null)
{
	Console.WriteLine("Couldn't find a mysql database connection string in appsettings.json");
	return;
}

builder.Services.AddDbContext<AppDbContext>(
	options => options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr))
	);
// ------------------------------Configure MySqlDb------------------------------//

// ------------------------------Configure Authentication and Authorization------------------------------//
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
	.AddIdentityCookies();
builder.Services.AddAuthorizationBuilder();

builder.Services.AddIdentityCore<User>(options =>
{
	options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddApiEndpoints();
// ------------------------------Configure Authentication and Authorization------------------------------//

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ------------------------------Configure so it accepts big tus files------------------------------//
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
// ------------------------------Configure so it accepts big tus files------------------------------//

var app = builder.Build();

// Make user an admin(TEMP!!!)
app.Map("/promote", async (HttpContext httpContext, RoleManager<IdentityRole> roleManager, UserManager<User> userManager) =>
{
	await userManager.AddToRoleAsync((await userManager.GetUserAsync(httpContext.User))!, "Admin");
}).RequireAuthorization();



string? pathToDefaultAppPic = builder.Configuration.GetSection("DefaultAppPicPath").Value;
if (pathToDefaultAppPic == null)
{
	Console.WriteLine("Couldn't retrieve 'DefaultAppPicPath' value from configuration(indicates a full path to a default app picture)");
    return;
}

string? pathToImagesDir = builder.Configuration.GetSection("ImageStore").Value;
if (pathToImagesDir == null)
{
    Console.WriteLine("Couldn't retrieve 'ImageStore' value from configuration(indicates a full path to directory for app to where to store images)");
    return;
}

ConfigurationValues configValues = new(pathToDefaultAppPic, pathToImagesDir);
app.MapTus("/files", async (httpContext) => new()
{
	// This method is called on each request so different configurations can be returned per user, domain, path etc.
	// Return null to disable tusdotnet for the current request.

	// Where to store data?
	Store = tusStore,
	Events = new()
	{
		OnBeforeCreateAsync = async ctx =>
		{
			void FailRequest(string msg)
			{
				Console.WriteLine(msg);
				ctx.FailRequest(msg);
			}

			(string, (string, string))[] stringsKeyErrorPairs =
			[
				// Metadata key | Error message in case metadata value is invalid					| Error message in case metadata entry is missing
				( "app_name",    ("App's name is a zero length string after trimming", "App's name must be specified") ),
				( "description", ("App's description is a zero length string after trimming"        , "App's description metadata must be specified") ),
				( "spec_desc",   ("App's special description is a zero length string after trimming", "The app's special description must be specified") ),
				( "filename",    ("Invalid app name(zero length name after trimming it)"			, "App name metadata must be specified") )
			];

			Metadata? valueData;
			bool hasData;

			for (int i = 0; i < stringsKeyErrorPairs.Length; i++)
			{
				hasData = ctx.Metadata.TryGetValue(stringsKeyErrorPairs[i].Item1, out valueData);
				if (hasData)
				{
					string valueStr = valueData!.GetString(Encoding.UTF8).Trim();

					if (valueStr.Length == 0)
					{
						FailRequest(stringsKeyErrorPairs[i].Item2.Item1);
						return;
					}
				}
				else
				{
					FailRequest(stringsKeyErrorPairs[i].Item2.Item2);
					return;
				}
			}

			hasData = ctx.Metadata.TryGetValue("price", out valueData);
			if (hasData)
			{
				bool parsed = decimal.TryParse(valueData!.GetString(Encoding.UTF8), out decimal appPrice);
				if (parsed)
				{
					if (appPrice < 0)
					{
						FailRequest("App's price can not be < 0");
						return;
					}
				}
				else
				{
					FailRequest("Invalid price value format(couldn't parse to a decimal)");
					return;
				}
			}
			else
			{
				FailRequest("Price metadata must be specified");
				return;
			}

			hasData = ctx.Metadata.TryGetValue("category_name", out valueData);
			if (hasData)
			{
				AppDbContext? dbContext = httpContext.RequestServices.GetService<AppDbContext>();
				if (dbContext == null)
				{
					Console.WriteLine("ERROR: failed to get database context for confirming an incoming upload. Upload will be declined");
					FailRequest("Internal error. Try uploading later");
					return;
				}

				string categoryName = valueData!.GetString(Encoding.UTF8);
				bool categoryExists = await dbContext.AppCategories.AsNoTracking().ContainsAsync(new AppCategory() { CategoryName = categoryName });

				if (!categoryExists)
				{
					FailRequest("Invalid category name(couldn't find category with given name in the database)");
					return;
				}
			}
			else
			{
				FailRequest("App's category must be specified");
				return;
			}

			return;
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

			// TO DO should probably add a check for existing id in database new guid is unlikely to
			//		 collide with existing one but it is not impossible
			// file.Id is generated by Guid.NewGuid()
			toAdd.AppId = Guid.NewGuid().ToString();
			User? developer = await userManager.GetUserAsync(eventContext.HttpContext.User);
			if(developer == null)
			{
				Console.WriteLine("File was uploaded succesfully but couldn't retrieve user of the uploaded file");
				await DiscardFile();
				return;
			}
			toAdd.DeveloperId = developer.Id;

			toAdd.Name				 = metadata["app_name"].GetString(Encoding.UTF8).Trim();
			toAdd.Description		 = metadata["description"].GetString(Encoding.UTF8).Trim();
			toAdd.SpecialDescription = metadata["spec_desc"].GetString(Encoding.UTF8).Trim();
			toAdd.CategoryName		 = metadata["category_name"].GetString(Encoding.UTF8).Trim();
			toAdd.Price				 = decimal.Parse(metadata["price"].GetString(Encoding.UTF8).Trim());

            AppDbContext? dbContext = httpContext.RequestServices.GetService<AppDbContext>();
            if (dbContext == null)
            {
                Console.WriteLine("ERROR: failed to get database context to add file. Discarding file...");
                await DiscardFile();
                return;
            }

			//---------------Set app pic---------------//
			string pathToAppPic = configValues.PathToImagesDir + $"/{toAdd.AppId}.png";

			// Copying default app pic instead of pointing to source because
			// I think it's better than having to check that to be deleted App's pic is default pick or not
			File.Copy(configValues.PathToDefaultAppPic, pathToAppPic);

			toAdd.AppPicturePath = pathToAppPic;
			//---------------Set app pic---------------//

			toAdd.UploadDate = DateTime.UtcNow;

			AppFile appFile = new()
			{
				AppFileId = file.Id,
				AppId = toAdd.AppId,
				Filename = metadata["filename"].GetString(Encoding.UTF8).Trim(),
				Path = tusStorePath + $"/{file.Id}"
			};

			await dbContext.Apps.AddAsync(toAdd);
			await dbContext.AppFiles.AddAsync(appFile);
			await dbContext.SaveChangesAsync();

			eventContext.HttpContext.Response.Headers.Append("AppId", toAdd.AppId);
		}
	}
});

app.MapTus("/append", async (httpContext) => new()
{
	// This method is called on each request so different configurations can be returned per user, domain, path etc.
	// Return null to disable tusdotnet for the current request.

	// Where to store data?
	Store = tusStore,
	Events = new()
	{
		OnBeforeCreateAsync = async ctx =>
		{
			void FailRequest(string msg)
			{
				Console.WriteLine(msg);
				ctx.FailRequest(msg);
			}

			Metadata? valueData;
			bool hasData;

			hasData = ctx.Metadata.TryGetValue("filename", out valueData);
			if (hasData)
			{
				string valueStr = valueData!.GetString(Encoding.UTF8).Trim();

				if (valueStr.Length == 0)
				{
					FailRequest("Filename must be non zero string after trimming.");
					return;
				}
			}
			else
			{
				FailRequest("File's filename must be specified!");
				return;
			}

			hasData = ctx.Metadata.TryGetValue("appId", out valueData);
			if (hasData)
			{
				AppDbContext? dbContext = httpContext.RequestServices.GetService<AppDbContext>();
				if (dbContext == null)
				{
					Console.WriteLine("ERROR: failed to get database context for confirming an incoming upload. Upload will be declined");
					FailRequest("Internal error. Try uploading later");
					return;
				}

				string appId = valueData!.GetString(Encoding.UTF8);
				App? toAppend = await dbContext.Apps.AsNoTracking().SingleOrDefaultAsync(a => a.AppId == appId);

				if (toAppend == null)
				{
					FailRequest("Couldn't find an app with the specified app id");
					return;
				}
			}
			else
			{
				FailRequest("App's id must be specified");
				return;
			}
		},
		OnFileCompleteAsync = async eventContext =>
		{
			ITusFile file = await eventContext.GetFileAsync();
			Dictionary<string, Metadata> metadata = await file.GetMetadataAsync(eventContext.CancellationToken);
			string appId = metadata["appId"].GetString(Encoding.UTF8).Trim();
			string filename = metadata["filename"].GetString(Encoding.UTF8).Trim();

			async Task DiscardFile()
			{
				var terminationStore = (ITusTerminationStore)eventContext.Store;
				await terminationStore.DeleteFileAsync(file.Id, eventContext.CancellationToken);
			}

			UserManager<User>? userManager = httpContext.RequestServices.GetService<UserManager<User>>();
			if (userManager == null)
			{
				Console.WriteLine("File was uploaded succesfully but couldn't retrieve user manager to identify uploader");
				await DiscardFile();
				return;
			}

			AppDbContext? dbContext = httpContext.RequestServices.GetService<AppDbContext>();
			if (dbContext == null)
			{
				Console.WriteLine("ERROR: failed to get database context to add file. Discarding file...");
				await DiscardFile();
				return;
			}

			App? toAppend = await dbContext.Apps.AsNoTracking().SingleOrDefaultAsync(a => a.AppId == appId);
			if (toAppend == null)
			{
				Console.WriteLine("ERROR: failed to find file to append incoming file to. Discarding file...");
				await DiscardFile();
				return;
			}

			AppFile appFile = new()
			{
				AppFileId = file.Id,
				AppId = toAppend.AppId,
				Filename = filename,
				Path = tusStorePath + $"/{file.Id}"
			};

			await dbContext.AppFiles.AddAsync(appFile);
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
   "Upload-Concat", "Location", "Upload-Offset", "Upload-Length", 
   // AppId for communicating back the Id of an app uploaded with tus
   "AppId"));

app.MapControllers();

app.Run();
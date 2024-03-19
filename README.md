# AppsMarketplaceWebApi

A Web Api of an apps marketplace platform built with C#, ASP.NET and Tus protocol(for apps uploading).
Basically an apps marketplace server in a form of a web api.

# Requirements

- .NET 8.0
- MySql Database

You can use any other database though you would have to make a few changes(nothing troublesome).

# Setup

For development of this project I use Visual Studio 2022 with ASP.NET Components installed and a MySql database.

For the app to work you need to change some stuff in appsettings.json:
- Database connection string
```
  "ConnectionStrings": {
    "DefaultConnection": "your_connection_string"
  }
```
- A path pointing to a directory for tus protocol implementation to store uploaded apps to
```
  "TusStoreLocation": "C:/dev/AppMarket/apps"
```
- A path pointing to a directory for app to store users profile's avatar pics and app's pictures
```
  "ImageStore": "C:/dev/AppMarket/images",
```
- A path to a default user's avatar and a path to a default app's pic
```
  "DefaultUserAvatarPath": "C:/dev/AppMarket/images/defaults/profile_png.png",
  "DefaultAppPicPath": "C:/dev/AppMarket/images/defaults/app_icon.png"
```

# Using other databases

If you want to use some other database instead of **MySql** it should be of no problem, all you would have to do is delete MySql Entity framework provider(Not sure if that's the proper name) **Pomelo.EntityFrameworkCore.MySql** and install the EF(entity framework) provider you want(You should be able to do all that from NuGet Package Manager), update the database connection string in the **appsettings.json**(like mentioned in the setup paragraph).
After that you would need to chnage the **Program.cs** a bit:
```
builder.Services.AddDbContext<AppDbContext>(
	options => options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr))
	);
```
Instead of the code above change it to use your prefered database entity framework provider(Or whatever it's called).

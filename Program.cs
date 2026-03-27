using GameVault.Components;
using GameVault.Data;
using GameVault.Data.IGDB;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

// Get MySQl Environment Variables
string mySqlUsername = Environment.GetEnvironmentVariable("MYSQL_USER") ?? string.Empty;
string mySqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? string.Empty;
string mySqlHost = Environment.GetEnvironmentVariable("MYSQL_URL") ?? string.Empty;
string mySqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? string.Empty;
string mySqlDatabase = Environment.GetEnvironmentVariable("MYSQL_DB_NAME") ?? string.Empty;
string mySqlConnectionString = $"server={mySqlHost};port={mySqlPort};database={mySqlDatabase};user={mySqlUsername};password={mySqlPassword};";
Environment.SetEnvironmentVariable("MYSQL_CONNECTION_STRING", mySqlConnectionString);


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddMudServices()
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Register StartupStateService as singleton
builder.Services.AddSingleton(StartupStateService.Instance);

// Register Services
builder.Services
    .AddScoped<IGDBService>()
    .AddScoped<IGDBSyncService>()
    .AddScoped<IGDBPlatformTypeService>()
    .AddScoped<IGDBPlatformFamilyService>();

// Force mysql to use version 8.  Actual check is done in the AppDbContext file
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseMySql(mySqlConnectionString, new MySqlServerVersion(new Version(8,0,0))));



var app = builder.Build();

// Initialize database and check startup health
try
{
    // MySQL
    using IServiceScope scope = app.Services.CreateScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    
    //IGDB
    IGDBService igdbHealth = scope.ServiceProvider.GetRequiredService<IGDBService>();
    bool igdbConnected = await igdbHealth.CanConnectAsync();

    if (!igdbConnected)
    {
        StartupStateService.Instance.IsInitialized = true;
        StartupStateService.Instance.LockedOutBy = LockoutType.IGDB;
        Console.WriteLine("IGDB connection failed");
    }
    else
    {
        StartupStateService.Instance.IsInitialized = true;
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
    StartupStateService.Instance.IsInitialized = true;
    StartupStateService.Instance.LockedOutBy = LockoutType.MySql;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

using JobSearchAssistant.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

var isWindowsService = WindowsServiceHelpers.IsWindowsService();

if (isWindowsService)
{
    builder.WebHost.UseUrls("http://127.0.0.1:5117");
}

var dataDirectory = Path.Combine(
    Environment.GetFolderPath(
        isWindowsService
            ? Environment.SpecialFolder.CommonApplicationData
            : Environment.SpecialFolder.LocalApplicationData),
    "JobSearchAssistant");

Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "jobsearch.db");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddRazorPages();

var app = builder.Build();

if (isWindowsService)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    if (!isWindowsService)
    {
        app.UseHsts();
    }
}

if (!isWindowsService)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

public partial class Program { }
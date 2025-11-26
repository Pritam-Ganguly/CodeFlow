using CodeFlow.core.Data;
using CodeFlow.core.Identity;
using CodeFlow.core.Models;
using CodeFlow.core.Repositories.Seed;
using CodeFlow.Web.Extensions;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
}

builder.Services.AddCodeFlowServices();
builder.Services.AddCustomValidationServices();

builder.Services.AddScoped<IDbConnectionFactory>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<NpgsqlConnectionFactory>>();
    return new NpgsqlConnectionFactory(configuration, logger);
});

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddUserStore<DapperUserStore>()
    .AddRoleStore<DapperRoleStore>()
    .AddDefaultTokenProviders();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(30);
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

using (var scope = app.Services.CreateScope())
{
    var badgeDataSeedService = scope.ServiceProvider.GetRequiredService<BadgeDataSeed>();
    await badgeDataSeedService.SeedBadges();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

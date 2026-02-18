using CodeFlow.core.Data;
using CodeFlow.core.Identity;
using CodeFlow.core.Models;
using CodeFlow.core.Repositories.Seed;
using CodeFlow.Web.Extensions;
using CodeFlow.Web.Hubs;
using CodeFlow.Web.Services;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (string.IsNullOrEmpty(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
}

builder.Services.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
{
    return ConnectionMultiplexer.Connect(redisConnectionString!);
});

builder.Services.AddCodeFlowServices();
builder.Services.AddCustomValidationServices();
builder.Services.AddHttpContextAccessor();

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

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(60);
    options.IOTimeout = TimeSpan.FromSeconds(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminPrivilege", policy => policy.RequireRole("ADMIN"))
    .AddPolicy("RequireModeratorPrivilege", policy => policy.RequireRole("MODERATOR"));

if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<SessionOptions>(options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
}

builder.Services.AddHostedService<CacheWarmupService>();
builder.Services.AddHostedService<ImageCleanupService>();

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
    var serviceProvider = scope.ServiceProvider; 
    var badgeDataSeedService = serviceProvider.GetRequiredService<BadgeDataSeed>();
    await badgeDataSeedService.SeedBadges();
    await UserDataSeed.Initialize(serviceProvider, "AdminPritam@385");
}

app.UseSession();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/notification");

app.Run();

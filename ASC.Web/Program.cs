using ASC.DataAccess;
using ASC.Web.Services;
using ASC.Web.Configuration;
using ASC.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD
builder.Services
    .AddConfig(builder.Configuration)
    .AddMyDependencyGroup();
=======
// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<DbContext, ApplicationDbContext>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddOptions();
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession();

// Add application services
builder.Services.AddTransient<IEmailSender, AuthMessageSender>();
builder.Services.AddTransient<ISmsSender, AuthMessageSender>();

// Add IdentitySeed và UnitOfWork
builder.Services.AddSingleton<IIdentitySeed, IdentitySeed>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddScoped<INavigationCacheOperations, NavigationCacheOperations>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areaRoute",
    pattern: "{area:exists}/{controller=Home}/{action=Index}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Config đưa dữ liệu mẫu từ appsettings.json lên CSDL
using (var scope = app.Services.CreateScope())
{
    var storageSeed = scope.ServiceProvider.GetRequiredService<IIdentitySeed>();
    await storageSeed.Seed(
        scope.ServiceProvider.GetService<UserManager<IdentityUser>>(),
        scope.ServiceProvider.GetService<RoleManager<IdentityRole>>(),
        scope.ServiceProvider.GetService<IOptions<ApplicationSettings>>());
}

// Create navigation cache
using (var scope = app.Services.CreateScope())
{
    var navigationCacheOperations = scope.ServiceProvider.GetRequiredService<INavigationCacheOperations>();
    await navigationCacheOperations.CreateNavigationCacheAsync();
}
// Create Navigation Cache
using (var scope = app.Services.CreateScope())
{
    var navigationCacheOperations = scope.ServiceProvider.GetRequiredService<INavigationCacheOperations>();
    await navigationCacheOperations.CreateNavigationCacheAsync();
}
app.Run();
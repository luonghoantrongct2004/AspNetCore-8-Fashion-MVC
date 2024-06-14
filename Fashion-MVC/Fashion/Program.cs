using App_Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using App_Web.ExtendMethods;
using App_Web.Repository;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped(typeof(IRepo<>), typeof(Repo<>));



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.IsEssential = true;
});
builder.Services.AddRazorPages();
builder.Services.AddAuthentication(options =>
{
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });


/*builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOwnerRole", policy => policy.RequireRole("Owner"));
    options.AddPolicy("RequireSaleRole", policy => policy.RequireRole("Sale"));
    options.AddPolicy("RequireManageRole", policy => policy.RequireRole("Manage"));
    options.AddPolicy("RequireOwnerOrManageRole", policy =>
    {
        policy.RequireRole("Owner", "Manage");
    });
    options.AddPolicy("RequireOwnerOrManageOrSaleRole", policy =>
    {
        policy.RequireRole("Owner", "Manage", "Sale");
    });
});*/
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOwnerRole", policy => policy.Requirements.Add(new CustomAuthorizationRequirement("Owner")));
    options.AddPolicy("RequireAnyRole", policy => policy.Requirements.Add(new CustomAuthorizationRequirement("Owner", "Sale", "Manage")));
    options.AddPolicy("RequireOwnerAndSaleRole", policy => policy.Requirements.Add(new CustomAuthorizationRequirement("Sale", "Owner")));
    options.AddPolicy("RequireOwnerAndManageRole", policy => policy.Requirements.Add(new CustomAuthorizationRequirement("Manage", "Owner")));
});

builder.Services.AddSingleton<AuthorizationService>();
builder.Services.AddScoped<IAuthorizationHandler, CustomAuthorizationHandler>();

var app = builder.Build();

StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
});
app.Run();

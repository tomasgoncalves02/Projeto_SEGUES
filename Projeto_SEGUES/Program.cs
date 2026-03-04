using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Services;
using Projeto_SEGUES.Models.User;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        if (OperatingSystem.IsWindows())
            options.UseSqlServer(builder.Configuration.GetConnectionString("LocalSQLServer"));
        else
            options.UseSqlServer(builder.Configuration.GetConnectionString("DockerSQLServer"));
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSQL"));
    }
});

// Identity
builder.Services.AddIdentity<AppUser, Role>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
    // Password
    options.Password.RequireDigit = true;           
    options.Password.RequireLowercase = true;     
    options.Password.RequireUppercase = true;       
    options.Password.RequireNonAlphanumeric = true; 
    options.Password.RequiredLength = 12;            
    options.Password.RequiredUniqueChars = 4;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options => 
    options.ValidationInterval = TimeSpan.FromMinutes(15)
);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    options.SlidingExpiration = true; // Reset timer on request
    // Redirect unauthenticated users (invalid token/cookie) to the Index page
    options.LoginPath = "/"; 
    // Redirect authenticated users who lack the required role to the Index page
    options.AccessDeniedPath = "/";
});

// Microsoft Authentication
// Use with azure keys
/*
builder.Services.AddAuthentication()
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    });
*/

// Other services
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();

// MVC and Razor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Adiciona isto antes do var app = builder.Build();
builder.Services.AddHttpClient("MbWayClient", client =>
{
    client.BaseAddress = new Uri("https://sandbox.ifthenpay.com/"); // Exemplo
});
QuestPDF.Settings.License = LicenseType.Community;
var app = builder.Build();

// Set localization (first thing after build!)
var supportedCultures = new[] { "pt-PT" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

/*
 * This clears the providers. By default, ASP.NET checks the Browser's Language Header.
 * If the browser is in English, it might try to override.
 * Clearing this list forces the app to use the DefaultCulture (pt-PT) for EVERYONE.
 */
localizationOptions.RequestCultureProviders.Clear();

app.UseRequestLocalization(localizationOptions);
// Rest of pipeline after localization!

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Create the database if it doesn't exist and migrate
        await context.Database.MigrateAsync();
        // Seed initial data
        await Projeto_SEGUES.Data.DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao criar a base de dados ou ao semear os dados iniciais.");
    }
}

// Errors and Security
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
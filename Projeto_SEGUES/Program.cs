using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Context;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using Stripe;
using System.Collections.ObjectModel;
using System.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

string connectionName;
string? password;
if (builder.Environment.IsDevelopment())
{
    bool isWindows = OperatingSystem.IsWindows();
    connectionName = isWindows ? "LocalSQLServer" : "DockerSQLServer";
    password = isWindows ? null : config["Secrets:DockerSQLServerPassword"];
}
else
{
    connectionName = "AzureSQLServer";
    password = config["Secrets:AzureSQLPassword"];
}
string? connectionString = builder.Configuration.GetConnectionString(connectionName);

var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
if (!string.IsNullOrEmpty(password))
{
    connectionStringBuilder.Password = password;
}
if (connectionName == "LocalSQLServer")
{
    // Use Windows Integrated Security for local SQL Server (no password)
    connectionStringBuilder.IntegratedSecurity = true;
    if (connectionStringBuilder.ContainsKey("User ID"))
        connectionStringBuilder.Remove("User ID");
    if (connectionStringBuilder.ContainsKey("Password"))
        connectionStringBuilder.Remove("Password");
}
else
{
    connectionStringBuilder.IntegratedSecurity = false;
}
connectionString = connectionStringBuilder.ConnectionString;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Update global config for Identity
builder.Configuration[$"ConnectionStrings:{connectionName}"] = connectionString;

var columnOptions = new ColumnOptions
{
    AdditionalColumns = new Collection<SqlColumn>
    {
        // Common
        new() { ColumnName = "AppUserId", DataType = SqlDbType.NVarChar, DataLength = 450, AllowNull = true },
        new() { ColumnName = "RequestPath", DataType = SqlDbType.NVarChar, DataLength = 250, AllowNull = true },
        // UserLog
        new() { ColumnName = "UserAction", DataType = SqlDbType.TinyInt, AllowNull = false },
        // ErrorLog
        new() { ColumnName = "Table", DataType = SqlDbType.TinyInt, AllowNull = false },
        new() { ColumnName = "Operation", DataType = SqlDbType.TinyInt, AllowNull = false }
    }
};
columnOptions.Level.StoreAsEnum = true;

builder.Host.UseSerilog((ctx, configuration) =>
    configuration.ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        // UserLog: Log user actions
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(Matching.WithProperty("LogType", "UserAction"))
            .WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions { TableName = "UserLog", AutoCreateSqlTable = false },
                columnOptions: columnOptions
            )
        )
        // ErrorLog: Capture exceptions and database errors
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(Matching.WithProperty("LogType", "Error"))
            .WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions { TableName = "ErrorLog", AutoCreateSqlTable = false },
                columnOptions: columnOptions
            )
        )
        // DbStats: Logging performance and storage metrics
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(Matching.WithProperty("LogType", "DbStats"))
            .WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions { TableName = "DbStats", AutoCreateSqlTable = false },
                columnOptions: columnOptions
            )
        )
        // Default System Logs
        .WriteTo.Logger(lc => lc
            .Filter.ByExcluding(Matching.WithProperty("LogType"))
            .WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions { TableName = "ErrorLog", AutoCreateSqlTable = false },
                columnOptions: columnOptions
            )
        )
);

// Identity
builder.Services.AddIdentity<AppUser, Role>(options =>
{
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
    // Redirect to the Index page after HTMX Request when the user is logged out
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Headers["HX-Request"] == "true")
        {
            ctx.Response.Headers["HX-Redirect"] = ctx.RedirectUri;
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// Microsoft Authentication
// Use with azure keys
/*
builder.Services.AddAuthentication()
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
        options.ClientSecret = password;
    });
*/

// Other services
builder.Services.AddHttpClient();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

//Stripe
StripeConfiguration.ApiKey = builder.Configuration["Secrets:StripeSecretKey"];

// MVC and Razor
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddRazorPages();


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

app.UseSerilogRequestLogging();
app.Use(async (context, next) =>
{
    // Grab the user identity (or set to Anonymous if not logged in)
    string userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
    string requestPath = context.Request.Path;

    // Push these properties to Serilog's context for the duration of the request
    using (LogContext.PushProperty("UserId", userId))
    using (LogContext.PushProperty("RequestPath", requestPath))
    {
        await next();
    }
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Create the database if it doesn't exist and migrate
        await context.Database.MigrateAsync();
        // Seed initial data
        await DbSeeder.SeedInitialDataAsync(services);
    }
    catch (Exception ex)
    {
        var localizer = services.GetService<IStringLocalizer<Errors>>() ?? throw new InvalidOperationException("Localization service not found");
        app.Logger.LogError(ex, localizer[nameof(AppErrors.InternalServerError)], "Error", TableName.All, AppOperation.DatabaseInitialization);
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

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        if (OperatingSystem.IsWindows())
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("LocalSQLServer") ?? 
                throw new InvalidOperationException("Connection string 'LocalSQLServer' not found.")
            );
        else
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DockerSQLServer") ?? 
                throw new InvalidOperationException("Connection string 'DockerSQLServer' not found.")
            );
    }
    else
        options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSQL") ?? throw new InvalidOperationException("Connection string 'AzureSQL' not found."));
});

//builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<AppDbContext>();

// Use <User> pois é o nome da sua classe personalizada
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false; // Mude para false para testar mais rápido
    options.User.RequireUniqueEmail = true;         // Importante para o RF01
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
// Troque isso:
// builder.Services.AddDefaultIdentity<IdentityUser>(...)

// Por isso:
/*builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();*/
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddRazorPages();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapGet("/", context => {
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
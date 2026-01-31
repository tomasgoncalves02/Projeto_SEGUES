using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models;
using Projeto_SEGUES.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Base de Dados
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

// 2. Identity (Configuração relaxada para testes)
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;           
    options.Password.RequireLowercase = true;     
    options.Password.RequireUppercase = true;       
    options.Password.RequireNonAlphanumeric = true; 
    options.Password.RequiredLength = 6;            
    options.Password.RequiredUniqueChars = 1;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. Autenticação Microsoft (COMENTADO PARA EVITAR ERROS DE CONFIGURAÇÃO AGORA)
// Descomentar apenas quando tiveres as chaves do Azure configuradas
/*
builder.Services.AddAuthentication()
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    });
*/



builder.Services.AddTransient<IEmailSender, EmailSender>();


// 5. MVC e Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // ESTA LINHA É ESSENCIAL: Cria as tabelas no Azure se elas não existirem
        await context.Database.MigrateAsync();
        // Chama o método que criámos no DbSeeder.cs
        await Projeto_SEGUES.Data.DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao criar o Admin automático (Seeding).");
    }
}
// ==============================================================

// 6. Pipeline de Erros e Segurança
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



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
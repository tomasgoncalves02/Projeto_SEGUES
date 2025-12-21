using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models;
using Microsoft.AspNetCore.Identity.UI.Services; // <--- NECESSÁRIO
using Projeto_SEGUES.Services; // <--- NECESSÁRIO (para encontrar a classe EmailSender)

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração da Base de Dados
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

// 2. Configuração do Identity (Login)
builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false; // False para não bloquear o login enquanto testas
    options.User.RequireUniqueEmail = true;

    // Podes relaxar a password para testes se quiseres:
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. REGISTO DO EMAIL SENDER (A CORREÇÃO DO ERRO)
// Isto diz ao programa: "Usa a classe EmailSender que está na pasta Services"
builder.Services.AddTransient<IEmailSender, EmailSender>();

// 4. Serviços MVC e Razor Pages
builder.Services.AddControllersWithViews(); // Usei 'WithViews' que é melhor para MVC
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// 5. Pipeline de Erros
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 6. AUTENTICAÇÃO E AUTORIZAÇÃO (A ORDEM É CRÍTICA!)
app.UseAuthentication(); // <--- FALTAVA ESTA LINHA (Sem isto o login não funciona)
app.UseAuthorization();

// 7. Redirecionar a raiz "/" para o Login (Opcional, conforme o teu código original)
app.MapGet("/", context => {
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using System.Security.Cryptography;

namespace Projeto_SEGUES.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IEmailSender _emailSender;

    public AdminService(
        AppDbContext context, 
        UserManager<AppUser> userManager, 
        RoleManager<Role> roleManager, 
        IEmailSender emailSender)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
    }

    /*
     * Create Internal Account
     */
    public async Task<IdentityResult> CreateInternalUserAsync(CreateInternalUserViewModel model)
    {
        // Validate role exists
        if (await _roleManager.FindByNameAsync(model.AccountType) == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Dados inválidos, tente novamente." });
        }
        using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var category = await _context.UserCategory.FirstAsync(c => c.Name == "Externo");
        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            BirthDate = model.BirthDate,
            Balance = 0m,
            CreationDate = DateTime.Now,
            EmailConfirmed = true,
            Status = UserStatus.Active,
            UserCategory = category
        };

        string password = GenerateSecurePassword();
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded) return result;         

            // Adicionar Role
            await _userManager.AddToRoleAsync(user, model.AccountType);
   
            // Se a net falhar aqui, o código salta para o catch
            await SendWelcomeEmailAsync(model.Email, model.FirstName, model.AccountType, password);

            // Só chegamos aqui se o email foi enviado com sucesso
            await transaction.CommitAsync();

            return IdentityResult.Success;
        }
        catch (Exception)
        {
            // Se houve erro de rede no email, desfazemos TUDO o que foi feito na BD
            await transaction.RollbackAsync();

            return IdentityResult.Failed(new IdentityError
            {
                Description = "Erro de conexão: Não foi possível enviar o e-mail de ativação. A conta não foi criada. Verifique a sua ligação à internet."
            });
        }
    }
    public async Task<string> GetBarMenuLinkAsync()
    {
        var config = await _context.AppConfig.FirstOrDefaultAsync();
        return config?.BarLink ?? "https://www.ips.pt";
    }

    public async Task<string> GetRefeitorioMenuLinkAsync()
    {
        var config = await _context.AppConfig.FirstOrDefaultAsync();
        return config?.RefeitorioLink ?? "https://www.ips.pt";
    }

    public async Task UpdateMenuLinksAsync(string refeitorioLink, string barLink)
    {
        var config = await _context.AppConfig.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new AppConfig();
            _context.AppConfig.Add(config);
        }

        config.RefeitorioLink = refeitorioLink;
        config.BarLink = barLink;

        await _context.SaveChangesAsync();
    }

    // Substitui o teu método UpdateBarScheduleAsync por este mais completo:
    public async Task UpdateBarScheduleAsync(string open, string close, string serviceName = "Bar")
    {
        if (!TimeSpan.TryParse(open, out var openTime) || !TimeSpan.TryParse(close, out var closeTime))
        {
            throw new ArgumentException("Formato de hora inválido.");
        }

        var config = await _context.AppConfig.FirstAsync();

        switch (serviceName)
        {
            case "Almoço":
                config.OpenLunchTime = openTime;
                config.CloseLunchTime = closeTime;
                break;
            case "Jantar":
                config.OpenDinnerTime = openTime;
                config.CloseDinnerTime = closeTime;
                break;
            default: // "Bar"
                config.OpenBarTime = openTime;
                config.CloseBarTime = closeTime;
                break;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<TimeSpan> GetOpenLunchTimeAsync() => (await _context.AppConfig.FirstAsync()).OpenLunchTime;
    public async Task<TimeSpan> GetCloseLunchTimeAsync() => (await _context.AppConfig.FirstAsync()).CloseLunchTime;
    public async Task<TimeSpan> GetOpenDinnerTimeAsync() => (await _context.AppConfig.FirstAsync()).OpenDinnerTime;
    public async Task<TimeSpan> GetCloseDinnerTimeAsync() => (await _context.AppConfig.FirstAsync()).CloseDinnerTime;

    private static string GenerateSecurePassword(int length = 12)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*()_+-=[]{}|;:,.?";
        const string allChars = lowercase + uppercase + digits + symbols;
    
        var password = new char[length];
            
        // Ensure at least one of each required character type
        password[0] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[1] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
    
        // Fill remaining characters randomly
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }
    
        // Shuffle the password to randomize position of required characters
        return new string(password.OrderBy(_ => RandomNumberGenerator.GetInt32(length)).ToArray());
    }

    public async Task<List<SelectListItem>> GetNonClientRolesForDropdownAsync()
    {
        var roles = await _roleManager.Roles.Where(r => r.Name != "Client").ToListAsync();
        return roles.Select(r => new SelectListItem { Value = r.Name, Text = r.DisplayName}).ToList();
    }
    
    public async Task<List<SelectListItem>> GetAllRolesForDropdownAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return roles.Select(r => new SelectListItem { Value = r.Name, Text = r.DisplayName}).ToList();
    }
    
    public async Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync()
    {
        var categories = await _context.UserCategory.ToListAsync();
        return categories.Select(c => new SelectListItem { Value = c.Name, Text = c.Name}).ToList();
    }

    private async Task SendWelcomeEmailAsync(string email, string name, string type, string password)
    {
        var roleDisplay = (await _roleManager.FindByNameAsync(type))!.DisplayName;
        string emailBody = ((EmailSender)_emailSender).GetEmailBody(
            "Conta Interna - SEGUES",
            name,
            $"""
             <p>Conta de <strong>{roleDisplay}</strong> criada com sucesso no SEGUES.</p>
             <div style='background:#f8f9fa; padding:15px;'>
                 <p><strong>Email:</strong> {email}</p>
                 <p><strong>Senha:</strong> {password}</p>
                 <p>Faça login e altere sua senha o mais breve possível.</p>
             </div>
             """);
            
            try
            {
                await _emailSender.SendEmailAsync(email, "SEGUES - Bem-vindo", emailBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro crítico de rede no envio de email para {email}: {ex.Message}");
                throw new Exception($"Failed to send welcome email to: {email} after multiple attempts.");
            }

    }  
    
    /*
     * User Management
     */
    public async Task<List<AppUser>> GetFilteredUsersAsync(string? searchString, string? roleFilter, string? categoryFilter)
    {
        // All users
        var query = _userManager.Users.Include(u => u.UserCategory).AsQueryable();

        // Filter users by name or email
        if (!string.IsNullOrEmpty(searchString))
        {
            searchString = searchString.Trim().ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(searchString)
                                     || u.LastName.ToLower().Contains(searchString)
                                     || u.Email!.ToLower().Contains(searchString));
        }
        
        // Role
        if (!string.IsNullOrEmpty(roleFilter))
        {
            roleFilter = roleFilter.Trim();
            var role = await _roleManager.FindByNameAsync(roleFilter);
            if (role == null) return await query.ToListAsync();
                
            var userIdsInRole = _context.UserRoles
                .Where(ur => ur.RoleId == role.Id)
                .Select(ur => ur.UserId);

            query = query.Where(u => userIdsInRole.Contains(u.Id));
        }

        if (string.IsNullOrEmpty(categoryFilter)) return await query.ToListAsync();
        
        // Category
        categoryFilter = categoryFilter.Trim();
        query = query.Where(u => u.UserCategory.Name == categoryFilter);
        return await query.ToListAsync();
    }

    public Task<UserCategory> GetCategoryByNameAsync(string modelCategory)
    { 
        return _context.UserCategory.FirstAsync(c => c.Name == modelCategory);
    }

    /*
     * Ticket Management
     */
    public async Task<List<TicketPrice>> GetTicketPricesAsync()
    {
        return await _context.TicketPrice.Include(tp => tp.UserCategory).ToListAsync();
    }

    public async Task UpdateTicketPricesAsync(List<TicketPrice> prices)
    {
        foreach (var p in prices)
        {
            var dbPrice = await _context.TicketPrice.FindAsync(p.Id);
            if (dbPrice != null)
            {
                if (p.Price > 0)
                {
                    dbPrice.Price = p.Price;
                    dbPrice.EndDatePrice = DateTime.Today.AddDays(1).AddTicks(-1);
                }
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetTicketValidityDaysAsync()
    {
        int days = (await _context.AppConfig.FirstAsync()).TicketValidityDays;
        return days > 0 ? days : 365;
    }
    
    public async Task UpdateTicketValidityDaysAsync(int days)
    {
        var config = await _context.AppConfig.FirstAsync();
        config.TicketValidityDays = days;
        await _context.SaveChangesAsync();
    }



    public async Task<TimeSpan> GetOpenBarTimeAsync()
    {
        TimeSpan time = (await _context.AppConfig.FirstAsync()).OpenBarTime;
        return time;
    }

    public async Task<TimeSpan> GetCloseBarTimesAsync()
    {
        TimeSpan time = (await _context.AppConfig.FirstAsync()).CloseBarTime;
        return time;
    }

    public async Task UpdateBarScheduleAsync(string openBarTime, string closeBarTime)
    {
        if (!TimeSpan.TryParse(openBarTime, out var open) || !TimeSpan.TryParse(closeBarTime, out var close))
        {
            throw new ArgumentException("Formato de hora inválido.");
        }
        var config = await _context.AppConfig.FirstAsync();
        config.OpenBarTime = open;
        config.CloseBarTime = close;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsBarOpenAsync(TimeSpan? requestedTime = null)
    {
        var config = await _context.AppConfig.FirstAsync();
        var timeToCheck = requestedTime ?? DateTime.Now.TimeOfDay;
    
        if (config.OpenBarTime <= config.CloseBarTime)
        {
            return timeToCheck >= config.OpenBarTime && timeToCheck <= config.CloseBarTime;
        }

        return timeToCheck >= config.OpenBarTime || timeToCheck <= config.CloseBarTime;
    }




}
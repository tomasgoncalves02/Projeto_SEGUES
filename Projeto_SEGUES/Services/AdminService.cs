using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums;

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
        
        // InternalUsers are External to IPS
        var category = await _context.UserCategories.FirstAsync(c => c.Name == "Externo");
        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            BirthDate =  model.BirthDate,
            Balance = 0m,
            CreationDate = DateTime.Now,
            EmailConfirmed = true, // Internal accounts are created by admins, so we can consider their email as confirmed by default
            Status = UserStatus.Active,
            UserCategory = category
        };

        string password = GenerateSecurePassword();
        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.AccountType);
            await SendWelcomeEmailAsync(model.Email, model.FirstName, model.AccountType, password);
        }

        return result;
    }
    
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
        var categories = await _context.UserCategories.ToListAsync();
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
        await _emailSender.SendEmailAsync(email, "SEGUES - Bem-vindo", emailBody);
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
        return _context.UserCategories.FirstAsync(c => c.Name == modelCategory);
    }

    /*
     * Ticket Management
     */
    public async Task<List<TicketPrice>> GetTicketPricesAsync()
    {
        return await _context.TicketPrices.Include(tp => tp.UserCategory).ToListAsync();
    }

    public async Task UpdateTicketPricesAsync(List<TicketPrice> prices)
    {
        foreach (var p in prices)
        {
            var dbPrice = await _context.TicketPrices.FindAsync(p.Id);
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
        int days = (await _context.AppConfigs.FirstAsync()).TicketValidityDays;
        return days > 0 ? days : 365;
    }
    
    public async Task UpdateTicketValidityDaysAsync(int days)
    {
        var config = await _context.AppConfigs.FirstAsync();
        config.TicketValidityDays = days;
        await _context.SaveChangesAsync();
    }
}
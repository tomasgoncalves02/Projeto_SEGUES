using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminService(
        AppDbContext context, 
        UserManager<AppUser> userManager, 
        RoleManager<Role> roleManager, 
        IEmailSender emailSender, 
        LinkGenerator linkGenerator,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
        _linkGenerator = linkGenerator;
        _httpContextAccessor = httpContextAccessor;
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
        var category = await _context.UserCategories.FirstOrDefaultAsync(c => c.Name == "Externo");
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
            EmailConfirmed = false, // Must reset password using link sent to email on first login
            Status = UserStatus.Active,
            UserCategory = category!
        };

        string password = GenerateSecurePassword(12);
        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.AccountType);
            await SendWelcomeEmailAsync(model.Email, model.FirstName, model.AccountType);
        }

        return result;
    }
    
    private static string GenerateSecurePassword(int length = 12)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";
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
        return categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name}).ToList();
    }
    
    private async Task SendWelcomeEmailAsync(string email, string name, string type)
    {
        var roleDisplay = (await _roleManager.FindByNameAsync(type))!.DisplayName;
        var user = (await _userManager.FindByEmailAsync(email))!;
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(
            await _userManager.GeneratePasswordResetTokenAsync(user)
        ));
        var httpContext = _httpContextAccessor.HttpContext!;
        string callbackUrl = _linkGenerator.GetUriByPage(
            httpContext,
            page: "/Account/ResetPassword",
            values: new { area = "Identity", email, code },
            scheme: httpContext.Request.Scheme)!;
        string emailBody = ((EmailSender)_emailSender).GetEmailBody(
            "Redefinir Senha - SEGUES",
            name,
            $"""
             <p>Conta de <strong>{roleDisplay}</strong> criada com sucesso no SEGUES.</p>
             <div style='background:#f8f9fa; padding:15px;'>
                 <p><strong>Email:</strong> {email}</p>
                 <p>Por favor, defina sua senha clicando no link abaixo:</p>
                 <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='display:inline-block; margin-top:10px; padding:10px 20px; background:#009697; color:white; text-decoration:none; border-radius:5px;'>Definir Senha</a>
                 <p>Se o botão não funcionar, copia e cola o seguinte link no teu navegador:</p>
                 <p class='text-color-ips' style='word-break: break-all; font-size: 12px;'>{callbackUrl}</p>
             </div>
             """);
        await _emailSender.SendEmailAsync(email, "SEGUES - Bem-vindo", emailBody);
    }
    
    /*
     * User Management
     */
    public async Task<List<AppUser>> GetFilteredUsersAsync(string? searchString, string? roleFilter)
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
        
        // Role filter
        if (string.IsNullOrWhiteSpace(roleFilter)) return await query.ToListAsync();
        
        roleFilter = roleFilter.Trim();
        if (roleFilter is "Admin" or "Employee")
        {
            var role = await _roleManager.FindByNameAsync(roleFilter);
            if (role == null) return await query.ToListAsync();
                
            var userIdsInRole = _context.UserRoles
                .Where(ur => ur.RoleId == role.Id)
                .Select(ur => ur.UserId);

            query = query.Where(u => userIdsInRole.Contains(u.Id));
        }
        else if (int.TryParse(roleFilter, out int categoryId))
        {
            query = query.Where(u => u.UserCategory.Id == categoryId);
            
            // Exclude Admins and Employees from category filter
            var excludedRoleIds = await _roleManager.Roles
                .Where(r => r.Name == "Admin" || r.Name == "Employee")
                .Select(r => r.Id)
                .ToListAsync();
            var excludedUserIds = _context.UserRoles
                .Where(ur => excludedRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId);
            query = query.Where(u => !excludedUserIds.Contains(u.Id));
        }
        
        return await query.ToListAsync();
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
        foreach (var price in prices)
        {
            _context.TicketPrices.Update(price);
        }
        await _context.SaveChangesAsync();
    }
}
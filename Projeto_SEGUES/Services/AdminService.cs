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

        string password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12));
        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.AccountType);
            await SendWelcomeEmailAsync(model.Email, model.FirstName, model.AccountType);
        }

        return result;
    }

    public async Task<List<SelectListItem>> GetRolesForDropdownAsync()
    {
        var roles = await _roleManager.Roles.Where(r => r.Name != "Client").ToListAsync();
        return roles.Select(r => new SelectListItem { Value = r.Name, Text = r.DisplayName}).ToList();
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
    public async Task<List<AppUser>> GetFilteredUsersAsync(string roleFilter, string searchString)
    {
        var query = _userManager.Users.Include(u => u.UserCategory).AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(u => u.FirstName.Contains(searchString, StringComparison.CurrentCultureIgnoreCase)
                                     || u.LastName.Contains(searchString, StringComparison.CurrentCultureIgnoreCase)
                                     || u.Email!.Contains(searchString, StringComparison.CurrentCultureIgnoreCase));
        }

        var users = await query.ToListAsync();

        if (string.IsNullOrEmpty(roleFilter)) return users;
        
        // If role filter
        var filtered = new List<AppUser>();
        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, roleFilter)) filtered.Add(user);
        }
        return filtered;
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
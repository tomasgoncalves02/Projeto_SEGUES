using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using System.Security.Cryptography;
using System.Text;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Audit;
using Projeto_SEGUES.Resources;

namespace Projeto_SEGUES.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AdminService> _logger;
    private readonly IStringLocalizer<Errors> _localizer;
    private readonly IUserService _userService;

    public AdminService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<Role> roleManager,
        IEmailSender emailSender,
        ILogger<AdminService> logger,
        IStringLocalizer<Errors> localizer,
        IUserService userService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
        _logger = logger;
        _localizer = localizer;
        _userService = userService;
    }
    
    #region General
    
    private async Task<AppConfig> GetAppConfigAsync()
    {
        return await _context.AppConfig.FirstAsync();
    }
    
    #endregion
    
    #region Internal User Creation
    
    public async Task<ServiceResult> CreateInternalUserAsync(CreateInternalUserViewModel model)
    {
        // Validate role exists
        if (await _roleManager.FindByNameAsync(model.AccountType) == null)
        {
            _logger.LogError(
                Errors.ResourceManager.GetString(nameof(AppErrors.DataNotFoundError), System.Globalization.CultureInfo.InvariantCulture)
                , "Error", TableName.Identity, AppOperation.Create);
            return ServiceResult.Fail(_localizer[nameof(AppErrors.DataNotFoundError)].Value);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var category = await _context.UserCategory.FirstAsync(c => c.Name == "Externo");
        AppUser user;
        if (model.AccountType == "Employee")
        {
            user = new Employee
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
        }
        else
        {
            user = new AppUser
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
        }

        string password = GenerateSecurePassword();
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded) return ServiceResult.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));
        
        await _userManager.AddToRoleAsync(user, model.AccountType);
        try
        {
            // If net fails, throws exception
            await SendWelcomeEmailAsync(model.Email, model.FirstName, model.AccountType, password);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogAppError(AppErrors.SendActivationEmailError, TableName.All, AppOperation.Other, ex);
            return ServiceResult.Fail(AppErrors.SendActivationEmailError.GetViewErrorMessage());
        }
        await transaction.CommitAsync();
        return ServiceResult.Ok($"Conta criada para {model.FirstName}!");
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
    
    private async Task SendWelcomeEmailAsync(string email, string name, string type, string password)
    {
        var roleDisplay = (await _roleManager.FindByNameAsync(type))!.DisplayName;
        string emailBody = ((EmailSender) _emailSender).GetEmailBody(
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
        
        // Throws exception if email fails to send
        await _emailSender.SendEmailAsync(email, "SEGUES - Bem-vindo", emailBody);
    }
    
    #endregion
    
    #region User Management
    
    private async Task<List<UserDto>> MapUsersToDtoAsync(List<AppUser> users, List<Role> roles)
    {
        if (users.Count == 0) return [];
        
        var userIds = users.Select(u => u.Id).ToList();
        var userRoleMapping = await _context.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .ToDictionaryAsync(ur => ur.UserId, ur => roles.First(r => r.Id == ur.RoleId));
        
        return users.Select(user =>
        {
            var categoryName = user.UserCategory.Name;
            var role = userRoleMapping[user.Id];
            return new UserDto
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email!,
                
                RoleName = role.DisplayName,
                RoleBadgeClass = role.Name.ToBadgeClass(),
                
                CategoryName = categoryName,
                CategoryBadgeClass = categoryName.ToBadgeClass(),
                
                IsActive = user.Status == UserStatus.Active,
                BalanceFormatted = user.Balance.ToString("C")
            };
        }).ToList();
    }
    
    public async Task<List<UserDto>> GetFilteredUsersAsync(string? searchString = null, string? roleFilter = null, string? categoryFilter = null)
    {
        // All users
        var query = _userManager.Users
            .Include(u => u.UserCategory)
            .AsNoTracking()
            .AsQueryable();
        
        // Roles
        var roles = await _roleManager.Roles.ToListAsync();
        
        // Filter users by name or email
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim().ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(searchString)
                                     || u.LastName.ToLower().Contains(searchString)
                                     || u.Email!.ToLower().Contains(searchString));
        }

        // Role
        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            var role = roles.FirstOrDefault(r => r.Name == roleFilter.Trim());
            if (role != null)
            {
                var userIdsInRole = _context.UserRoles
                    .Where(ur => ur.RoleId == role.Id)
                    .Select(ur => ur.UserId);
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
        }

        // Category
        if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            query = query.Where(u => u.UserCategory.Name == categoryFilter.Trim());
        }
        
        var users = await query.ToListAsync();
        return await MapUsersToDtoAsync(users, roles);
    }

    public Task<UserCategory?> GetCategoryByNameAsync(string modelCategory)
    {
        return _context.UserCategory.FirstOrDefaultAsync(c => c.Name == modelCategory);
    }

    public Task<Role?> GetRoleByNameAsync(string roleName)
    {
        return _roleManager.FindByNameAsync(roleName);
    }
    
    public async Task RequestEmailChangeAsync(AppUser user, string newEmail, IUrlHelper urlHelper, string scheme)
    {
        // Create token
        var code = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var codeEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        // Create the confirmation link
        var callbackUrl = urlHelper.Page(
            "/Account/ConfirmEmailChange",
            pageHandler: null,
            values: new { area = "Identity", userId = user.Id, email = newEmail, code = codeEncoded },
            protocol: scheme)!;

        // Template
        string title = "Alteração de Email - SEGUES";
        string content = $"""
            <p>Foi solicitada uma alteração do endereço de email da sua conta para: <strong>{newEmail}</strong>.</p>
            <p>Para confirmar esta alteração, clique no botão abaixo:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{callbackUrl}' style='background-color: #009697; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Confirmar Novo Email</a>
            </div>
        """;
        
        var emailSenderService = _emailSender as EmailSender;
        string finalBody = emailSenderService?.GetEmailBody(title, user.FirstName, content) ?? content;
        
        // Throws exception if email fails to send
        await _emailSender.SendEmailAsync(newEmail, "SEGUES - Confirmação de Email", finalBody);
    }

    public async Task<ServiceResult> UpdateUserAdminAsync(AppUser user, EditUserAdminViewModel model, IUrlHelper url, string scheme)
    {
        // Check duplicated Email
        string? pendingEmail = null;
        if (model.Email != user.Email)
        {
            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null) return ServiceResult.Fail("Este email já está em uso.");
            pendingEmail = model.Email;
        }
        
        // Update profile
        var result = await _userService.UpdateUserProfileAsync(user, new EditUserViewModel
        {
            Id = user.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            BirthDate = model.BirthDate,
            FiscalNumber = model.FiscalNumber,
            Address = model.Address,
            City = model.City,
            PostalCode = model.PostalCode,
            SchoolId = (user is Student ? model.SchoolId : (user is Employee ? model.SchoolId : null)),
            StudentNumber = (user is Student ? model.StudentNumber : null)
        });
        if (!result.Success) return result;
        
        // Role Description and category
        if (!string.IsNullOrWhiteSpace(model.RoleDescription) && user is Employee e) e.RoleDescription = model.RoleDescription;
        user.UserCategory = (await GetCategoryByNameAsync(model.Category)) ?? user.UserCategory;
        await _context.SaveChangesAsync();
        
        // Role
        var oldRoles = await _userManager.GetRolesAsync(user);
        if (model.Role != oldRoles.First())
        {
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
            await _userManager.UpdateSecurityStampAsync(user);
        }
        
        // Email
        if (!string.IsNullOrWhiteSpace(pendingEmail))
        {
            try
            {
                await RequestEmailChangeAsync(user, pendingEmail, url, scheme);
                return ServiceResult.Ok("Utilizador atualizado! O link de confirmação foi enviado para o novo e-mail.");
            }
            catch (Exception ex)
            {
                _logger.LogAppError(AppErrors.EmailSenderError, TableName.User, AppOperation.Other, ex);
                return ServiceResult.Fail("Utilizador salvo, mas ocorreu um erro ao enviar o email de confirmação.");
            }
        }
        
        return ServiceResult.Ok("Utilizador atualizado com sucesso.");
    }

    public async Task<List<UserLog>> GetStaffLogFilteredAsync(string? searchString = null, string? dateFilter = null)
    {
        var employeeIds = (await _userManager.GetUsersInRoleAsync("Employee")).Select(u => u.Id).ToList();
        
        var query = _context.UserLog
            .AsNoTracking()
            .Include(l => l.AppUser)
            .Where(l => l.AppUser != null && employeeIds.Contains(l.AppUser.Id))
            .AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim().ToLower();
            query = query.Where(l => 
                l.AppUser!.UserName.ToLower().Contains(searchString) || 
                l.Message.ToLower().Contains(searchString) || 
                l.AppUser.FirstName.ToLower().Contains(searchString) || 
                l.AppUser.LastName.ToLower().Contains(searchString));
        }
        
        if (!string.IsNullOrWhiteSpace(dateFilter) && DateTime.TryParse(dateFilter, out DateTime parsedDate))
        {
            query = query.Where(l => l.TimeStamp.Date == parsedDate.Date);
        }
        
        return await query.OrderByDescending(l => l.TimeStamp).ToListAsync();
    }
    
    // Dropdowns
    
    public async Task<List<SelectListItem>> GetNonClientRolesForDropdownAsync()
    {
        var roles = await _roleManager.Roles.Where(r => r.Name != "Client").ToListAsync();
        return roles.Select(r => new SelectListItem { Value = r.Name, Text = r.DisplayName }).ToList();
    }

    public async Task<List<SelectListItem>> GetAllRolesForDropdownAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return roles.Select(r => new SelectListItem { Value = r.Name, Text = r.DisplayName }).ToList();
    }

    public async Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync()
    {
        var categories = await _context.UserCategory.ToListAsync();
        return categories.Select(c => new SelectListItem { Value = c.Name, Text = c.Name }).ToList();
    }
    
    #endregion
    
    #region Bar and Canteen Configuration

    public async Task<BarCanteenConfigViewModel> GetMenuLinksAsync()
    {
        var config = await GetAppConfigAsync();
        return new BarCanteenConfigViewModel
        {
            BarMenuLink = config.BarLink,
            CanteenMenuLink = config.CanteenLink
        };
    }
    
    public async Task UpdateMenuLinksAsync(string? canteenLink, string? barLink)
    {
        var config = await GetAppConfigAsync();
        config.CanteenLink = canteenLink ?? config.CanteenLink;
        config.BarLink = barLink ?? config.BarLink;
        await _context.SaveChangesAsync();
    }
    
    public async Task<BarCanteenConfigViewModel> GetScheduleAsync()
    {
        var config = await GetAppConfigAsync();
        return new BarCanteenConfigViewModel {
            BarOpeningTime = config.BarOpeningTime,
            BarOpeningTimeString = config.BarOpeningTime.ToString(@"hh\:mm"),
            BarClosingTime = config.BarClosingTime,
            BarClosingTimeString = config.BarClosingTime.ToString(@"hh\:mm"),
            BarMenuLink = config.BarLink,
            CanteenLunchOpeningTime = config.CanteenLunchOpeningTime,
            CanteenLunchOpeningTimeString = config.CanteenLunchOpeningTime.ToString(@"hh\:mm"),
            CanteenLunchClosingTime = config.CanteenLunchClosingTime,
            CanteenLunchClosingTimeString = config.CanteenLunchClosingTime.ToString(@"hh\:mm"),
            CanteenDinnerOpeningTime = config.CanteenDinnerOpeningTime,
            CanteenDinnerOpeningTimeString = config.CanteenDinnerOpeningTime.ToString(@"hh\:mm"),
            CanteenDinnerClosingTime = config.CanteenDinnerClosingTime,
            CanteenDinnerClosingTimeString = config.CanteenDinnerClosingTime.ToString(@"hh\:mm"),
            CanteenMenuLink = config.CanteenLink,
            IsOpenSaturday = config.IsOpenSaturday,
            IsOpenSunday = config.IsOpenSunday
        };
    }

    public async Task<ServiceResult> UpdateSpecificDayStatusAsync(string day, bool isOpen)
    {
        try
        {
            var config = await GetAppConfigAsync();

            switch (day.ToLower())
            {
                case "saturday":
                    config.IsOpenSaturday = isOpen;
                    break;
                case "sunday":
                    config.IsOpenSunday = isOpen;
                    break;
                default:
                    return ServiceResult.Fail("Dia da semana inválido.");
            }

            await _context.SaveChangesAsync();

            string dayTranslated = day.ToLower() == "saturday" ? "Sábado" : "Domingo";
            string state = isOpen ? "aberto" : "fechado";

            return ServiceResult.Ok($"{dayTranslated} está agora {state} para pedidos.");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.DatabaseUpdateError, TableName.AppConfig, AppOperation.Update, ex);
            return ServiceResult.Fail("Erro ao guardar a alteração no servidor.");
        }
    }

    public async Task<ServiceResult> UpdateScheduleAsync(BarCanteenConfigViewModel model)
    {
        var config = await GetAppConfigAsync();
        
        if (model is { BarOpeningTime: not null, BarClosingTime: not null })
        {
            if (model.BarOpeningTime == model.BarClosingTime)
                return ServiceResult.Fail("A hora de abertura e de fecho não podem ser iguais.");
            if (model.BarOpeningTime > model.BarClosingTime)
                return ServiceResult.Fail("A hora de fecho não pode ser anterior à hora de abertura.");
            if (model.BarClosingTime - model.BarOpeningTime < TimeSpan.FromHours(1))
                return ServiceResult.Fail("O bar deve estar aberto pelo menos 1 hora.");
        }
        else if (model is { CanteenLunchOpeningTime: not null, CanteenLunchClosingTime: not null })
        {
            if (model.CanteenLunchOpeningTime == model.CanteenLunchClosingTime)
                return ServiceResult.Fail("A hora de abertura e de fecho não podem ser iguais.");
            if (model.CanteenLunchOpeningTime > model.CanteenLunchClosingTime)
                return ServiceResult.Fail("A hora de fecho não pode ser anterior à hora de abertura.");
            if (model.CanteenLunchClosingTime - model.CanteenLunchOpeningTime < TimeSpan.FromHours(1))
                return ServiceResult.Fail("A cantina deve estar aberta pelo menos 1 hora para almoço.");
        }
        else if (model is { CanteenDinnerOpeningTime: not null, CanteenDinnerClosingTime: not null })
        {
            if (model.CanteenDinnerOpeningTime == model.CanteenDinnerClosingTime)
                return ServiceResult.Fail("A hora de abertura e de fecho não podem ser iguais.");
            if (model.CanteenDinnerOpeningTime > model.CanteenDinnerClosingTime)
                return ServiceResult.Fail("A hora de fecho não pode ser anterior à hora de abertura.");
            if (model.CanteenDinnerClosingTime - model.CanteenDinnerOpeningTime < TimeSpan.FromHours(1))
                return ServiceResult.Fail("A cantina deve estar aberta pelo menos 1 hora para jantar.");
        }
        
        config.BarOpeningTime = model.BarOpeningTime ?? config.BarOpeningTime;
        config.BarClosingTime = model.BarClosingTime ?? config.BarClosingTime;
        config.CanteenLunchOpeningTime = model.CanteenLunchOpeningTime ?? config.CanteenLunchOpeningTime;
        config.CanteenLunchClosingTime = model.CanteenLunchClosingTime ?? config.CanteenLunchClosingTime;
        config.CanteenDinnerOpeningTime = model.CanteenDinnerOpeningTime ?? config.CanteenDinnerOpeningTime;
        config.CanteenDinnerClosingTime = model.CanteenDinnerClosingTime ?? config.CanteenDinnerClosingTime;
        
        await _context.SaveChangesAsync();
        return ServiceResult.Ok("Horario de funcionamento alterado com sucessso");
    }

    public async Task<bool> IsBarOpenAsync(TimeSpan? requestedTime)
    {
        if (requestedTime == null) return false;

        var config = await GetAppConfigAsync();
        var today = DateTime.Now.DayOfWeek;

        if (today == DayOfWeek.Saturday && !config.IsOpenSaturday) return false;
        if (today == DayOfWeek.Sunday && !config.IsOpenSunday) return false;

        return requestedTime >= config.BarOpeningTime && requestedTime <= config.BarClosingTime;
    }

    #endregion

    #region Ticket Management

    public async Task<List<TicketPrice>> GetTicketPricesAsync()
    {
        return await _context.TicketPrice
            .Include(tp => tp.UserCategory)
            .Where(tp => tp.EndDatePrice == null || tp.EndDatePrice > DateTime.Today)
            .GroupBy(tp => tp.UserCategory.Id)
            .Select(group => group.OrderByDescending(tp => tp.InitialDatePrice).First())
            .ToListAsync();
    }
    
    public async Task<ServiceResult> UpdateTicketPricesAsync(List<TicketPriceUpdateDto> prices)
    {
        var currentPrices = await GetTicketPricesAsync();

        try
        {
            foreach (var p in prices)
            {
                if (p.Price <= 0) continue;
                
                var dbPrice = currentPrices.FirstOrDefault(tp => tp.Id == p.Id);
                if (dbPrice == null || dbPrice.Price == p.Price) continue;
                
                dbPrice.EndDatePrice = DateTime.Now;
                _context.TicketPrice.Add(new TicketPrice
                {
                    UserCategory = dbPrice.UserCategory,
                    Price = p.Price,
                    InitialDatePrice = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Preços atualizados com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.PricingNotAvailable, TableName.TicketPrice, AppOperation.Update, ex);
            return ServiceResult.Fail(AppErrors.PricingNotAvailable.GetViewErrorMessage());
        }
    }
    
    public async Task<int> GetTicketValidityDaysAsync()
    {
        var config = await GetAppConfigAsync();
        int days = config.TicketValidityDays;
        return days > 0 ? days : 365;
    }
    
    public async Task<ServiceResult> UpdateTicketValidityDaysAsync(int days)
    {
        if (days <= 0) return ServiceResult.Fail("O prazo de validade das senhas deve ser maior que zero.");
        
        try
        {
            var config = await GetAppConfigAsync();
            config.TicketValidityDays = days;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Prazo de validade das senhas atualizado com sucesso. Só se aplica a senhas emitidas a partir de agora.");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.PricingNotAvailable, TableName.TicketPrice, AppOperation.Update, ex);
            return ServiceResult.Fail(AppErrors.PricingNotAvailable.GetViewErrorMessage());
        }
    }
    
    #endregion
}
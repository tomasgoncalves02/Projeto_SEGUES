using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Service implementation for managing user-related operations.
/// Handles profile updates, school management, and specialized user data 
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Identity user manager.</param>
    /// <param name="context">The primary database context.</param>
    /// <param name="logger">The application logger.</param>
    public UserService(UserManager<AppUser> userManager, AppDbContext context, ILogger<UserService> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a list of all registered schools to populate dropdown menus.
    /// </summary>
    /// <returns>A list of <see cref="SelectListItem"/> containing school names and IDs.</returns>
    public async Task<List<SelectListItem>> GetSchoolsAsync()
    {
        var schools = await _context.School.ToListAsync();
        return schools.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToList();
    }

    /// <summary>
    /// Fetches a user by ID with all related category, school, and location data.
    /// Uses polymorphic includes to handle specific data for Student or Employee subclasses.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The <see cref="AppUser"/> entity or null if not found.</returns>
    public async Task<AppUser?> GetUserForEditAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        return await _userManager.Users
            .Include(u => u.UserCategory)
            .Include(u => u.PostalCode)
            .Include(u => (u as Student)!.School)
            .Include(u => (u as Employee)!.School)
            .Include(u => (u as WorkerIps)!.School)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    /// Updates the user's profile information. 
    /// Manages the creation of new PostalCode records if they don't exist and 
    /// handles specific logic for Student numbers and institutional affiliations.
    /// </summary>
    /// <param name="user">The current user entity to update.</param>
    /// <param name="model">The view model containing the new profile data.</param>
    /// <returns>A <see cref="ServiceResult"/> indicating success or containing error details.</returns>
    public async Task<ServiceResult> UpdateUserProfileAsync(AppUser user, EditUserViewModel model)
    {
        // Update user profile
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Gender = model.Gender;
        user.BirthDate = model.BirthDate;
        user.FiscalNumber = model.FiscalNumber;
        user.Address = model.Address;
        user.City = model.City;

        // Manage PostalCode entity relation
        if (!string.IsNullOrWhiteSpace(model.PostalCode))
        {
            var postalCode = await _context.PostalCode.FirstOrDefaultAsync(p => p.Code == model.PostalCode);
            if (postalCode == null)
            {
                postalCode = _context.PostalCode.Add(new PostalCode { Code = model.PostalCode }).Entity;
                await _context.SaveChangesAsync();
                user.PostalCode = postalCode;
            }
            user.PostalCode = postalCode;
        }
        else
        {
            user.PostalCode = null;

            // Also clear FK if present (ensures EF will set DB column to NULL)
            var fkProp = user.GetType().GetProperty("PostalCodeId");
            if (fkProp != null && fkProp.PropertyType == typeof(int?))
            {
                fkProp.SetValue(user, null);
            }
        }

        // Manage School entity relation
        School? selectedSchool = null;
        if (model.SchoolId.HasValue)
        {
            selectedSchool = await _context.School.FindAsync(model.SchoolId);
        }
        else
        {
            var fkProp = user.GetType().GetProperty("SchoolId");
            if (fkProp != null && fkProp.PropertyType == typeof(int?))
            {
                fkProp.SetValue(user, null);
            }
        }
        
        // Polymorphic logic for specialized user types
        if (user is Student studentUser)
        {
            studentUser.School = selectedSchool;
            studentUser.StudentNumber = model.StudentNumber ?? studentUser.Email!.ToLower().Split('@')[0];
        }
        else if (user is Employee employeeUser)
        {
            employeeUser.School = selectedSchool;
        }
        else if (user is WorkerIps workerUser)
        {
            workerUser.School = selectedSchool;
        }

        // Persistence via Identity UserManager
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            _logger.LogAppUser($"User {user.Email} profile updated.", UserAction.Update);
            return ServiceResult.Ok("Perfil atualizado com sucesso!");
        }

        _logger.LogAppUser($"Error updating {user.Email} profile. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}", UserAction.Update);
        return ServiceResult.Fail("Erro ao atualizar o perfil.");
    }
}
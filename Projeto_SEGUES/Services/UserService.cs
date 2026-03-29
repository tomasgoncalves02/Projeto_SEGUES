using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(UserManager<AppUser> userManager, AppDbContext context, ILogger<UserService> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }
    
    public async Task<List<SelectListItem>> GetSchoolsAsync()
    {
        var schools = await _context.School.ToListAsync();
        return schools.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToList();
    }

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
        
        if (user is Student studentUser)
        {
            studentUser.School = selectedSchool;
            studentUser.StudentNumber = model.StudentNumber ?? studentUser.Email!.ToLower().Split('@')[0];
        }
        else if (user is Employee employeeUser)
        {
            employeeUser.School = selectedSchool;
        }
        
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
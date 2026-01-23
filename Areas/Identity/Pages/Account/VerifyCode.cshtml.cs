using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Projeto_SEGUES.Models;
using System; // Necessário para DateTime
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Text.Json;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Areas.Identity.Pages.Account
{
    public class VerifyCodeModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public VerifyCodeModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string UserEmailDisplay { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Introduza o código.")]
            public string Code { get; set; }
        }

        
        private class TempUserData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Gender Gender { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string Code { get; set; }
            public DateTime ExpiryTime { get; set; }
        }

        public IActionResult OnGet()
        {
            if (TempData["RegistrationData"] is string jsonData)
            {
                var data = JsonSerializer.Deserialize<TempUserData>(jsonData);
                UserEmailDisplay = data.Email;
                TempData.Keep("RegistrationData");
                return Page();
            }
            return RedirectToPage("Register");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!(TempData["RegistrationData"] is string jsonData))
            {
                return RedirectToPage("Register");
            }
            TempData.Keep("RegistrationData");

            var data = JsonSerializer.Deserialize<TempUserData>(jsonData);
            UserEmailDisplay = data.Email;

            if (!ModelState.IsValid) return Page();

           
            if (DateTime.Now > data.ExpiryTime)
            {
              
                TempData.Remove("RegistrationData");
                ModelState.AddModelError("", "O código expirou (limite de 5 minutos). Por favor registe-se novamente.");

               

                return Page();
            }

            
            if (Input.Code != data.Code)
            {
                ModelState.AddModelError("", "Código incorreto. Tente novamente.");
                return Page();
            }

            

            var user = new User
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                Gender = data.Gender,
                UserName = data.Email,
                Email = data.Email,
                Balance = 0m,
                CreationDate = DateTime.Now,
                Status = UserStatus.Active,
                EmailConfirmed = true
            };

            string roleNameString = "ExternalEmployee";
            UserRole roleEnum = UserRole.External;

            if (data.Email.ToLower().Contains("@estudantes."))
            {
                roleNameString = "Student";
                roleEnum = UserRole.Student;
            }
            user.Role = roleEnum;

            var result = await _userManager.CreateAsync(user, data.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(roleNameString))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleNameString));
                }
                await _userManager.AddToRoleAsync(user, roleNameString);
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData.Remove("RegistrationData");

                if (roleNameString == "Student")
                    return RedirectToAction("Index", "/");
                else
                    return RedirectToAction("Index", "/");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
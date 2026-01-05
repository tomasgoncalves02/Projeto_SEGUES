using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models;
using Projeto_SEGUES.Models.Enums; 
using System.Text.RegularExpressions;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Mostrar o formulário de criação
        public IActionResult CreateInternalAccount()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInternalAccount(CreateInternalUserViewModel model)
        {
            if (ModelState.IsValid)
            {
               
                model.UsernameStub = model.UsernameStub.Replace("@", "").Trim();

                string emailDomain = "";
                UserRole roleEnum;
                string identityRole = "";

               
                if (model.AccountType == "Admin")
                {
                    emailDomain = "@admin.com";
                    roleEnum = UserRole.Admin;
                    identityRole = "Admin";
                }
                else if (model.AccountType == "Teacher") 
                {
                    emailDomain = "@docente.com";
                    roleEnum = UserRole.Teacher;
                    identityRole = "Teacher";
                }
                else 
                {
                    emailDomain = "@func.com";
                    roleEnum = UserRole.Employee;
                    identityRole = "Employee";
                }

              
                var user = new User
                {
                    UserName = model.UsernameStub + emailDomain,
                    Email = model.UsernameStub + emailDomain,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    BirthDate = model.BirthDate,
                    Gender = model.Gender,
                    Role = roleEnum, // Define o Enum
                    Balance = 0,
                    CreationDate = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                   
                    await _userManager.AddToRoleAsync(user, identityRole);

                    TempData["Success"] = "Conta criada com sucesso!";

                  
                    return RedirectToAction("ListUsers");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
        // Página inicial do Admin (Dashboard)
        public IActionResult Index()
        {
            return View();
        }
       
        // 2. AÇÃO ATUALIZADA PARA A LISTA COM PESQUISA
        public async Task<IActionResult> ListUsers(string roleFilter, string searchString)
        {
            // 1. Começamos com a Query base
            var usersQuery = _userManager.Users.AsQueryable();

            // 2. Filtro por Nome ou Email (Texto)
            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => u.FirstName.Contains(searchString)
                                                || u.LastName.Contains(searchString)
                                                || u.Email.Contains(searchString));
            }

            // 3. Filtro por Perfil (Dropdown)
            if (!string.IsNullOrEmpty(roleFilter))
            {
                if (Enum.TryParse(typeof(Projeto_SEGUES.Models.Enums.Enums.UserRole), roleFilter, out var roleEnum))
                {
                    var roleValue = (Projeto_SEGUES.Models.Enums.Enums.UserRole)roleEnum;
                    usersQuery = usersQuery.Where(u => u.Role == roleValue);
                }
            }

            // 4. Guardamos os filtros na ViewData para a View os mostrar nos campos
            ViewData["CurrentFilter"] = roleFilter;
            ViewData["SearchString"] = searchString;

            // 5. Executa a query
            var users = await usersQuery.ToListAsync();
            return View(users);
        }

        // ============================================================
        // 1. DETAILS (Detalhes)
        // ============================================================
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Vamos buscar a Role do utilizador para mostrar na view
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault() ?? "Sem Role";

            return View(user);
        }

        // ============================================================
        // 2. EDIT (Editar)
        // ============================================================
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Balance = user.Balance,
                Role = userRoles.FirstOrDefault() // Assume que só tem 1 role principal
            };

            // Enviar lista de Roles para o Dropdown
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
         
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

        
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Balance = model.Balance;

           

            
            int roleId = int.Parse(model.Role);

           
            var roleEnum = (Projeto_SEGUES.Models.Enums.Enums.UserRole)roleId;
            user.Role = roleEnum;

          
            string roleName = roleEnum.ToString();
     

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
            

                var oldRoles = await _userManager.GetRolesAsync(user);

                
                if (oldRoles.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(user, oldRoles);
                }

               
                if (!string.IsNullOrEmpty(roleName))
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }

                TempData["Success"] = "Utilizador atualizado com sucesso!";
                return RedirectToAction("ListUsers");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }


        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["Success"] = "Utilizador apagado com sucesso.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
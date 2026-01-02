using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models;
using Projeto_SEGUES.Models.Enums; // Para aceder aos Enums
using System.Text.RegularExpressions;
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Controllers
{
    [Authorize(Roles = "Admin")] // <--- SÓ O ADMIN ENTRA AQUI
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
                // 1. LIMPEZA DO ARROBA (Isto faz passar o teste "DeveLimparArroba...")
                // Se o user escrever "rui@santos", transforma em "ruisantos"
                model.UsernameStub = model.UsernameStub.Replace("@", "").Trim();

                string emailDomain = "";
                UserRole roleEnum;
                string identityRole = "";

                // 2. LÓGICA DAS ROLES (Isto faz passar o teste "DeveCriarDocente...")
                if (model.AccountType == "Admin")
                {
                    emailDomain = "@admin.com";
                    roleEnum = UserRole.Admin;
                    identityRole = "Admin";
                }
                else if (model.AccountType == "Teacher") // <--- O Código do Docente
                {
                    emailDomain = "@docente.com";
                    roleEnum = UserRole.Teacher;
                    identityRole = "Teacher";
                }
                else // Funcionario
                {
                    emailDomain = "@func.com";
                    roleEnum = UserRole.Employee;
                    identityRole = "Employee";
                }

                // Criar o utilizador
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
                    // Adiciona a Role do Identity
                    await _userManager.AddToRoleAsync(user, identityRole);

                    TempData["Success"] = "Conta criada com sucesso!";

                    // 3. REDIRECIONAMENTO (Isto faz passar o teste do Redirect)
                    // Tem de ser "ListUsers" para bater certo com o teste
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

        // 2. NOVA AÇÃO PARA A LISTA
        public async Task<IActionResult> ListUsers()
        {
            var users = await _userManager.Users.ToListAsync();
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
            // Carrega as roles para o caso de dar erro e ter de voltar à página
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // ============================================================
            // 1. ATUALIZAR DADOS BÁSICOS
            // ============================================================
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Balance = model.Balance;

            // ============================================================
            // 2. ATUALIZAR O ENUM (Para o crachá aparecer correto na lista)
            // ============================================================
            // Isto garante que se escolheres "Teacher", ele fica com UserRole.Teacher
            switch (model.Role)
            {
                case "Admin":
                    user.Role = UserRole.Admin;
                    break;
                case "Teacher":
                    user.Role = UserRole.Teacher; 
                    break;
                case "Employee":
                    user.Role = UserRole.Employee;
                    break;
                case "Student":
                    user.Role = UserRole.Student;
                    break;
                default:
                    user.Role = UserRole.ExternalEmployee;
                    break;
            }

            // Grava as alterações na tabela de Utilizadores (User + Enum)
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // ============================================================
                // 3. ATUALIZAR A ROLE DO IDENTITY (Permissões de Login)
                // ============================================================
                var oldRoles = await _userManager.GetRolesAsync(user);

                // Remove as roles antigas
                if (oldRoles.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(user, oldRoles);
                }

                // Adiciona a nova role (ex: "Teacher")
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                TempData["Success"] = "Utilizador atualizado com sucesso!";
                return RedirectToAction("ListUsers"); // Redireciona para a lista
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ============================================================
        // 3. DELETE (Apagar)
        // ============================================================
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
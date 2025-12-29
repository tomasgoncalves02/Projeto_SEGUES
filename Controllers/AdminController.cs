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

        // POST: Receber os dados e criar a conta
        [HttpPost]
        public async Task<IActionResult> CreateInternalAccount(CreateInternalUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // ============================================================
                // 1. LIMPEZA (ALTERADO)
                // ============================================================
                // Agora permitimos tudo (pontos, traços, etc.) MENOS o '@'.
                // O .Trim() remove apenas espaços acidentais no início ou fim.

                string cleanName = model.UsernameStub.Replace("@", "").Trim();

                // Se depois de limpar ficar vazio, damos erro
                if (string.IsNullOrWhiteSpace(cleanName))
                {
                    ModelState.AddModelError("UsernameStub", "O identificador não pode estar vazio.");
                    return View(model);
                }

                // Nota: Se o utilizador escrever espaços no meio (ex: "joao silva"), 
                // o Identity vai dar erro mais à frente a dizer que o email é inválido, 
                // o que é o comportamento correto.

                // ============================================================
                // 2. GERAR O EMAIL AUTOMÁTICO
                // ============================================================
                string emailDomain = "";
                UserRole roleEnum;
                string identityRole = "";

                if (model.AccountType == "Admin")
                {
                    emailDomain = "@admin.com";
                    roleEnum = UserRole.Admin;
                    identityRole = "Admin";
                }
                else // Funcionario
                {
                    emailDomain = "@func.com";
                    roleEnum = UserRole.Employee;
                    identityRole = "Employee";
                }

                // Junta o nome limpo com o domínio
                string finalEmail = cleanName + emailDomain;

                // ============================================================
                // 3. CRIAR O UTILIZADOR
                // ============================================================
                var user = new User
                {
                    UserName = finalEmail,
                    Email = finalEmail,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Gender = model.Gender,
                    Role = roleEnum,
                    Status = UserStatus.Active,
                    CreationDate = DateTime.Now,
                    Balance = 0m,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 4. ATRIBUIR A ROLE
                    if (!await _roleManager.RoleExistsAsync(identityRole))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(identityRole));
                    }
                    await _userManager.AddToRoleAsync(user, identityRole);

                    TempData["Success"] = $"Conta criada com sucesso: {finalEmail}";
                    return RedirectToAction("Index", "Admin");
                }

                // Se houver erros (ex: email inválido por ter espaços, ou duplicado)
                // eles aparecem aqui automaticamente.
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
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Atualizar dados básicos
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email; // Mantém o username igual ao email
            user.Balance = model.Balance;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Atualizar a Role (Remove a antiga e mete a nova)
                var oldRoles = await _userManager.GetRolesAsync(user);
                if (oldRoles.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(user, oldRoles);
                }

                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                TempData["Success"] = "Utilizador atualizado com sucesso!";
                return RedirectToAction(nameof(Index)); // Assume que tens uma lista Index
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
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
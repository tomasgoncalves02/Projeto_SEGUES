using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    }
}
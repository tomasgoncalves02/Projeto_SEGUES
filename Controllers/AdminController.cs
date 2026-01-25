using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models;
using Projeto_SEGUES.Models.Enums;
using System.Text.RegularExpressions;
using static Projeto_SEGUES.Models.Enums.Enums;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Projeto_SEGUES.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context, IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
        }

        // ============================================================
        // CRIAR CONTA INTERNA
        // ============================================================

        [HttpGet]
        public IActionResult CreateInternalAccount()
        {
            return View(new CreateInternalUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInternalAccount(CreateInternalUserViewModel model)
        {
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.BirthDate));
            ModelState.Remove(nameof(model.UsernameStub));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este endereço de email já está registado.");
                return View(model);
            }

            UserRole roleEnum = model.AccountType == "Admin" ? UserRole.Admin : UserRole.Employee;
            string identityRole = model.AccountType == "Admin" ? "Admin" : "Employee";
            string temporaryPassword = GenerateSecurePassword();

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender, // Grava o género na criação
                Role = roleEnum,
                Balance = 0,
                CreationDate = DateTime.Now,
                EmailConfirmed = true,
                Status = UserStatus.Active
            };

            var result = await _userManager.CreateAsync(user, temporaryPassword);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, identityRole);

                if (roleResult.Succeeded)
                {
                    await EnviarEmailBoasVindas(model.Email, model.FirstName, temporaryPassword, model.AccountType);

                    TempData["Success"] = $"Conta de {model.FirstName} criada! A senha foi enviada para {model.Email}.";
                    ModelState.Clear();
                    return View(new CreateInternalUserViewModel());
                }

                foreach (var error in roleResult.Errors) ModelState.AddModelError("", "Erro na Role: " + error.Description);
            }
            else
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ============================================================
        // MÉTODOS AUXILIARES
        // ============================================================
        private string GenerateSecurePassword()
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string nums = "0123456789";
            const string specials = "!@#$%^&*";
            var r = new Random();
            return $"{lower[r.Next(26)]}{upper[r.Next(26)]}{nums[r.Next(10)]}{specials[r.Next(8)]}" +
                   Guid.NewGuid().ToString("n").Substring(0, 8);
        }

        private async Task EnviarEmailBoasVindas(string email, string name, string password, string type)
        {
            string roleDisplay = type == "Admin" ? "Administrador" : "Funcionário";
            string emailBody = $@"
                <div style='font-family: sans-serif; border-top: 6px solid #009697; padding: 20px;'>
                    <h2 style='color: #009697;'>Olá, {name}!</h2>
                    <p>A tua conta de <strong>{roleDisplay}</strong> no SEGUES foi criada.</p>
                    <p>Credenciais:</p>
                    <p><strong>Email:</strong> {email}<br>
                       <strong>Senha:</strong> <span style='color: #009697; font-weight: bold;'>{password}</span></p>
                    <br>
                    <a href='{Request.Scheme}://{Request.Host}/Identity/Account/Login' 
                       style='background:#009697; color:white; padding:10px 20px; text-decoration:none; border-radius:5px;'>Login</a>
                </div>";
            await _emailSender.SendEmailAsync(email, "SEGUES - Conta Criada", emailBody);
        }

        // ============================================================
        // AÇÕES DE GESTÃO (Index, List, Edit, Delete)
        // ============================================================

        public async Task<IActionResult> Index()
        {
            var prices = await _context.TicketPrices.ToListAsync();
            if (!prices.Any())
            {
                var initialPrices = new List<TicketPrice>
                {
                    new TicketPrice { TicketType = TicketType.Student, Price = 2.90m, InitialDatePrice = DateTime.Now, EndDatePrice = DateTime.Now.AddYears(1) },
                    new TicketPrice { TicketType = TicketType.IPSWorker, Price = 5.20m, InitialDatePrice = DateTime.Now, EndDatePrice = DateTime.Now.AddYears(1) },
                    new TicketPrice { TicketType = TicketType.External, Price = 5.50m, InitialDatePrice = DateTime.Now, EndDatePrice = DateTime.Now.AddYears(1) }
                };
                _context.TicketPrices.AddRange(initialPrices);
                await _context.SaveChangesAsync();
                prices = initialPrices;
            }
            ViewBag.Prices = prices;
            var tickets = await _context.Tickets.Include(t => t.Owner).OrderByDescending(t => t.PurchaseDate).ToListAsync();
            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrices(List<TicketPrice> updatedPrices)
        {
            if (ModelState.IsValid)
            {
                foreach (var price in updatedPrices) _context.TicketPrices.Update(price);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Preçário atualizado com sucesso!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ListUsers(string roleFilter, string searchString)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => u.FirstName.Contains(searchString) || u.LastName.Contains(searchString) || u.Email.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                if (Enum.TryParse(typeof(UserRole), roleFilter, out var roleEnum))
                {
                    var roleValue = (UserRole)roleEnum;
                    usersQuery = usersQuery.Where(u => u.Role == roleValue);
                }
            }

            ViewData["CurrentFilter"] = roleFilter;
            ViewData["SearchString"] = searchString;

            return View(await usersQuery.ToListAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault() ?? "Sem Role";
            return View(user);
        }

        // --------------------------------------------------------
        // EDITAR UTILIZADOR (GET) - Atualizado com GÉNERO
        // --------------------------------------------------------
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
                Gender = user.Gender, // <--- LÊ O GÉNERO DA BD
                Balance = user.Balance,
                Role = userRoles.FirstOrDefault()
            };
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        // --------------------------------------------------------
        // EDITAR UTILIZADOR (POST) - Atualizado com GÉNERO
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            // Método auxiliar para não repetir código caso a validação falhe
            void PrepareRoles()
            {
                ViewBag.Roles = Enum.GetNames(typeof(UserRole))
                    .Where(r => r != "ExternalEmployee") // Remove o nome antigo
                    .Select(r => new
                    {
                        Id = r,
                        Name = r switch
                        {
                            "Admin" => "Administrador",
                            "Employee" => "Funcionário",
                            "IPSWorker" => "TrabalhadorIPS",
                            "External" => "Externo",
                            "Student" => "Estudante",
                            _ => r
                        }
                    }).ToList();
            }

            if (!ModelState.IsValid)
            {
                PrepareRoles();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // 1. Atualizar propriedades básicas
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Balance = model.Balance;
            user.Gender = model.Gender;

            // 2. Atualizar o Enum 'Role' na tabela de Users
            // O model.Role traz a string (ex: "IPSWorker")
            if (Enum.TryParse(typeof(UserRole), model.Role, out var newRoleEnum))
            {
                user.Role = (UserRole)newRoleEnum;
            }

            // Gravar alterações no objeto User
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // 3. Atualizar as Roles do Identity (Tabela AspNetUserRoles)
                if (!string.IsNullOrEmpty(model.Role))
                {
                    // Garante que a Role existe no sistema Identity antes de atribuir
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    }

                    var oldRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, oldRoles);
                    await _userManager.AddToRoleAsync(user, model.Role); // Aqui atribui "IPSWorker", "Admin", etc.
                }

                TempData["Success"] = "Utilizador atualizado com sucesso!";
                return RedirectToAction("ListUsers");
            }

            // Se houver erros do Identity (ex: email já existe)
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);

            PrepareRoles();
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
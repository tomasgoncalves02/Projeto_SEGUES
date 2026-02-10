using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.ViewModels;

namespace Projeto_SEGUES.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public AdminController(UserManager<AppUser> userManager, RoleManager<Role> roleManager, AppDbContext context, IEmailSender emailSender)
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
            // TODO: remove form view model
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

            var category = await _context.UserCategories.FirstOrDefaultAsync(c => c.Name == model.AccountType);

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender,
                Balance = 0,
                CreationDate = DateTime.Now,
                EmailConfirmed = true,
                Status = UserStatus.Active,
                UserCategory = category!
            };

            string temporaryPassword = GenerateSecurePassword();
            var result = await _userManager.CreateAsync(user, temporaryPassword);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, model.AccountType);

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
            var prices = await _context.TicketPrices.Include(tp => tp.UserCategory).ToListAsync();
            ViewBag.Prices = prices;
            
            var tickets = await _context.Tickets
                .Include(t => t.Owner)
                .OrderByDescending(t => t.TicketPurchase.TransactionDate)
                .ToListAsync();
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
            var usersQuery = _userManager.Users.Include(u => u.UserCategory).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => u.FirstName.Contains(searchString) || u.LastName.Contains(searchString) || u.Email.Contains(searchString));
            }
            
            var users = await usersQuery.ToListAsync();
            
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var usersInRole = new List<AppUser>();
                foreach (var user in users)
                {
                    if (await _userManager.IsInRoleAsync(user, roleFilter)) usersInRole.Add(user);
                }
                users = usersInRole;
            }

            ViewData["CurrentFilter"] = roleFilter;
            ViewData["SearchString"] = searchString;

            return View(users);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.Users.Include(u => u.UserCategory).FirstOrDefaultAsync(u => u.Id == id);
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
            
            var roles = await _userManager.GetRolesAsync(user);

            // TODO: update viewmodel
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Gender = user.Gender,
                Balance = user.Balance,
                Role = roles.FirstOrDefault() ?? ""
            };

            await PrepareRoles(); // Chama o método auxiliar para preencher a ViewBag
            return View(model);
        }

        // --------------------------------------------------------
        // EDITAR UTILIZADOR (POST) - Atualizado com GÉNERO
        // --------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PrepareRoles();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Balance = model.Balance;
            user.Gender = model.Gender;
            
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var oldRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, oldRoles);
                await _userManager.AddToRoleAsync(user, model.Role);

                TempData["Success"] = "Utilizador atualizado com sucesso!";
                return RedirectToAction("ListUsers");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            await PrepareRoles();
            return View(model);
        }

        private async Task PrepareRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.DisplayName
                }).ToList();
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
            return RedirectToAction(nameof(ListUsers));
        }
    }
}
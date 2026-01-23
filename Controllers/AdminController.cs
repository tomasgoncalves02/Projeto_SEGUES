using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models;
using Projeto_SEGUES.Models.Enums; 
using System.Text.RegularExpressions;
using static Projeto_SEGUES.Models.Enums.Enums;
using System.Text; // Necessário para o StringBuilder
using System.Threading.Tasks; // Necessário para o Task
using Microsoft.AspNetCore.Identity.UI.Services; // Necessário para o IEmailSender

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

        [HttpGet]
        public IActionResult CreateInternalAccount()
        {
            return View(new CreateInternalUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInternalAccount(CreateInternalUserViewModel model)
        {
            // 1. IMPORTANTE: Ignorar campos que não estão no formulário
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.BirthDate));
            ModelState.Remove(nameof(model.UsernameStub)); // Se já não usas stubs

            if (!ModelState.IsValid)
            {
                // Se entrar aqui, é porque falta algum campo obrigatório no ViewModel
                return View(model);
            }

            // 2. Verificar se o email já existe
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este endereço de email já está registado.");
                return View(model);
            }

            // 3. Mapeamento de Roles (Admin ou Employee)
            UserRole roleEnum = model.AccountType == "Admin" ? UserRole.Admin : UserRole.Employee;
            string identityRole = model.AccountType == "Admin" ? "Admin" : "Employee";

            // 4. Gerar Password (Aumentada para 12 chars para evitar erros de política)
            string temporaryPassword = GenerateSecurePassword();

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = roleEnum,
                Balance = 0,
                CreationDate = DateTime.Now,
                EmailConfirmed = true
            };

            // 5. Tentar criar a conta
            var result = await _userManager.CreateAsync(user, temporaryPassword);

            if (result.Succeeded)
            {
                // 6. Atribuir a Role (Verifica se estas roles existem na BD!)
                var roleResult = await _userManager.AddToRoleAsync(user, identityRole);

                if (roleResult.Succeeded)
                {
                    // 7. Enviar Email (Se o registo normal envia, este também enviará)
                    await EnviarEmailBoasVindas(model.Email, model.FirstName, temporaryPassword, model.AccountType);

                    TempData["Success"] = $"Sucesso! Conta criada e senha enviada para {model.Email}.";
                    return RedirectToAction("ListUsers");
                }

                // Se falhar a Role, adicionamos o erro
                foreach (var error in roleResult.Errors) ModelState.AddModelError("", "Erro na Role: " + error.Description);
            }
            else
            {
                // 8. Se falhou a criação (ex: password fraca), mostra o erro exato
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        // Password mais robusta para passar em qualquer validação
        private string GenerateSecurePassword()
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string nums = "0123456789";
            const string specials = "!@#$%^&*";
            var r = new Random();

            // Garante um de cada tipo + aleatórios
            return $"{lower[r.Next(26)]}{upper[r.Next(26)]}{nums[r.Next(10)]}{specials[r.Next(8)]}" +
                   Guid.NewGuid().ToString("n").Substring(0, 8);
        }

        private async Task EnviarEmailBoasVindas(string email, string name, string password, string type)
        {
            string roleDisplay = type == "Admin" ? "Administrador" : (type == "Teacher" ? "Docente" : "Funcionário");

            string emailBody = $@"
                <div style='font-family: sans-serif; border-top: 6px solid #009697; padding: 20px;'>
                    <h2 style='color: #009697;'>Olá, {name}!</h2>
                    <p>A tua conta de <strong>{roleDisplay}</strong> no SEGUES foi criada.</p>
                    <p>Utiliza as seguintes credenciais para o teu primeiro acesso:</p>
                    <p><strong>Email:</strong> {email}<br>
                       <strong>Senha Temporária:</strong> <span style='color: #009697; font-weight: bold;'>{password}</span></p>
                    <br>
                    <a href='{Request.Scheme}://{Request.Host}/Identity/Account/Login' 
                       style='background:#009697; color:white; padding:10px 20px; text-decoration:none; border-radius:5px;'>Fazer Login</a>
                </div>";

            await _emailSender.SendEmailAsync(email, "SEGUES - A tua conta foi criada", emailBody);
        }
        // Página inicial do Admin (Dashboard)
        public async Task<IActionResult> Index()
        {
            // 1. Procurar preços no domínio (Parte 3)
            var prices = await _context.TicketPrices.ToListAsync();

            // 2. Se a tabela estiver vazia, inicializamos com as tuas Roles do Enum
            if (!prices.Any())
            {
                var initialPrices = new List<TicketPrice>
        {
            new TicketPrice { TicketType = TicketType.Student, Price = 2.90m, InitialDatePrice = DateTime.Now, EndDatePrice = DateTime.Now.AddYears(1) },
            new TicketPrice { TicketType = TicketType.DocenteNaoDocente, Price = 5.20m, InitialDatePrice = DateTime.Now, EndDatePrice = DateTime.Now.AddYears(1) },
            new TicketPrice { TicketType = TicketType.External, Price = 5.50m, InitialDatePrice = DateTime.Now, EndDatePrice = DateTime.Now.AddYears(1) }
        };
                _context.TicketPrices.AddRange(initialPrices);
                await _context.SaveChangesAsync();
                prices = initialPrices;
            }

            ViewBag.Prices = prices;

            // 3. Carregar auditoria de senhas para a Parte 2 (Interação)
            var tickets = await _context.Tickets.Include(t => t.Owner).OrderByDescending(t => t.PurchaseDate).ToListAsync();
            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrices(List<TicketPrice> updatedPrices)
        {
            if (ModelState.IsValid)
            {
                foreach (var price in updatedPrices)
                {
                    // Atualiza os valores que o Admin definiu na interface
                    _context.TicketPrices.Update(price);
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Preçário atualizado com sucesso!";
            }
            return RedirectToAction(nameof(Index));
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
        [HttpPost]
       
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

            // Converte o ID do Dropdown para o Enum correto
            if (int.TryParse(model.Role, out int roleId))
            {
                var roleEnum = (UserRole)roleId;
                user.Role = roleEnum;
                string roleName = roleEnum.ToString(); // Ex: "External" ou "DocenteNaoDocente"

                // Garante que a Role existe na BD antes de tentar associar
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var oldRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, oldRoles);
                    await _userManager.AddToRoleAsync(user, roleName);

                    TempData["Success"] = "Utilizador atualizado com sucesso!";
                    return RedirectToAction("ListUsers");
                }
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

        /*public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            // 1. Procuramos e apagamos manualmente as senhas do utilizador primeiro
            var userTickets = _context.Tickets.Where(t => t.OwnerId == id);
            _context.Tickets.RemoveRange(userTickets);

            // 2. Só agora apagamos o utilizador
            await _userManager.DeleteAsync(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("ListUsers");
        }*/
    }
}
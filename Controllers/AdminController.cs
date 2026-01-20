using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
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
        private readonly AppDbContext _context;


        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;

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
                    emailDomain = "@estsetubal.ips.pt";
                    roleEnum = UserRole.DocenteNaoDocente;
                    identityRole = "DocenteNaoDocente";
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

                if (emailDomain == "@admin.com" || emailDomain == "@func.com")
                {
                    user.EmailConfirmed = true;
                }

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
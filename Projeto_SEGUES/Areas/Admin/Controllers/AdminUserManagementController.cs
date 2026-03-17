using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsável pela gestão de utilizadores na área administrativa.
/// </summary>
/// <remarks>
/// Este controlador permite listar, detalhar, editar, ativar e desativar contas de utilizadores, 
/// além de gerir as permissões (roles), categorias e visualizar logs de auditoria do staff.
/// </remarks>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminUserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;
    private readonly AppDbContext _context;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de Identity, administração e contexto de dados.
    /// </summary>
    /// <param name="userManager">Serviço nativo do ASP.NET Identity para gestão de utilizadores.</param>
    /// <param name="adminService">Serviço personalizado com lógica de negócio administrativa.</param>
    /// <param name="context">Contexto da base de dados para consultas diretas (ex: Logs).</param>
    public AdminUserManagementController(UserManager<AppUser> userManager, IAdminService adminService, AppDbContext context)
    {
        _userManager = userManager;
        _adminService = adminService;
        _context = context;
    }

    /// <summary>
    /// Lista os utilizadores do sistema com suporte a pesquisa e filtros.
    /// </summary>
    /// <param name="searchString">Termo de pesquisa (nome ou email).</param>
    /// <param name="roleFilter">Filtro por tipo de função (Admin, Staff, Client).</param>
    /// <param name="categoryFilter">Filtro por categoria de utilizador (Aluno, Docente, etc.).</param>
    /// <returns>A View de índice com a coleção de utilizadores filtrada.</returns>
    public async Task<IActionResult> Index(string? searchString, string? roleFilter, string? categoryFilter)
    {
        var users = await _adminService.GetFilteredUsersAsync(searchString, roleFilter, categoryFilter);
        ViewData["SearchString"] = searchString;
        ViewData["CurrentRole"] = roleFilter;
        ViewData["CurrentCategory"] = categoryFilter;

        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
        return View(users);
    }

    /// <summary>
    /// Apresenta os detalhes completos de um utilizador específico.
    /// </summary>
    /// <param name="id">Identificador único (GUID) do utilizador.</param>
    /// <returns>A View de detalhes ou NotFound caso o utilizador não exista.</returns>
    /// <remarks>
    /// Traduz enums e estados para português e define classes CSS dinâmicas para a interface.
    /// </remarks>
    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.UserCategory)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var userRoleRaw = roles.FirstOrDefault() ?? "Client";
        var allRoles = await _adminService.GetAllRolesForDropdownAsync();

        ViewBag.UserRole = allRoles.Find(r => r.Value == userRoleRaw)?.Text ?? userRoleRaw;
        ViewBag.UserRoleRaw = userRoleRaw;

        ViewBag.GenderPT = user.Gender switch
        {
            Gender.Male => "Masculino",
            Gender.Female => "Feminino",
            Gender.Other => "Outro",
            _ => "Não especificado"
        };

        ViewBag.StatusPT = user.Status == UserStatus.Active ? "ATIVO" : "INATIVO";
        ViewBag.StatusClass = user.Status == UserStatus.Active ? "bg-success" : "bg-danger";
        ViewBag.StatusIcon = user.Status == UserStatus.Active ? "bi-check-circle" : "bi-x-circle";

        return View(user);
    }

    /// <summary>
    /// Apresenta o formulário de edição de um utilizador.
    /// </summary>
    /// <param name="id">ID do utilizador a editar.</param>
    /// <returns>View com o ViewModel preenchido para edição.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();

        return View(new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Gender = user.Gender,
            BirthDate = user.BirthDate,
            Balance = user.Balance,
            Role = roles.FirstOrDefault() ?? "Client",
            Category = user.UserCategory.Name
        });
    }

    /// <summary>
    /// Processa as alterações de dados, categoria e função (role) de um utilizador.
    /// </summary>
    /// <param name="model">ViewModel com os dados atualizados.</param>
    /// <returns>Redireciona para o Index em caso de sucesso.</returns>
    /// <remarks>
    /// Em caso de alteração de Role, o SecurityStamp é atualizado para forçar o refresh das claims do utilizador.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
            ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Balance = model.Balance;
        user.Gender = model.Gender;
        user.BirthDate = model.BirthDate;
        user.UserCategory = await _adminService.GetCategoryByNameAsync(model.Category);

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var oldRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            TempData.SetSwalSuccess("Utilizador atualizado.");

            await _userManager.UpdateSecurityStampAsync(user);
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
        return View(model);
    }

    /// <summary>
    /// Desativa um utilizador, impedindo o login através de Lockout permanente.
    /// </summary>
    /// <param name="id">ID do utilizador a desativar.</param>
    /// <returns>Redireciona para o Index com o resultado da operação.</returns>
    /// <remarks>Impede que o administrador desative a sua própria conta.</remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData.SetSwalError("Utilizador não encontrado.");
            return RedirectToAction(nameof(Index));
        }

        if (user.UserName == User.Identity?.Name)
        {
            TempData.SetSwalError("Não podes apagar a tua própria conta.");
            return RedirectToAction(nameof(Index));
        }

        user.Status = UserStatus.Inactive;
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData.SetSwalSuccess($"O utilizador {user.FirstName} foi desativado com sucesso.");
        }
        else
        {
            TempData.SetSwalError("Erro ao desativar utilizador.");
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Reativa uma conta de utilizador anteriormente desativada.
    /// </summary>
    /// <param name="id">ID do utilizador a ativar.</param>
    /// <returns>Redireciona para os Detalhes do utilizador.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData.SetSwalError("Utilizador não encontrado.");
            return RedirectToAction(nameof(Index));
        }

        user.Status = UserStatus.Active;
        await _userManager.SetLockoutEndDateAsync(user, null);

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData.SetSwalSuccess($"A conta de {user.FirstName} foi reativada.");
        }
        else
        {
            TempData.SetSwalError("Erro ao reativar utilizador.");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Apresenta a página de seleção para diferentes tipos de logs.
    /// </summary>
    /// <returns>A View de seleção de logs.</returns>
    public IActionResult UserLogSelection()
    {
        return View();
    }

    /// <summary>
    /// Lista os logs de atividade realizados pelos membros do Staff (auditoria interna).
    /// </summary>
    /// <param name="search">Termo de pesquisa (username ou conteúdo da mensagem).</param>
    /// <param name="date">Filtro por data específica.</param>
    /// <returns>A View com a lista de logs ordenada por data descendente.</returns>
    public async Task<IActionResult> StaffLog(string search, string date)
    {
        var query = _context.UserLog
            .Include(l => l.AppUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => l.AppUser.UserName.Contains(search) || l.Message.Contains(search));
        }

        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedDate))
        {
            query = query.Where(l => l.TimeStamp.Date == parsedDate.Date);
        }

        var logs = await query.OrderByDescending(l => l.TimeStamp).ToListAsync();

        return View(logs);
    }
}
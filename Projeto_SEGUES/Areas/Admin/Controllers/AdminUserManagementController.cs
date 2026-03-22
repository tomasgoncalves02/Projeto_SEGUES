using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using AppErrors = Projeto_SEGUES.Models.Enums.AppErrors;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controlador responsável pela gestão administrativa de utilizadores, permissões e auditoria.
/// </summary>
/// <remarks>
/// Centraliza operações críticas como edição de perfis, gestão de saldos, alteração de cargos (Roles)
/// e visualização de logs de sistema. Implementa uma política rigorosa de tratamento de erros:
/// falhas de carregamento redirecionam para a página de erro global, enquanto falhas de operação 
/// disparam alertas contextuais (SweetAlert).
/// </remarks>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminUserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;
    private readonly AppDbContext _context;
    private readonly ILogger<AdminUserManagementController> _logger;
    private readonly IStringLocalizer<Errors> _localizer;

    /// <summary>
    /// Inicializa uma nova instância do controlador injetando serviços de Identity, Auditoria e Localização.
    /// </summary>
    public AdminUserManagementController(
        UserManager<AppUser> userManager,
        IAdminService adminService,
        AppDbContext context,
        ILogger<AdminUserManagementController> logger,
        IStringLocalizer<Errors> localizer)
    {
        _userManager = userManager;
        _adminService = adminService;
        _context = context;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Lista os utilizadores do sistema com suporte a pesquisa por texto e filtros de cargo/categoria.
    /// </summary>
    /// <param name="searchString">Termo de pesquisa (Nome ou Email).</param>
    /// <param name="roleFilter">Filtro por cargo (Admin, Staff, Client).</param>
    /// <param name="categoryFilter">Filtro por categoria (Ex: Aluno, Docente).</param>
    /// <returns>View Index com a coleção filtrada. Redireciona para Error Home em caso de falha na BD.</returns>
    public async Task<IActionResult> Index(string? searchString, string? roleFilter, string? categoryFilter)
    {
        try
        {           
            var users = await _adminService.GetFilteredUsersAsync(searchString, roleFilter, categoryFilter);
            ViewData["SearchString"] = searchString;
            ViewData["CurrentRole"] = roleFilter;
            ViewData["CurrentCategory"] = categoryFilter;

            ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
            ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
            return View(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao listar utilizadores no Index.");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Apresenta o perfil detalhado de um utilizador, incluindo estado de conta e metadados formatados.
    /// </summary>
    /// <param name="id">GUID do utilizador.</param>
    /// <returns>View de Detalhes ou NotFound. Falhas técnicas redirecionam para a página de erro.</returns>
    public async Task<IActionResult> Details(string id)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao recuperar detalhes do utilizador {id}.");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Prepara o formulário de edição de utilizador com dados atuais e listas de seleção.
    /// </summary>
    /// <param name="id">ID do utilizador a editar.</param>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
            ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();

            return View(new EditUserViewModelAdmin
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Gender = user.Gender,
                BirthDate = user.BirthDate,
                Balance = user.Balance,
                Role = roles.FirstOrDefault() ?? "Client",
                Category = user.UserCategory?.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao carregar edição para {id}.");
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
    }

    /// <summary>
    /// Persiste as alterações no utilizador, gere a troca de cargos (Roles) e solicita confirmação se o email mudar.
    /// </summary>
    /// <param name="model">ViewModel com os dados submetidos para atualização.</param>
    /// <remarks>
    /// Implementa uma lógica de e-mail pendente: se o endereço mudar, o sistema não o altera 
    /// imediatamente na base de dados, mas envia um pedido de confirmação para o novo endereço.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModelAdmin model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
            ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
            return View(model);
        }
        try
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();
            string? pendingEmail = null;
            if (model.Email != user.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null)
                {
                    ModelState.AddModelError("Email", "Este email já está em uso por outra conta.");
                    ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
                    ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
                    return View(model);
                }
                pendingEmail = model.Email;
            }
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Balance = model.Balance;
            user.Gender = model.Gender;
            user.BirthDate = model.BirthDate;
            user.UserCategory = (await _adminService.GetCategoryByNameAsync(model.Category)) ?? user.UserCategory;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var oldRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, oldRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
                await _userManager.UpdateSecurityStampAsync(user);

                if (!string.IsNullOrEmpty(pendingEmail))
                {
                    try
                    {
                        await _adminService.RequestEmailChangeAsync(user, pendingEmail, Url, Request.Scheme);
                        TempData.SetSwalInfo("Utilizador atualizado! O link de confirmação foi enviado para o novo e-mail.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogAppError($"Erro ao enviar e-mail de confirmação para {pendingEmail}: {ex.Message}", TableName.User, AppOperation.Other);

                        var erroEmail = AppErrors.EmailSenderError;
                        TempData.SetSwalError($"{_localizer[erroEmail.ToString()].Value} [Erro: {(int)erroEmail}]");
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    TempData.SetSwalSuccess("Utilizador atualizado com sucesso.");
                }

                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Erro crítico na edição do utilizador {model.Id}: {ex.Message}", TableName.User, AppOperation.Update);

            var erroEnum = AppErrors.DatabaseUpdateError;
            TempData.SetSwalError($"{_localizer[erroEnum.ToString()].Value} [Erro: {(int)erroEnum}]");
        }

        ViewBag.Roles = await _adminService.GetAllRolesForDropdownAsync();
        ViewBag.Categories = await _adminService.GetAllCategoriesForDropdownAsync();
        return View(model);
    }

    /// <summary>
    /// Desativa um utilizador e aplica um Lockout permanente.
    /// </summary>
    /// <remarks>Impede a auto-desativação para evitar perda de acesso administrativo.</remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData.SetSwalError("O utilizador indicado não foi encontrado.");
                return RedirectToAction(nameof(Index));
            }

            if (user.UserName == User.Identity?.Name)
            {
                TempData.SetSwalError("Medida de segurança: Não podes desativar a tua própria conta.");
                return RedirectToAction(nameof(Index));
            }

            user.Status = UserStatus.Inactive;
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            await _userManager.UpdateAsync(user);

            TempData.SetSwalSuccess($"A conta de {user.FirstName} foi desativada.");
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Erro ao desativar ID {id}: {ex.Message}", TableName.User, AppOperation.Update);
            TempData.SetSwalError("Falha técnica ao desativar o utilizador. [Erro: 1004]");
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Reativa uma conta de utilizador e remove qualquer restrição de Lockout.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData.SetSwalError("Utilizador não encontrado.");
                return RedirectToAction(nameof(Index));
            }

            user.Status = UserStatus.Active;
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.UpdateAsync(user);

            TempData.SetSwalSuccess($"A conta de {user.FirstName} está novamente ativa.");
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Erro ao reativar ID {id}: {ex.Message}", TableName.User, AppOperation.Update);
            TempData.SetSwalError("Não foi possível reativar a conta. [Erro: 1004]");
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Consulta os logs de auditoria interna (ações realizadas pela equipa Staff).
    /// </summary>
    /// <param name="search">Termo de busca nos logs.</param>
    /// <param name="date">Data específica da ocorrência.</param>
    public async Task<IActionResult> StaffLog(string search, string date)
    {
        try
        {
            var query = _context.UserLog.Include(l => l.AppUser).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(l => l.AppUser.UserName.Contains(search) || l.Message.Contains(search));

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedDate))
                query = query.Where(l => l.TimeStamp.Date == parsedDate.Date);

            var logs = await query.OrderByDescending(l => l.TimeStamp).ToListAsync();
            return View(logs);
        }
        catch (Exception ex)
        {
            _logger.LogAppError($"Falha na consulta de auditoria: {ex.Message}", TableName.UserLog, AppOperation.Read);
            return RedirectToAction("Error", "Home", new { area = "", errorCode = (int)AppErrors.DatabaseQueryError });
        }
    }
}
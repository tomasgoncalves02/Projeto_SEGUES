using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin.Controllers;

/// <summary>
/// Controller responsável pela gestão administrativa dos links das ementas do refeitório e do bar.
/// </summary>
/// <remarks>
/// Este controlador permite que a administração atualize dinamicamente os URLs que apontam para as ementas semanais,
/// garantindo que os utilizadores tenham sempre acesso à informação mais recente sem necessidade de alterações no código.
/// </remarks>
[Area("Admin")]
public class AdminMenuManagementController : Controller
{
    private readonly IAdminService _adminService;

    /// <summary>
    /// Inicializa uma nova instância do controlador com o serviço administrativo.
    /// </summary>
    /// <param name="adminService">Serviço que gere as configurações globais e persistência de links do sistema.</param>
    public AdminMenuManagementController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Apresenta a página de gestão de ementas com os links atualmente configurados.
    /// </summary>
    /// <returns>A View de índice populada com o <see cref="MenuManagementViewModel"/> contendo os URLs atuais.</returns>
    public async Task<IActionResult> Index()
    {
        var model = new MenuManagementViewModel
        {
            CanteenUrl = await _adminService.GetCanteenMenuLinkAsync(),
            BarUrl = await _adminService.GetBarMenuLinkAsync()
        };
        return View(model);
    }

    /// <summary>
    /// Processa a submissão dos novos URLs das ementas.
    /// </summary>
    /// <param name="model">Modelo contendo os novos links validados.</param>
    /// <returns>
    /// Redireciona para o índice com uma mensagem de sucesso (SweetAlert) ou 
    /// retorna a View com erros de validação caso o modelo seja inválido.
    /// </returns>
    /// <remarks>
    /// O método utiliza o <see cref="IAdminService.UpdateMenuLinksAsync"/> para persistir as alterações na base de dados.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> SaveLinks(MenuManagementViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);
        await _adminService.UpdateMenuLinksAsync(model.CanteenUrl, model.BarUrl);
        TempData.SetSwalSuccess("Os links das ementas foram atualizados com sucesso!");
        return RedirectToAction("Index");
    }
}
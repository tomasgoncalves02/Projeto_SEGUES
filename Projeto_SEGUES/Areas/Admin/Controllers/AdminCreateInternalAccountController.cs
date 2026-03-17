using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

/// <summary>
/// Controller responsável pela criação de contas internas (funcionários/administradores) no sistema.
/// </summary>
/// <remarks>
/// Este controlador gere o processo de registo de novos utilizadores que não são clientes, 
/// incluindo a atribuição de permissões e o envio de e-mails de ativação.
/// </remarks>
[Authorize(Roles = "Admin")]
[Area("Admin")]
public class AdminCreateInternalAccountController : Controller
{
    private readonly IAdminService _adminService;

    /// <summary>
    /// Inicializa uma nova instância do controlador com o serviço de administração.
    /// </summary>
    /// <param name="adminService">Interface do serviço que contém a lógica de gestão de utilizadores.</param>
    public AdminCreateInternalAccountController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Apresenta o formulário de criação de conta interna.
    /// </summary>
    /// <returns>A View de índice com a lista de funções (roles) disponíveis no ViewBag.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        return View();
    }

    /// <summary>
    /// Processa a submissão do formulário para criar um novo utilizador interno.
    /// </summary>
    /// <param name="model">Modelo de dados contendo as informações do novo utilizador.</param>
    /// <returns>
    /// Redireciona para o Index em caso de sucesso ou devolve a View com mensagens de erro em caso de falha.
    /// </returns>
    /// <remarks>
    /// Valida o estado do modelo, tenta criar o utilizador via serviço e gere exceções relacionadas com o envio de e-mails.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInternalUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
            return View("Index", model);
        }

        try
        {
            var result = await _adminService.CreateInternalUserAsync(model);

            if (result.Succeeded)
            {
                TempData.SetSwalSuccess($"Conta criada para {model.FirstName}!");
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Erro ao enviar e-mail de ativação. Verifique a sua conexão à Internet.");
            TempData.SetSwalError("Falha na conexão: O e-mail não pode ser enviado, por isso a conta não foi criada.");
        }

        ViewBag.Roles = await _adminService.GetNonClientRolesForDropdownAsync();
        return View("Index", model);
    }
}

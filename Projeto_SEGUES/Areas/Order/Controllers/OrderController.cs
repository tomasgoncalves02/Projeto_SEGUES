using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsável pela página inicial do módulo de encomendas.
/// </summary>
/// <remarks>
/// Este controlador serve como ponto de entrada para o utilizador, fornecendo informações essenciais 
/// como o saldo disponível, o horário de funcionamento do bar e o acesso à ementa digital.
/// </remarks>
[Authorize]
[Area("Order")]
public class OrderController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;
    private readonly IOrderService _orderService;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de gestão de utilizadores e administração.
    /// </summary>
    /// <param name="userManager">Gestor de utilizadores do Identity para aceder aos dados de perfil e saldo.</param>
    /// <param name="adminService">Serviço administrativo para obtenção de horários e links das ementas.</param>
    public OrderController(UserManager<AppUser> userManager, IAdminService adminService, IOrderService orderService)
    {
        _userManager = userManager;
        _adminService = adminService;
        _orderService = orderService;
    }

    /// <summary>
    /// Prepara e apresenta a página inicial da área de encomendas.
    /// </summary>
    /// <returns>
    /// A View principal de encomendas populada com o saldo do utilizador e horários de funcionamento no ViewBag.
    /// Devolve um desafio de autenticação (Challenge) caso o utilizador não seja encontrado.
    /// </returns>
    /// <remarks>
    /// Os horários de abertura e fecho são formatados para o padrão "hh\:mm" para exibição direta na interface.
    /// </remarks>
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        ViewBag.UserBalance = user.Balance;
        var cart = await _orderService.GetCartAsync(user.Id, false);
        if (cart != null)
        {
            ViewBag.CartTotal = _orderService.GetOrderTotal(cart);
        }
        BarCanteenConfigViewModel barCanteenConfig = await _adminService.GetScheduleAsync();
        ViewBag.BarOpeningTimeString = barCanteenConfig.BarOpeningTimeString;
        ViewBag.BarClosingTimeString = barCanteenConfig.BarClosingTimeString;

        ViewBag.BarMenuLink = barCanteenConfig.BarMenuLink;
        return View();
    }
}
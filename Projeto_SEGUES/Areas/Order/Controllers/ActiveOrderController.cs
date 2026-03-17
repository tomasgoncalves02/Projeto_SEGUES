using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsável pela gestão e visualização dos pedidos ativos do utilizador autenticado.
/// </summary>
/// <remarks>
/// Este controlador permite que os utilizadores consultem o estado dos seus pedidos em curso, 
/// visualizem detalhes específicos e realizem o cancelamento de pedidos, desde que as regras de negócio o permitam.
/// </remarks>
[Area("Order")]
[Authorize]
public class ActiveOrderController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IOrderService _orderService;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de utilizador e pedidos.
    /// </summary>
    /// <param name="userManager">Gestor de utilizadores do ASP.NET Core Identity.</param>
    /// <param name="orderService">Serviço de lógica de negócio para operações de encomendas.</param>
    public ActiveOrderController(UserManager<AppUser> userManager, IOrderService orderService)
    {
        _userManager = userManager;
        _orderService = orderService;
    }

    /// <summary>
    /// Apresenta a lista de pedidos ativos (em processamento ou prontos) do utilizador.
    /// </summary>
    /// <returns>A View principal com a coleção de pedidos ativos filtrada pelo ID do utilizador logado.</returns>
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        return View(await _orderService.GetActiveOrdersAsync(userId!));
    }

    /// <summary>
    /// Endpoint otimizado para HTMX que devolve apenas os cartões de pedidos ativos.
    /// </summary>
    /// <returns>Uma PartialView contendo a representação visual atualizada das encomendas.</returns>
    /// <remarks>
    /// Utilizado para polling ou atualizações em tempo real na interface sem necessidade de recarregar a página completa.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetUpdatedActiveOrders()
    {
        var userId = _userManager.GetUserId(User);
        return PartialView("_ActiveOrdersCards", await _orderService.GetActiveOrdersAsync(userId!));
    }

    /// <summary>
    /// Apresenta os detalhes detalhados de uma encomenda específica.
    /// </summary>
    /// <param name="id">Identificador único da encomenda.</param>
    /// <returns>A View de detalhes, ou redirecionamento com erro caso a encomenda não exista ou não pertença ao utilizador.</returns>
    /// <remarks>
    /// Inclui validação de segurança para garantir que um utilizador não acede a detalhes de pedidos de terceiros.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null || !order.Status.IsActive() || order.AppUser.Id != _userManager.GetUserId(User))
        {
            TempData.SetSwalError("Pedido não encontrado.");
            return RedirectToAction(nameof(Index));
        }
        ViewBag.TotalQuantity = _orderService.GetOrderTotal(order).TotalQuantity;
        return View(order);
    }

    /// <summary>
    /// Processa o pedido de cancelamento de uma encomenda ativa.
    /// </summary>
    /// <param name="id">ID da encomenda a cancelar.</param>
    /// <returns>Redireciona para o índice com mensagem de sucesso ou erro (via SweetAlert).</returns>
    /// <remarks>
    /// O cancelamento depende das regras implementadas no <see cref="IOrderService"/> (ex: tempo limite ou estado atual).
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var result = await _orderService.CancelOrderAsync(id);
        if (!result.Success)
        {
            TempData.SetSwalError(result.Message);
            return RedirectToAction(nameof(Index));
        }
        TempData.SetSwalSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }
}
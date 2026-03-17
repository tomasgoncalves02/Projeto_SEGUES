using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsável pela gestão operacional de pedidos por parte dos funcionários e administradores.
/// </summary>
/// <remarks>
/// Este controlador permite ao staff monitorizar pedidos pendentes, atualizar estados de produção 
/// e validar códigos de levantamento para finalizar o ciclo de entrega ao utilizador.
/// </remarks>
[Authorize(Roles = "Admin,Employee")]
[Area("Order")]
public class OrderManagementController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de pedidos e gestão de utilizadores.
    /// </summary>
    /// <param name="orderService">Serviço de lógica de negócio para manipulação de encomendas.</param>
    /// <param name="userManager">Gestor de utilizadores para identificar o funcionário que realiza as ações.</param>
    public OrderManagementController(IOrderService orderService, UserManager<AppUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    /// <summary>
    /// Apresenta a interface principal de gestão de pedidos não entregues.
    /// </summary>
    /// <returns>A View de índice com a lista de pedidos pendentes para processamento.</returns>
    public async Task<IActionResult> Index()
    {
        return View(await _orderService.GetUndeliveredOrdersAsync());
    }

    /// <summary>
    /// Obtém apenas a tabela de pedidos para atualizações parciais de interface.
    /// </summary>
    /// <returns>Uma PartialView contendo a tabela atualizada de encomendas por entregar.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrdersTable()
    {
        return PartialView("_ManageOrdersTablePartial", await _orderService.GetUndeliveredOrdersAsync());
    }

    /// <summary>
    /// Obtém os detalhes de um pedido específico para exibição num painel lateral (Side Card).
    /// </summary>
    /// <param name="id">Identificador único da encomenda.</param>
    /// <returns>Uma PartialView com os detalhes da encomenda ou NotFound caso não exista.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrderDetailsSide(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        ViewBag.TotalQuantity = _orderService.GetOrderTotal(order).TotalQuantity;
        return PartialView("_ManageOrderDetailsSideCardPartial", order);
    }

    /// <summary>
    /// Atualiza o estado de um pedido (ex: Em Preparação, Pronto).
    /// </summary>
    /// <param name="id">ID do pedido a atualizar.</param>
    /// <param name="newStatus">Inteiro representativo do novo estado (Enum OrderStatus).</param>
    /// <returns>StatusCode 200 (Ok) em caso de sucesso ou 400 (BadRequest) em caso de erro na lógica de negócio.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int newStatus)
    {
        var staffMember = await _userManager.GetUserAsync(User);
        var result = await _orderService.UpdateOrderStatusAsync(id, newStatus, staffMember);
        if (!result.Success) return BadRequest(result.Message);
        return Ok();
    }

    /// <summary>
    /// Valida o código de redenção inserido pelo funcionário para confirmar a entrega do pedido.
    /// </summary>
    /// <param name="id">ID do pedido a validar.</param>
    /// <param name="codeEntered">Código alfanumérico fornecido pelo cliente.</param>
    /// <returns>Resultado da operação em formato JSON.</returns>
    /// <remarks>
    /// Adiciona o Header "HX-Trigger" para notificar o frontend (via HTMX) de que o pedido foi atualizado com sucesso.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> ValidateOrderCode(int id, string codeEntered)
    {
        var staffMember = await _userManager.GetUserAsync(User);
        var result = await _orderService.ValidateOrderCodeAsync(id, codeEntered, staffMember);
        if (!result.Success) return BadRequest(result);
        Response.Headers.Append("HX-Trigger", "orderUpdated");
        return Ok(new { success = true, message = result.Message });
    }
}
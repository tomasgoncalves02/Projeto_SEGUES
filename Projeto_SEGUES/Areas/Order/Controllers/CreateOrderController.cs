using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsável pela criação de novas encomendas e gestão do carrinho de compras.
/// </summary>
/// <remarks>
/// Este controlador coordena a interação entre o inventário de produtos e o serviço de encomendas, 
/// permitindo a adição/remoção de itens e a finalização do processo de compra (Checkout).
/// </remarks>
[Area("Order")]
[Authorize]
public class CreateOrderController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de inventário, encomendas, utilizadores e administração.
    /// </summary>
    /// <param name="inventoryService">Serviço para consulta de produtos disponíveis.</param>
    /// <param name="orderService">Serviço para gestão de operações do carrinho e encomendas.</param>
    /// <param name="userManager">Gestor de utilizadores Identity.</param>
    /// <param name="adminService">Serviço de configurações globais do sistema.</param>
    public CreateOrderController(
        IInventoryService inventoryService,
        IOrderService orderService,
        UserManager<AppUser> userManager,
        IAdminService adminService
    )
    {
        _inventoryService = inventoryService;
        _orderService = orderService;
        _userManager = userManager;
        _adminService = adminService;
    }

    /// <summary>
    /// Apresenta a página de seleção de produtos para a nova encomenda.
    /// </summary>
    /// <returns>A View com a lista de produtos disponíveis e o estado atual do carrinho no ViewBag.</returns>
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var cart = await _orderService.GetCartAsync(userId);
        ViewBag.CartTotal = _orderService.GetOrderTotal(cart);
        ViewBag.Categories = await _inventoryService.GetAllCategoriesForDropdownAsync();
        return View(await _inventoryService.GetAvailableProductsAsync());
    }

    /// <summary>
    /// Adiciona um produto e respetiva quantidade ao carrinho do utilizador via AJAX.
    /// </summary>
    /// <param name="id">ID do produto a adicionar.</param>
    /// <param name="qty">Quantidade pretendida.</param>
    /// <returns>Objeto JSON contendo o sucesso da operação, mensagem e totais atualizados do carrinho.</returns>
    [HttpPost]
    public async Task<IActionResult> AddToCart(int id, int qty)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        
        var result = await _orderService.AddToCartAsync(userId, id, qty);
        if (!result.Success) return NotFound(new { failMessage = result.Message});
        
        OrderTotalViewModel orderTotal = (OrderTotalViewModel) result.Data!;
        return Ok(new { successMessage = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
    }

    /// <summary>
    /// Remove um produto específico do carrinho de compras via AJAX.
    /// </summary>
    /// <param name="id">ID do produto a remover.</param>
    /// <returns>Objeto JSON com o estado atualizado do carrinho após a remoção.</returns>
    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();
        
        var result = await _orderService.RemoveFromCartAsync(userId, id);
        if (!result.Success) return NotFound(new { failMessage = result.Message});
        
        OrderTotalViewModel orderTotal = (OrderTotalViewModel) result.Data!;
        return Ok(new { successMessage = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
    }

    /// <summary>
    /// Apresenta a página de finalização de compra (Checkout), mostrando o resumo do pedido e saldo do utilizador.
    /// </summary>
    /// <returns>A View de Checkout com o conteúdo do carrinho atual.</returns>
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.Balance = user!.Balance;
        var cart = await _orderService.GetCartAsync(user.Id);
        ViewBag.TotalQuantity = _orderService.GetOrderTotal(cart).TotalQuantity;
        return View(cart);
    }

    /// <summary>
    /// Processa a submissão final da encomenda, validando saldo e horários de recolha.
    /// </summary>
    /// <param name="receiveNow">Indica se a recolha é imediata.</param>
    /// <param name="pickupTime">Hora agendada para a recolha (opcional).</param>
    /// <returns>Redireciona para a lista de pedidos ativos em caso de sucesso ou volta ao Checkout em caso de erro.</returns>
    /// <remarks>
    /// Utiliza o serviço <see cref="IOrderService.SubmitOrderAsync"/> para persistir a encomenda e abater o saldo do utilizador.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOrder(bool receiveNow, string? pickupTime)
    {
        var user = await _userManager.GetUserAsync(User);

        var result = await _orderService.SubmitOrderAsync(user!, receiveNow, pickupTime);

        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction("Index", "ActiveOrder", new { area = "Order" });
        }
        TempData.SetSwalError(result.Message);
        return RedirectToAction(nameof(Checkout));
    }
}
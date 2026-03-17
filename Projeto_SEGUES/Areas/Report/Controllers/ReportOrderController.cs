using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report;

/// <summary>
/// Controller responsável pela visualização do histórico e detalhes de pedidos para o utilizador.
/// </summary>
/// <remarks>
/// Este controlador disponibiliza ferramentas para que o utilizador consulte as suas compras passadas
/// e obtenha detalhes técnicos sobre cada transação via API interna.
/// </remarks>
[Area("Report")]
public class ReportOrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de pedidos e gestão de utilizadores.
    /// </summary>
    /// <param name="orderService">Serviço de lógica de negócio para consulta de histórico de encomendas.</param>
    /// <param name="userManager">Gestor de utilizadores para identificação do contexto do utilizador atual.</param>
    public ReportOrderController(IOrderService orderService, UserManager<AppUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    /// <summary>
    /// Apresenta o histórico completo de pedidos do utilizador autenticado.
    /// </summary>
    /// <returns>
    /// A View de índice populada com a lista de pedidos do utilizador ou redirecionamento para o Login caso não esteja autenticado.
    /// </returns>
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        return View(await _orderService.GetOrderHistoryAsync(userId));
    }

    /// <summary>
    /// Fornece os detalhes de um pedido específico em formato JSON para consumo via JavaScript.
    /// </summary>
    /// <param name="id">Identificador único da encomenda.</param>
    /// <returns>
    /// Um objeto JSON contendo o código de redenção e a lista de produtos, ou erro de autorização/não encontrado.
    /// </returns>
    /// <remarks>
    /// Inclui uma verificação de segurança que impede utilizadores comuns de visualizarem detalhes de pedidos de terceiros, 
    /// permitindo o acesso total apenas a administradores.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();

        // Security check: if the user is not admin, do not show order info if it does not belong to the user
        if (!User.IsInRole("Admin") && order.AppUser.Id != _userManager.GetUserId(User))
            return Unauthorized();

        return Json(new
        {
            code = order.RedemptionCode,
            products = order.ProductPurchases.Select(p => new
            {
                name = p.Product.Name,
                quantity = p.Quantity,
                price = p.ProductValue
            }).ToList()
        });
    }
}
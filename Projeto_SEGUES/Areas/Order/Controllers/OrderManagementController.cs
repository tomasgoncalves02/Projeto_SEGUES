using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

[Authorize(Roles = "Admin,Employee")]
[Area("Order")]
public class OrderManagementController : Controller
{
    private readonly IOrderService _orderService;
    
    public OrderManagementController(IOrderService orderService)
    {
        _orderService = orderService;
    }
    
    public async Task<IActionResult> Index()
    {
        return View(await _orderService.GetUndeliveredOrdersAsync());
    }
    
    //Apenas a tabela
    [HttpGet]
    public async Task<IActionResult> GetOrdersTable()
    {
        return PartialView("_ManageOrdersTablePartial", await _orderService.GetUndeliveredOrdersAsync());
    }
    
    [HttpGet]
    public async Task<IActionResult> GetOrderDetailsSide(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        ViewBag.TotalQuantity = _orderService.GetOrderTotal(order).TotalQuantity;
        return PartialView("_ManageOrderDetailsSideCardPartial", order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int newStatus)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, newStatus);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(); // Retorna OK para o JavaScript saber que deu certo
    }

    [HttpPost]
    public async Task<IActionResult> ValidateOrderCode(int id, string codeEntered)
    {
        var result = await _orderService.ValidateOrderCodeAsync(id, codeEntered);
        if (!result.Success) return BadRequest(result.Message);
        Response.Headers.Add("HX-Trigger", "orderUpdated");
        return Ok(new { success = true });
    }
}
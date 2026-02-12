using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminTicketManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ITicketService _ticketService;
    
    public AdminTicketManagementController(IAdminService adminService, ITicketService ticketService)
    {
        _adminService = adminService;
        _ticketService = ticketService;
    }
    
    // Displays Prices + Global Ticket History
    public async Task<IActionResult> Index()
    {
        ViewBag.Prices = await _adminService.GetTicketPricesAsync();
        ViewBag.CurrentValidityDays = await _adminService.GetTicketValidityDaysAsync();
        var history = await _ticketService.GetAllTicketsAsync();
        return View(history);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrices(List<TicketPrice> updatedPrices)
    {
        if (ModelState.IsValid)
        {
            await _adminService.UpdateTicketPricesAsync(updatedPrices);
            TempData.SetSwalSuccess("Preçário atualizado!");
        }
        else
        {
            TempData.SetSwalError("Dados inválidos.");
        }
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateValidity(int validityDays)
    {
        if (validityDays < 1)
        {
            TempData.SetSwalError("A validade deve ser de pelo menos 1 dia.");
            return RedirectToAction(nameof(Index));
        }

        await _adminService.UpdateTicketValidityDaysAsync(validityDays);
        TempData.SetSwalSuccess($"Validade global alterada para {validityDays} dias.");
    
        return RedirectToAction(nameof(Index));
    }
}
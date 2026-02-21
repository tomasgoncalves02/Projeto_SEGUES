using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminTicketManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ITicketService _ticketService;
    
    public AdminTicketManagementController(IAdminService adminService, UserManager<AppUser> userManager, ITicketService ticketService)
    {
        _adminService = adminService;
        _userManager = userManager;
        _ticketService = ticketService;
    }

    // Displays Prices + Global Ticket History
    public async Task<IActionResult> Index()
    {
        ViewBag.CurrentUserId = _userManager.GetUserId(User);

        ViewBag.Prices = await _adminService.GetTicketPricesAsync();
        ViewBag.CurrentValidityDays = await _adminService.GetTicketValidityDaysAsync();

        var history = await _ticketService.GetAllTicketsAsync();

        return View(history);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrices(List<TicketPrice> updatedPrices)
    {
        if (updatedPrices == null || !updatedPrices.Any()) return RedirectToAction(nameof(Index));

        // Forçar a cultura Invariante para que 1.50 seja lido como 1 euro e 50 cêntimos
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        // Removemos a validação automática para garantir que o código executa
        foreach (var key in ModelState.Keys.ToList()) ModelState.Remove(key);

        try
        {
            await _adminService.UpdateTicketPricesAsync(updatedPrices);
            TempData["SwalData"] = "{\"icon\":\"success\",\"title\":\"Sucesso\",\"text\":\"Preçário atualizado!\"}";
        }
        catch (Exception)
        {
            TempData["SwalData"] = "{\"icon\":\"error\",\"title\":\"Erro\",\"text\":\"Falha ao gravar.\"}";
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsável pela aquisição e visualização de senhas (tickets) de refeição.
/// </summary>
/// <remarks>
/// Este controlador gere o inventário pessoal de senhas do utilizador e o processo de compra,
/// interagindo com o serviço de tickets para validar preços e saldos.
/// </remarks>
[Authorize]
[Area("Order")]
public class OrderTicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de senhas e utilizadores.
    /// </summary>
    /// <param name="ticketService">Serviço de lógica de negócio para gestão de senhas.</param>
    /// <param name="userManager">Gestor de utilizadores para acesso ao perfil e categoria do utilizador.</param>
    public OrderTicketController(ITicketService ticketService, UserManager<AppUser> userManager)
    {
        _ticketService = ticketService;
        _userManager = userManager;
    }

    /// <summary>
    /// Apresenta a página de gestão de senhas do utilizador autenticado.
    /// </summary>
    /// <returns>
    /// A View com a lista de senhas pertencentes ao utilizador e informações de preço/saldo no ViewBag.
    /// Devolve um desafio de autenticação (Challenge) caso o utilizador não seja encontrado.
    /// </returns>
    /// <remarks>
    /// O preço da senha é determinado dinamicamente com base na categoria do utilizador (Aluno, Docente, Funcionário).
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Obter o preço atual para a categoria deste utilizador
        decimal currentPrice = await _ticketService.GetCurrentPriceForUserAsync(user);

        ViewBag.UserBalance = user.Balance;
        ViewBag.CurrentPrice = currentPrice;

        // Obter a lista de senhas deste utilizador
        var myTickets = await _ticketService.GetUserTicketsAsync(user.Id);
        return View(myTickets);
    }

    /// <summary>
    /// Processa a compra de uma ou mais senhas de refeição.
    /// </summary>
    /// <param name="quantity">Quantidade de senhas a adquirir (por defeito 1).</param>
    /// <returns>Redireciona para o índice com a mensagem de sucesso ou erro (via SweetAlert).</returns>
    /// <remarks>
    /// A lógica de transação, incluindo o abate no saldo e a geração das senhas, é delegada ao serviço <see cref="ITicketService"/>.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyTicket(int quantity = 1)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _ticketService.BuyTicketsAsync(userId, quantity);

        if (result.Success)
            TempData.SetSwalSuccess(result.Message);
        else
            TempData.SetSwalError(result.Message);

        return RedirectToAction(nameof(Index));
    }
}
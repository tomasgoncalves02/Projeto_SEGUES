using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Report;

/// <summary>
/// Controller responsável pela visualização e filtragem do histórico de senhas do utilizador.
/// </summary>
/// <remarks>
/// Este controlador permite ao utilizador auditar o uso das suas senhas, filtrando por estado 
/// (Disponível, Usada, Expirada), fluxo (Comprada, Recebida, Enviada) e datas específicas.
/// </remarks>
[Authorize]
[Area("Report")]
public class ReportTicketsController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Inicializa uma nova instância do controlador com os serviços de senhas e utilizadores.
    /// </summary>
    /// <param name="ticketService">Serviço de lógica de negócio para consulta e filtragem de senhas.</param>
    /// <param name="userManager">Gestor de utilizadores para identificação do contexto do utilizador atual.</param>
    public ReportTicketsController(ITicketService ticketService, UserManager<AppUser> userManager)
    {
        _ticketService = ticketService;
        _userManager = userManager;
    }

    /// <summary>
    /// Apresenta a página principal do histórico de senhas com suporte a múltiplos filtros.
    /// </summary>
    /// <param name="searchString">Termo de pesquisa para códigos ou utilizadores relacionados.</param>
    /// <param name="stateFilter">Filtro por estado da senha (Enum <see cref="TicketState"/>).</param>
    /// <param name="flowFilter">Filtro de fluxo (ex: "Enviadas", "Recebidas").</param>
    /// <param name="dateFilter">Filtro por data de transação ou uso.</param>
    /// <returns>A View de índice populada com os resultados da consulta.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(string searchString, TicketState? stateFilter, string flowFilter, DateTime? dateFilter)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        // Consulta o histórico através do serviço especializado
        var tickets = await _ticketService.QueryHistoryAsync(userId, searchString, stateFilter, flowFilter, dateFilter);

        ViewData["CurrentSearch"] = searchString;
        ViewData["CurrentState"] = stateFilter;
        ViewData["CurrentFlow"] = flowFilter;
        ViewData["CurrentDate"] = dateFilter;
        ViewBag.CurrentUserId = userId;

        return View(tickets);
    }

    /// <summary>
    /// Endpoint para atualização dinâmica (AJAX) da tabela de histórico de senhas.
    /// </summary>
    /// <param name="stateFilter">Estado da senha em formato string para conversão.</param>
    /// <param name="dateFilter">Data selecionada para filtragem.</param>
    /// <param name="flowFilter">Tipo de fluxo de transação.</param>
    /// <param name="searchString">Termo de pesquisa alfanumérico.</param>
    /// <returns>Uma PartialView contendo apenas as linhas da tabela filtradas.</returns>
    /// <remarks>
    /// Este método é otimizado para chamadas assíncronas no frontend, evitando o recarregamento total da página.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetFilteredHistory(string stateFilter, DateTime? dateFilter, string flowFilter, string searchString)
    {
        var userId = _userManager.GetUserId(User)!;
        ViewBag.CurrentUserId = userId;

        // Converte a string do filtro para o Enum TicketState (se não for nula)
        TicketState? state = null;
        if (!string.IsNullOrEmpty(stateFilter) && Enum.TryParse<TicketState>(stateFilter, out var parsedState))
        {
            state = parsedState;
        }

        // Recupera o histórico filtrado
        var history = await _ticketService.QueryHistoryAsync(userId, searchString, state, flowFilter, dateFilter);

        return PartialView("_TicketHistoryRows", history);
    }
}
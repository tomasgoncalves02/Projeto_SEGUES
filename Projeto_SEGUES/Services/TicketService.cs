using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Service implementation for managing Meal Tickets.
/// Handles the ticket lifecycle, including dynamic pricing, acquisition, 
/// peer-to-peer transfers, expiration logic, and canteen validation.
/// </summary>
public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TicketService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TicketService"/> class.
    /// </summary>
    /// <param name="context">The primary database context.</param>
    /// <param name="logger">The application logger.</param>
    public TicketService(AppDbContext context, ILogger<TicketService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Active Tickets

    /// <summary>
    /// Retrieves all valid, unused, and non-expired tickets for a specific user.
    /// Triggers a local expiration check before fetching.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of available tickets for use or transfer.</returns>
    public async Task<List<Ticket>> GetActiveTicketsAsync(string userId)
    {
        // Ensure we update expired tickets before fetching
        await ExpireUserTicketsAsync(userId);
        var now = DateTime.Now;
        return await _context.Ticket
            .Include(t => t.TicketPurchase)
            .Where(t => t.Owner.Id == userId && t.State == TicketState.Available && t.ExpirationDate >= now)
            .OrderBy(t => t.ExpirationDate)
            .ToListAsync();
    }

    #endregion

    #region Expire Tickets

    /// <summary>
    /// Sets the state of expired tickets to 'Expired' for a specific user.
    /// Uses high-performance <c>ExecuteUpdateAsync</c> to process records directly in the database.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    private async Task ExpireUserTicketsAsync(string userId)
    {
        var now = DateTime.Now;
        // Executes directly in the database without loading entities into memory
        await _context.Ticket
            .Where(t => t.Owner.Id == userId
                        && t.State == TicketState.Available
                        && t.ExpirationDate < now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.State, TicketState.Expired));
    }

    /// <summary>
    /// Global maintenance task to mark all available tickets in the system that have passed their expiration date.
    /// </summary>
    private async Task ExpireTicketsGlobalAsync()
    {
        var now = DateTime.Now;
        // Updates ALL expired tickets in the system in one go
        await _context.Ticket
            .Where(t => t.State == TicketState.Available
                        && t.ExpirationDate < now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.State, TicketState.Expired));
    }

    #endregion

    #region Order Tickets

    /// <summary>
    /// Determines the unit price for a ticket based on the user's category and the current date.
    /// </summary>
    /// <param name="user">The user entity to evaluate.</param>
    /// <returns>The decimal value of the current price.</returns>
    public async Task<decimal> GetCurrentPriceForUserAsync(AppUser user)
    {
        // Load navigation first and extract the category id into a local variable
        await _context.Entry(user).Reference(u => u.UserCategory).LoadAsync();
        var userCategoryId = user.UserCategory.Id;

        var now = DateTime.Now;
        var price = await _context.TicketPrice
            .Where(p => p.UserCategory.Id == userCategoryId
                        && now >= p.InitialDatePrice
                        && (p.EndDatePrice == null || now <= p.EndDatePrice))
            .OrderByDescending(p => p.InitialDatePrice)
            .FirstOrDefaultAsync();

        return price?.Price ?? 0m;
    }

    /// <summary>
    /// Retrieves the complete ticket history for a user, including used and expired tickets.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of all tickets associated with the user.</returns>
    public async Task<List<Ticket>> GetUserTicketsAsync(string userId)
    {
        // Ensure we update expired tickets before fetching
        await ExpireUserTicketsAsync(userId);
        return await _context.Ticket
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .Where(t => t.Owner.Id == userId)
            .OrderByDescending(t => t.TicketPurchase.TransactionDate)
            .ToListAsync();
    }

    /// <summary>
    /// Processes a bulk purchase of meal tickets. 
    /// Checks balance, creates purchase records, generates unique codes, and updates balance atomically.
    /// </summary>
    /// <param name="userId">Buyer's ID.</param>
    /// <param name="quantity">Amount of tickets to buy.</param>
    /// <returns>A ServiceResult indicating the outcome.</returns>
    public async Task<ServiceResult> BuyTicketsAsync(string userId, int quantity)
    {
        if (quantity <= 0) return ServiceResult.Fail("Quantidade inválida.");

        var dbUser = await _context.Users
            .Include(u => u.UserCategory)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (dbUser == null) return ServiceResult.Fail("Utilizador não encontrado.");

        var now = DateTime.Now;
        var pricePerUnit = await GetCurrentPriceForUserAsync(dbUser);
        if (pricePerUnit <= 0) return ServiceResult.Fail("Preçário não disponível. Contacte a administração.");

        var totalCost = pricePerUnit * quantity;
        if (dbUser.Balance < totalCost)
            return ServiceResult.Fail("Saldo insuficiente para a operação.");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create Purchase Record
            var purchase = new TicketPurchase
            {
                AppUser = dbUser,
                Quantity = quantity,
                TransactionDate = now,
                Value = totalCost
            };
            _context.TicketPurchase.Add(purchase);

            // Register transaction in the Transaction table
            var saldoMovimento = new Transaction
            {
                User = dbUser,
                Amount = -totalCost,
                Description = $"Compra de {quantity} senha(s) de refeição",
                Reference = "Compra Interna",
                IsPaid = true,
                CreatedAt = now
            };
            _context.Transaction.Add(saldoMovimento);

            // Create Tickets
            var validity = await _context.AppConfig.Select(c => c.TicketValidityDays).FirstOrDefaultAsync();
            if (validity == 0) validity = 365;
            var expiration = now.Date.AddDays(validity).AddHours(23).AddMinutes(59).AddSeconds(59);
            string code;

            for (int i = 0; i < quantity; i++)
            {
                do
                {
                    code = Guid.NewGuid().ToString()[..8].ToUpper();
                }
                while (_context.Ticket.Any(t => t.ValidationCode == code));

                _context.Ticket.Add(new Ticket
                {
                    Owner = dbUser,
                    ExpirationDate = expiration,
                    State = TicketState.Available,
                    TicketPurchase = purchase,
                    ValidationCode = code
                });
            }

            // Update User Balance
            dbUser.Balance -= totalCost;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            _logger.LogAppUser($"{quantity} ticket(s) bought by {dbUser.UserName} ({totalCost:C}).", UserAction.TicketPurchase);
            return ServiceResult.Ok("Compra realizada.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogAppError(AppErrors.DatabaseUpdateError, TableName.Ticket, AppOperation.Create, ex);
            return ServiceResult.Fail("Erro ao processar a compra.");
        }
    }

    #endregion

    #region Report Tickets

    /// <summary>
    /// Constructs the base query for ticket history reports, ensuring expiration checks are performed.
    /// </summary>
    private async Task<IQueryable<Ticket>> BuildTicketHistoryBaseQuery(string? userId = null)
    {
        // Ensure we update expired tickets before fetching
        if (userId == null)
        {
            await ExpireTicketsGlobalAsync();
        }
        else
        {
            await ExpireUserTicketsAsync(userId);
        }

        var query = _context.Ticket
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .Include(t => t.Transfers).ThenInclude(tr => tr.Sender)
            .Include(t => t.Transfers).ThenInclude(tr => tr.Receiver)
            .AsNoTracking()
            .AsQueryable();

        // Get tickets owned by the user or transferred to/from the user
        if (userId != null)
            query = query.Where(t => t.Owner.Id == userId || t.Transfers.Any(tr => tr.Sender.Id == userId || tr.Receiver.Id == userId));

        return query;
    }

    /// <summary>
    /// Applies sophisticated search filters for tickets, including flow (sent/received) and ownership.
    /// </summary>
    private IQueryable<Ticket> ApplyTicketHistorySearchFilters(IQueryable<Ticket> query, ReportTicketSearchViewModel model, string? userId = null)
    {
        bool isAdminLog = (userId == null);
        var searchString = model.SearchString?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(t =>
                t.ValidationCode.ToLower().Contains(searchString) ||
                (isAdminLog && (t.Owner.FirstName + " " + t.Owner.LastName).ToLower().Contains(searchString)) ||
                (isAdminLog && (t.Owner.Email!.ToLower().Contains(searchString))) ||
                t.Transfers.Any(tr =>
                    (tr.Sender.FirstName + " " + tr.Sender.LastName).ToLower().Contains(searchString) ||
                    (tr.Receiver.FirstName + " " + tr.Receiver.LastName).ToLower().Contains(searchString)
                )
            );
        }

        if (model.DateFilter.HasValue)
        {
            // From date forward
            query = query.Where(t => t.TicketPurchase.TransactionDate.Date >= model.DateFilter.Value.Date);
        }

        if (model.StateFilter.HasValue)
        {
            if (isAdminLog)
            {
                query = query.Where(t => t.State == model.StateFilter.Value);
            }
            else
            {
                // Exclude transferred tickets from state filter
                query = query.Where(t => t.State == model.StateFilter.Value && t.Owner.Id == userId);
            }
        }

        if (model.FlowFilter.HasValue)
        {
            switch (model.FlowFilter.Value)
            {
                case TicketFlow.Bought:
                    if (isAdminLog)
                    {
                        query = query.Where(t => t.TicketPurchase.AppUser.Id == t.Owner.Id);
                    }
                    else
                    {
                        query = query.Where(t => t.TicketPurchase.AppUser.Id == userId);
                    }
                    break;
                case TicketFlow.Sent:
                    if (isAdminLog)
                    {
                        query = query.Where(t => t.Transfers.Any(tr => tr.Sender.Id == t.Owner.Id));
                    }
                    else
                    {
                        query = query.Where(t => t.Transfers.Any(tr => tr.Sender.Id == userId));
                    }
                    break;
                case TicketFlow.Received:
                    if (isAdminLog)
                    {
                        query = query.Where(t => t.Transfers.Any(tr => tr.Receiver.Id == t.Owner.Id));
                    }
                    else
                    {
                        query = query.Where(t => t.Transfers.Any(tr => tr.Receiver.Id == userId));
                    }
                    break;
            }
        }

        return query;
    }

    /// <summary>
    /// Retrieves a filtered list of tickets for history reporting.
    /// </summary>
    public async Task<List<Ticket>> GetTicketHistoryAsync(string? userId, ReportTicketSearchViewModel model)
    {
        var query = await BuildTicketHistoryBaseQuery(userId);
        query = ApplyTicketHistorySearchFilters(query, model, userId);
        return await query.OrderByDescending(t => t.TicketPurchase.TransactionDate).ToListAsync();
    }

    #endregion

    #region Transfer Tickets

    /// <summary>
    /// Validates if a user is eligible to receive a ticket from another user.
    /// Checks category equality to maintain subsidized pricing integrity.
    /// </summary>
    public async Task<ServiceResult<string>> CheckTransferEligibilityAsync(string senderId, string recipientEmail)
    {
        var sender = await _context.Users
            .Include(u => u.UserCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == senderId);
        if (sender == null) return ServiceResult<string>.Fail("Utilizador remetente não encontrado.");

        if (sender.Email!.Equals(recipientEmail, StringComparison.OrdinalIgnoreCase))
            return ServiceResult<string>.Fail("Não pode transferir senhas para si próprio.");

        var recipient = await _context.Users
            .Include(u => u.UserCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == recipientEmail);

        if (recipient == null) return ServiceResult<string>.Fail("Não foi encontrado nenhum utilizador com esse e-mail.");

        if (sender.UserCategory.Id != recipient.UserCategory.Id)
            return ServiceResult<string>.Fail($"Transferência recusada: Só pode enviar senhas para utilizadores da categoria {sender.UserCategory.Name}. O destinatário é {recipient.UserCategory.Name}.");

        return ServiceResult<string>.Ok("Transferência válida.", $"{recipient.FirstName} {recipient.LastName}");
    }

    /// <summary>
    /// Executes the peer-to-peer transfer of selected tickets.
    /// Wraps owner changes and transfer records in a database transaction.
    /// </summary>
    public async Task<ServiceResult> TransferTicketsAsync(string senderId, string recipientEmail, List<string> selectedTickets)
    {
        if (selectedTickets.Count == 0)
            return ServiceResult.Fail("Nenhuma senha foi selecionada.");

        var eligibilityCheck = await CheckTransferEligibilityAsync(senderId, recipientEmail);
        if (!eligibilityCheck.Success) return ServiceResult.Fail(eligibilityCheck.Message);

        // Get tickets to transfer: must be owned by sender, in available state, and match selected codes
        var ticketsToTransfer = await _context.Ticket
            .Include(t => t.Owner)
            .Where(t => t.Owner.Id == senderId
                     && selectedTickets.Contains(t.ValidationCode)
                     && t.State == TicketState.Available)
            .ToListAsync();

        if (ticketsToTransfer.Count != selectedTickets.Count)
        {
            return ServiceResult.Fail("Algumas das senhas selecionadas já não estão disponíveis ou já não lhe pertencem. Por favor, atualize a página.");
        }

        // Load users
        var sender = await _context.Users.FindAsync(senderId);
        var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Email == recipientEmail);

        if (sender == null || receiver == null)
            return ServiceResult.Fail("Erro inesperado ao processar os dados dos utilizadores.");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var ticket in ticketsToTransfer)
            {
                ticket.Owner = receiver;

                var transferRecord = new TicketTransfer
                {
                    Ticket = ticket,
                    Sender = sender,
                    Receiver = receiver
                };
                _context.TicketTransfer.Add(transferRecord);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogAppUser($"User {sender.UserName} transfered {ticketsToTransfer.Count} ticket(s) to {receiver.UserName}", UserAction.TransferTicket);
            return ServiceResult.Ok($"{ticketsToTransfer.Count} senha(s) transferida(s) com sucesso!");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogAppError(AppErrors.DatabaseUpdateError, TableName.TicketTransfer, AppOperation.Update, ex);
            return ServiceResult.Fail("Ocorreu um erro interno ao processar a transferência.");
        }
    }

    #endregion

    #region Validate Tickets

    /// <summary>
    /// Retrieves a list of recently used tickets for staff monitoring purposes.
    /// </summary>
    /// <param name="take">Number of records to retrieve.</param>
    [Authorize(Roles = "Admin, Employee")]
    public async Task<List<Ticket>> GetRecentUsedTicketsAsync(int take = 10)
    {
        return await _context.Ticket
            .Include(t => t.Owner)
            .Where(t => t.State == TicketState.Used)
            .OrderByDescending(t => t.UsedDate)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>
    /// Validates a ticket code presented at the canteen.
    /// Marks the ticket as used, records the usage date and the staff member responsible.
    /// </summary>
    /// <param name="code">Validation code presented by the user.</param>
    /// <param name="validator">The staff member performing the validation.</param>
    [Authorize(Roles = "Admin, Employee")]
    public async Task<ServiceResult> ValidateTicketAsync(string code, AppUser validator)
    {
        if (string.IsNullOrWhiteSpace(code)) return ServiceResult.Fail("Código inválido.");

        var ticket = await _context.Ticket
            .Include(t => t.Owner)
            .FirstOrDefaultAsync(t => t.ValidationCode == code.ToUpper());

        if (ticket == null) return ServiceResult.Fail("Bilhete não encontrado.");

        if (ticket.State == TicketState.Used)
            return ServiceResult.Fail($"Bilhete já utilizado em {ticket.UsedDate:dd/MM HH:mm}.");

        if (ticket.State == TicketState.Expired || ticket.ExpirationDate < DateTime.Now)
        {
            // Auto-update to expired if it wasn't already
            if (ticket.State != TicketState.Expired)
            {
                ticket.State = TicketState.Expired;
                await _context.SaveChangesAsync();
            }
            return ServiceResult.Fail("Bilhete expirado.");
        }

        if (ticket.State != TicketState.Available)
            return ServiceResult.Fail("Bilhete não está disponível (Cancelado ou Pendente).");

        // Success - Consume the ticket
        ticket.State = TicketState.Used;
        ticket.UsedDate = DateTime.Now;
        ticket.ValidatedBy = validator;
        ticket.IsUsed = true;

        await _context.SaveChangesAsync();
        _logger.LogAppUser($"User {validator.UserName} validated ticket {ticket.ValidationCode}", UserAction.ValidateTicket);
        return ServiceResult.Ok("Bilhete Válido.");
    }

    #endregion
}
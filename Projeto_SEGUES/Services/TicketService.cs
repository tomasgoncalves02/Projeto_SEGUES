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

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TicketService> _logger;

    public TicketService(AppDbContext context, ILogger<TicketService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    #region Active Tickets
    
    // Get only active (available and not expired) tickets for a user
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
    
    #region Admin Logs
    
    // Get All Tickets (admin)
    [Authorize(Roles = "Admin")]
    public async Task<List<Ticket>> GetAllTicketsAsync()
    {
        await ExpireTicketsGlobalAsync();

        return await _context.Ticket
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .Include(t => t.Transfers)
            .ThenInclude(tr => tr.Sender)
            .Include(t => t.Transfers)
            .ThenInclude(tr => tr.Receiver)
            .OrderByDescending(t => t.TicketPurchase.TransactionDate)
            .ToListAsync();
    }
    
    #endregion
    
    #region Expire Tickets
    
    // Set expired tickets state to expired
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
    
    // Get current price for a user based on their category and current date
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
    
    // Get all tickets for a user (includes expired and used)
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
    
    // Buy Tickets: checks balance, creates purchase record, creates tickets, updates user balance
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
            var expiration = now.AddDays(validity);
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
    
    // Query History (Search/Filter)
    public async Task<List<Ticket>> QueryHistoryAsync(string userId, ReportTicketSearchViewModel model)
    {
        // Ensure we update expired tickets before fetching
        await ExpireUserTicketsAsync(userId);

        // Get tickets owned by the user or transferred to/from the user
        var query = _context.Ticket
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .Include(t => t.Transfers).ThenInclude(tr => tr.Sender)
            .Include(t => t.Transfers).ThenInclude(tr => tr.Receiver)
            .Where(t => t.Owner.Id == userId || t.Transfers.Any(tr => tr.Sender.Id == userId || tr.Receiver.Id == userId))
            .AsQueryable();

        var dateFilter = model.DateFilter;
        // PurchaseDate filter
        if (dateFilter.HasValue)
        {
            // Show PurchaseDate >= date
            query = query.Where(t => t.TicketPurchase.TransactionDate.Date >= dateFilter.Value.Date);
        }

        var searchString = model.SearchString;
        // Search filter for code
        if (!string.IsNullOrEmpty(searchString))
        {
            var upperSearch = searchString.ToUpper();
            query = query.Where(t =>
                t.ValidationCode.Contains(upperSearch) ||
                t.Transfers.Any(tr =>
                    tr.Receiver.FirstName.Contains(searchString) ||
                    tr.Receiver.LastName.Contains(searchString) ||
                    tr.Sender.FirstName.Contains(searchString) ||
                    tr.Sender.LastName.Contains(searchString)
                )
            );
        }

        var stateFilter = model.StateFilter;
        // State filter (Disponível, Usado, Expirado)
        if (stateFilter.HasValue)
        {
            // Exclude transferred tickets from state filter
            query = query.Where(t => t.State == stateFilter.Value && t.Owner.Id == userId);
        }

        var flowFilter = model.FlowFilter;
        // Flow filter (Compradas, Enviadas, Recebidas)
        if (flowFilter.HasValue)
        {
            switch (flowFilter)
            {
                case TicketFlow.Bought:
                    query = query.Where(t => t.TicketPurchase.AppUser.Id == userId);
                    break;
                case TicketFlow.Sent:
                    query = query.Where(t => t.Transfers.Any(tr => tr.Sender.Id == userId));
                    break;
                case TicketFlow.Received:
                    query = query.Where(t => t.Transfers.Any(tr => tr.Receiver.Id == userId));
                    break;
            }
        }

        return await query.OrderByDescending(t => t.TicketPurchase.TransactionDate).ToListAsync();
    }
    
    #endregion

    #region Transfer Tickets
    
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

    // Get recent used tickets (for admin/employee dashboard)
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

    // Validate Ticket: checks code, updates the state to the used state if valid
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
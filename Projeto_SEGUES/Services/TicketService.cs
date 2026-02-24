using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;

    public TicketService(AppDbContext context)
    {
        _context = context;
    }

    // Set expired tickets state to expired
    private async Task ExpireUserTicketsAsync(string userId)
    {
        var now = DateTime.Now;
        // Executes directly in the database without loading entities into memory
        await _context.Tickets
            .Where(t => t.Owner.Id == userId 
                        && t.State == TicketState.Available 
                        && t.ExpirationDate < now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.State, TicketState.Expired));
    }
    
    private async Task ExpireTicketsGlobalAsync()
    {
        var now = DateTime.Now;
        // Updates ALL expired tickets in the system in one go
        await _context.Tickets
            .Where(t => t.State == TicketState.Available 
                        && t.ExpirationDate < now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.State, TicketState.Expired));
    }

    // Get all tickets for a user (includes expired and used)
    public async Task<List<Ticket>> GetUserTicketsAsync(string userId)
    {
        // Ensure we update expired tickets before fetching
        await ExpireUserTicketsAsync(userId);
        return await _context.Tickets
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .Where(t => t.Owner.Id == userId)
            .OrderByDescending(t => t.TicketPurchase.TransactionDate)
            .ToListAsync();
    }

    // Get only active (available and not expired) tickets for a user
    public async Task<List<Ticket>> GetActiveTicketsAsync(string userId)
    {
        // Ensure we update expired tickets before fetching
        await ExpireUserTicketsAsync(userId);
        var now = DateTime.Now;
        return await _context.Tickets
            .Include(t => t.TicketPurchase)
            .Where(t => t.Owner.Id == userId && t.State == TicketState.Available && t.ExpirationDate >= now)
            .OrderBy(t => t.ExpirationDate)
            .ToListAsync();
    }

    // Get recent used tickets (for admin/employee dashboard)
    [Authorize(Roles = "Admin, Employee")]
    public async Task<List<Ticket>> GetRecentUsedTicketsAsync(int take = 10)
    {
        return await _context.Tickets
            .Include(t => t.Owner)
            .Where(t => t.State == TicketState.Used)
            .OrderByDescending(t => t.UsedDate)
            .Take(take)
            .ToListAsync();
    }

    // Get current price for a user based on their category and current date
    public async Task<decimal> GetCurrentPriceForUserAsync(AppUser user)
    {
        // Load navigation first and extract the category id into a local variable
        await _context.Entry(user).Reference(u => u.UserCategory).LoadAsync();
        var userCategoryId = user.UserCategory?.Id;
        if (userCategoryId == null) return 0m;
        
        var now = DateTime.Now;
        var price = await _context.TicketPrices
            .Where(p => p.UserCategory.Id == userCategoryId
                        && now >= p.InitialDatePrice 
                        && now <= p.EndDatePrice)
            .OrderByDescending(p => p.InitialDatePrice)
            .FirstOrDefaultAsync();

        return price?.Price ?? 0m;
    }

    // Buy Tickets: checks balance, creates purchase record, creates tickets, updates user balance
    public async Task<ServiceResult> BuyTicketsAsync(string userId, int quantity)
    {
        if (quantity <= 0) return ServiceResult.Fail("Quantidade inválida.");

        // Loads user from DB to ensure Fresh Balance and Category.
        // Includes UserCategory for pricing.
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
            _context.TicketPurchases.Add(purchase);

            // Get expiration date
            var validity = await _context.AppConfigs.Select(c => c.TicketValidityDays).FirstOrDefaultAsync();
            var expiration = now.AddDays(validity);

            for (int i = 0; i < quantity; i++)
            {
                _context.Tickets.Add(new Ticket
                {
                    Owner = dbUser,
                    ExpirationDate = expiration,
                    State = TicketState.Available,
                    TicketPurchase = purchase,
                    ValidationCode = Guid.NewGuid().ToString()[..8].ToUpper()
                });
            }

            dbUser.Balance -= totalCost;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Ok("Compra realizada.");
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            return ServiceResult.Fail("Erro ao processar a compra.");
        }
    }
        
    // Validate Ticket: checks code, updates state to used if valid, returns result message
    [Authorize(Roles = "Admin, Employee")]
    public async Task<ServiceResult> ValidateTicketAsync(string code, AppUser validator)
    {
        if (string.IsNullOrWhiteSpace(code)) return ServiceResult.Fail("Código inválido.");

        var ticket = await _context.Tickets
            .Include(t => t.Owner)
            .FirstOrDefaultAsync(t => t.ValidationCode == code.ToUpper());

        if (ticket == null) return ServiceResult.Fail("Bilhete não encontrado.");

        if (ticket.State == TicketState.Used)
            return ServiceResult.Fail($"Bilhete já utilizado em {ticket.UsedDate:dd/MM HH:mm}.");

        if (ticket.State == TicketState.Expired || ticket.ExpirationDate < DateTime.Now)
        {
            // Auto-update to expired if it wasn't already
            if(ticket.State != TicketState.Expired) 
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
        return ServiceResult.Ok("Bilhete Válido.");
    }

    // Query History (Search/Filter)
    public async Task<List<Ticket>> QueryHistoryAsync(string userId, string searchString, TicketState? stateFilter, string flowFilter, DateTime? dateFilter)
    {   
        // Ensure we update expired tickets before fetching
        await ExpireUserTicketsAsync(userId);

        // Get tickets owned by the user or transferred to/from the user
        var query = _context.Tickets
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .Include(t => t.Transfers).ThenInclude(tr => tr.Sender)
            .Include(t => t.Transfers).ThenInclude(tr => tr.Receiver)
            .Where(t => t.Owner.Id == userId || t.Transfers.Any(tr => tr.Sender.Id == userId || tr.Receiver.Id == userId))
            .AsQueryable();

        // PurchaseDate filter
        if (dateFilter.HasValue)
        {
            // Show PurchaseDate >= date
            query = query.Where(t => t.TicketPurchase.TransactionDate.Date >= dateFilter.Value.Date);
        }

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

        // State filter (Disponível, Usado, Expirado)
        if (stateFilter.HasValue)
        {
            query = query.Where(t => t.State == stateFilter.Value);
        }

        // Flow filter (Compradas, Enviadas, Recebidas)
        if (!string.IsNullOrEmpty(flowFilter))
        {
            switch (flowFilter)
            {
                case "Compradas":
                    query = query.Where(t => t.Owner.Id == userId && t.Transfers.All(tr => tr.Receiver.Id != userId));
                    break;
                case "Enviadas":
                    query = query.Where(t => t.Transfers.Any(tr => tr.Sender.Id == userId));
                    break;
                case "Recebidas":
                    query = query.Where(t => t.Transfers.Any(tr => tr.Receiver.Id == userId));
                    break;
            }
        }

        return await query.OrderByDescending(t => t.TicketPurchase.TransactionDate).ToListAsync();
    }

    // Get All Tickets (admin)
    [Authorize(Roles = "Admin")]
    public async Task<List<Ticket>> GetAllTicketsAsync()
    {
        await ExpireTicketsGlobalAsync();

        return await _context.Tickets
            .Include(t => t.Owner)                      
            .Include(t => t.TicketPurchase)             
            .Include(t => t.Transfers)                  
                .ThenInclude(tr => tr.Sender)           
            .Include(t => t.Transfers)
                .ThenInclude(tr => tr.Receiver)      
            .OrderByDescending(t => t.TicketPurchase.TransactionDate)
            .ToListAsync();
    }




    public async Task<ServiceResult> TransferTicketsAsync(string senderId, string recipientEmail, List<string> selectedTickets)
    {
        // 1. Carregar Sender e Receiver com suas CATEGORIAS (usando Include para evitar NullReference)
        var sender = await _context.Users
            .Include(u => u.UserCategory)
            .FirstOrDefaultAsync(u => u.Id == senderId);

        var receiver = await _context.Users
            .Include(u => u.UserCategory)
            .FirstOrDefaultAsync(u => u.Email == recipientEmail);

        if (sender == null) return ServiceResult.Fail("Utilizador remetente não encontrado.");
        if (receiver == null) return ServiceResult.Fail("Não foi encontrado nenhum utilizador com esse e-mail.");

        if (sender.Email!.Equals(recipientEmail, StringComparison.OrdinalIgnoreCase))
            return ServiceResult.Fail("Não pode transferir senhas para si próprio.");

        // 2. NOVA REGRA: Comparar Categorias (Trabalhador, Estudante, etc.) em vez de Roles
        if (sender.UserCategory.Id != receiver.UserCategory.Id)
        {
            return ServiceResult.Fail($"Transferência recusada: Só pode enviar senhas para utilizadores da categoria {sender.UserCategory.Name}. " +
                                      $"O destinatário é {receiver.UserCategory.Name}.");
        }

        // 3. Procurar os tickets
        var ticketsToTransfer = await _context.Tickets
            .Include(t => t.Owner)
            .Where(t => t.Owner.Id == sender.Id
                     && selectedTickets.Contains(t.ValidationCode)
                     && t.State == TicketState.Available)
            .ToListAsync();

        if (!ticketsToTransfer.Any())
            return ServiceResult.Fail("As senhas selecionadas já não estão disponíveis ou não lhe pertencem.");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var ticket in ticketsToTransfer)
            {
                ticket.State = TicketState.Available;
                ticket.Owner = receiver; // O destinatário passa a ser o novo dono

                var transferRecord = new TicketTransfer
                {
                    TransferDate = DateTime.Now,
                    Ticket = ticket,
                    Sender = sender,
                    Receiver = receiver
                };
                _context.TicketTransfers.Add(transferRecord);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return ServiceResult.Ok($"{ticketsToTransfer.Count} senha(s) transferida(s) com sucesso!");
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            return ServiceResult.Fail("Ocorreu um erro interno ao processar a transferência.");
        }
    }
}
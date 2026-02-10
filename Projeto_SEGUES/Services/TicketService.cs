using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public TicketService(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Set expired tickets state to expired
    public async Task ExpireUserTicketsAsync(string userId)
    {
        var now = DateTime.Now;
        // Executes directly in the database without loading entities into memory
        await _context.Tickets
            .Where(t => t.Owner.Id == userId 
                        && t.State == TicketState.Available 
                        && t.ExpirationDate < now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.State, TicketState.Expired));
    }
    
    public async Task ExpireTicketsGlobalAsync()
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
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServiceResult.Fail("Erro ao processar a compra.");
        }
    }
        
    // Validate Ticket: checks code, updates state to used if valid, returns result message
    [Authorize(Roles = "Admin], Employee")]
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
    public async Task<List<Ticket>> QueryHistoryAsync(string userId, string searchString, TicketState? stateFilter, string flowFilter)
    {
        // Ensure we update expired tickets before fetching
        await ExpireUserTicketsAsync(userId);
        var query = _context.Tickets
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .Where(t => t.Owner.Id == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(t => t.ValidationCode.Contains(searchString.ToUpper()));
        }
        if (stateFilter.HasValue)
        {
            query = query.Where(t => t.State == stateFilter.Value);
        }

        // Apply sorting based on "flow" (just an example of how you might use this)
        if (flowFilter == "Oldest")
            query = query.OrderBy(t => t.TicketPurchase.TransactionDate);
        else
            query = query.OrderByDescending(t => t.TicketPurchase.TransactionDate);

        return await query.ToListAsync();
    }

    // Get All Tickets (admin)
    [Authorize(Roles = "Admin]")]
    public async Task<List<Ticket>> GetAllTicketsAsync()
    {
        await ExpireTicketsGlobalAsync();
        return await _context.Tickets
            .Include(t => t.Owner)
            .Include(t => t.TicketPurchase)
            .OrderByDescending(t => t.TicketPurchase.TransactionDate)
            .ToListAsync();
    }
}
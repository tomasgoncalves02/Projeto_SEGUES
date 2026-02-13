using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Bar;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Services;

public class BarService : IBarService
{
    private readonly AppDbContext _context;

    public BarService(AppDbContext context) => _context = context;

    public async Task<decimal> GetBalanceAsync(string userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user?.Balance ?? 0m;
    }

    public async Task<List<Product>> GetAvailableProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.Category.Name == "Bar" && p.IsActive && p.Stock > 0)
            .ToListAsync();
    }

    public async Task<List<BarOrder>> GetOrderHistoryAsync(string userId)
    {
        return await _context.BarOrders
            .Include(o => o.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<(bool Succeeded, string Message)> PlaceOrderAsync(string userId, int productId)
    {
        var user = await _context.Users.FindAsync(userId);
        var product = await _context.Products.FindAsync(productId);

        if (user == null || product == null) return (false, "Utilizador ou Produto não encontrado.");
        if (user.Balance < product.Price) return (false, "Saldo insuficiente.");
        if (product.Stock <= 0) return (false, "Produto esgotado.");

        // Lógica de transação
        user.Balance -= product.Price;
        product.Stock--;

        var order = new BarOrder
        {
            UserId = userId,
            ProductId = productId,
            PriceAtTime = product.Price
        };

        _context.BarOrders.Add(order);
        await _context.SaveChangesAsync();

        return (true, $"Compra de {product.Name} realizada com sucesso!");
    }
}
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private static readonly OrderStatus[] _activeStatus = Enum.GetValues<OrderStatus>().Where(s => s.IsActive()).ToArray();
    
    public OrderService(AppDbContext context) => _context = context;

    public async Task<Order> GetCartAsync(string userId)
    {
        var cart = await _context.Orders
            .Include(o => o.ProductPurchases)
            .ThenInclude(ol => ol.Product)
            .FirstOrDefaultAsync(o => o.AppUser.Id == userId && o.Status == OrderStatus.Cart);
        if (cart != null) return cart;
        
        var user = await _context.Users.FindAsync(userId);
        cart = new Order 
        {
            AppUser = user!
        };
        _context.Orders.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public decimal ApplyDiscount(decimal price, Discount? discount)
    {
        if (discount is not { IsActive: true } || discount.EndDate < DateTime.Now) return price;
        if (discount.DiscountType == DiscountType.Percentage)
        {
            return price * (1 + discount.Value / 100);
        }
        return price - discount.Value; // Fixed amount discount
    }

    public OrderTotalViewModel GetOrderTotal(Order cart)
    {
        return new OrderTotalViewModel
        {
            TotalQuantity = cart.ProductPurchases.Sum(ol => ol.Quantity),
            TotalValue = cart.TotalValue
        };
    }
    
    public async Task<ServiceResult> AddToCartAsync(string userId, int productId, int quantity)
    {
        var cart = await GetCartAsync(userId);
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return ServiceResult.Fail("Produto não encontrado.", GetOrderTotal(cart));
        
        var line = await _context.OrderLines.FirstOrDefaultAsync(ol => ol.Order.Id == cart.Id && ol.ProductId == productId);
        if (line != null)
        {
            line.Quantity += quantity;
            cart.TotalValue += quantity * line.ProductValue;
        }
        else
        {
            var discount = await _context.Discounts
                .Where(d => d.IsActive && !d.IsGlobal && d.StartDate <= DateTime.Now && d.EndDate > DateTime.Now)
                .FirstOrDefaultAsync(d => d.Products.Any(p => p.Id == productId));
            var productValue = ApplyDiscount(product.Price, discount);
            _context.OrderLines.Add(
                new OrderLine {
                    OrderId = cart.Id,
                    Order = cart,
                    ProductId = productId,
                    Product = product,
                    Quantity = quantity,
                    ProductValue = productValue,
                    Discount = discount
                });
            cart.TotalValue += quantity * productValue;
        }
        
        await _context.SaveChangesAsync();
        return ServiceResult.Ok("Produto adicionado ao carrinho.", GetOrderTotal(cart));
    }

    public async Task<ServiceResult> RemoveFromCartAsync(string userId, int productId)
    {
        var cart = await GetCartAsync(userId);
        var line = await _context.OrderLines.FirstOrDefaultAsync(ol => ol.Order.Id == cart.Id && ol.ProductId == productId);
        if (line == null) return ServiceResult.Fail("Produto não encontrado no carrinho.");
        
        _context.OrderLines.Remove(line);
        await _context.SaveChangesAsync();
        return ServiceResult.Ok("Produto removido do carrinho.", GetOrderTotal(cart));
    }
    
    public async Task<ServiceResult> SubmitOrderAsync(AppUser user, bool receiveNow, string? pickupTime)
    {
        var cart = await GetCartAsync(user.Id);
        if (cart.ProductPurchases.Count == 0) return ServiceResult.Fail("O carrinho está vazio.");
        
        decimal total = ApplyDiscount(cart.TotalValue, cart.Discount);
        if (user.Balance < total) return ServiceResult.Fail("Saldo insuficiente.");
        
        // Validate stock
        foreach (var item in cart.ProductPurchases)
        {
            if (item.Product.Stock < item.Quantity)
                return ServiceResult.Fail($"Stock insuficiente para o produto: {item.Product.Name}");
        }

        // Handle Delivery Time
        TimeSpan? deliveryTime = null;
        if (!receiveNow && TimeSpan.TryParse(pickupTime, out var parsedTime))
        {
            if (DateTime.Today.Add(parsedTime) < DateTime.Now)
                return ServiceResult.Fail("Não é possível agendar para o passado.");
            deliveryTime = parsedTime;
        }
        
        // Recreate RedemptionCode
        while (_context.Orders.Any(o => o.RedemptionCode == cart.RedemptionCode))
        {
            cart.RedemptionCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            cart.Status = OrderStatus.Pending;
            cart.TotalValue = total;
            cart.OrderDate = DateTime.Now;
            cart.DeliveryTime = deliveryTime;

            foreach (var item in cart.ProductPurchases)
            {
                item.Product.Stock -= item.Quantity;
                item.ProductValue = ApplyDiscount(item.Product.Price, item.Discount); // Ensure latest
            }

            // Update User Balance
            user.Balance -= total;
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult.Ok("Encomenda realizada com sucesso!");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult.Fail("Ocorreu um erro ao processar a encomenda.");
        }
    }

    public async Task<ServiceResult> CancelOrderAsync(int id)
    {
        var order = await GetOrderByIdAsync(id);
        if (order == null) return ServiceResult.Fail("Pedido não encontrado.");
        if (order.Status != OrderStatus.Pending) return ServiceResult.Fail("Pedido não pode ser cancelado.");
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            order.Status = OrderStatus.Cancelled;
            foreach (var item in order.ProductPurchases)
            {
                item.Product.Stock += item.Quantity; // Readd stock
            }
            order.AppUser.Balance += order.TotalValue; // Refund user
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return ServiceResult.Ok("Pedido cancelado com sucesso e reembolso efetuado.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult.Fail("Ocorreu um erro ao cancelar o pedido.");
        }
    }
    
    public async Task<List<Order>> GetActiveOrdersAsync(string userId)
    {
        return await _context.Orders
            .Where(o => o.AppUser.Id == userId && _activeStatus.Contains(o.Status))
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        // Includes products info and user
        return await _context.Orders
            .Include(o => o.ProductPurchases)
            .ThenInclude(ol => ol.Product)
            .Include(o => o.AppUser)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    
    public async Task<List<Order>> GetOrderHistoryAsync(string userId)
    {
        // Doesn't include products info
        return await _context.Orders
            .Where(o => o.AppUser.Id == userId && o.Status != OrderStatus.Cart)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<List<Order>> GetUndeliveredOrdersAsync()
    {
        // Doesn't include products info, includes User, active orders
        return await _context.Orders
            .Include(o => o.AppUser)
            .Where(o => _activeStatus.Contains(o.Status))
            .OrderBy(o => o.PickupTime == TimeSpan.Zero ? o.OrderDate.TimeOfDay : o.PickupTime)
            .ToListAsync();
    }
    
    public async Task<List<Order>> GetAdminOrderHistoryAsync(string userId)
    {
        // Doesn't include products info, includes User
        return await _context.Orders
            .Where(o => o.AppUser.Id == userId && o.Status != OrderStatus.Cart)
            .Include(o => o.AppUser)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<ServiceResult> UpdateOrderStatusAsync(int id, int newStatusId)
    {
        var order = await GetOrderByIdAsync(id);
        if (order == null || !_activeStatus.Contains(order.Status)) return ServiceResult.Fail("Pedido não encontrado.");

        OrderStatus newStatus = (OrderStatus) newStatusId;

        if (!Enum.IsDefined(typeof(OrderStatus), newStatus) || newStatus == OrderStatus.Cart)
            return ServiceResult.Fail("Status inválido.");
       
        if (!_activeStatus.Contains(newStatus) && newStatus != OrderStatus.Delivered)
        {
            return ServiceResult.Fail("Não é possível mudar para este estado.");
        }
        
        if (newStatus != order.Status + 1)
            return ServiceResult.Fail("Transição de status inválida.");
     
        if (newStatus == OrderStatus.Cancelled)
            return ServiceResult.Fail("Use a função específica para cancelar pedidos.");

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Status do pedido atualizado com sucesso.");
    }

    public async Task<ServiceResult> ValidateOrderCodeAsync(int id, string codeEntered)
    {
        var order = await GetOrderByIdAsync(id);
        if (order == null) return ServiceResult.Fail("Pedido não encontrado.");
        
        if (!string.Equals(order.RedemptionCode.Trim(), codeEntered.Trim(), StringComparison.CurrentCultureIgnoreCase))
            return ServiceResult.Fail("Código inválido!");
        
        order.Status = OrderStatus.Delivered;
        order.PickupTime = DateTime.Now.TimeOfDay;
        await _context.SaveChangesAsync();
        return ServiceResult.Ok("Código validado e pedido marcado como entregue.");
    }
}
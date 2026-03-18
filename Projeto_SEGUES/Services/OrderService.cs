//using Castle.Core.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Audit;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IAdminService _adminService;
    private static readonly OrderStatus[] _activeStatus = Enum.GetValues<OrderStatus>().Where(s => s.IsActive()).ToArray();
    private readonly IEmailSender _emailSender;

    public OrderService(AppDbContext context, IAdminService adminService, IEmailSender emailSender)
    {
        _context = context;
        _adminService = adminService;
        _emailSender = emailSender;
    }

    public async Task<Order?> GetCartAsync(string userId, bool createIfNotFound = true)
    {
        var cart = await _context.Order
            .Include(o => o.ProductPurchases)
            .ThenInclude(ol => ol.Product)
            .FirstOrDefaultAsync(o => o.AppUser.Id == userId && o.Status == OrderStatus.Cart);
        if (cart != null || !createIfNotFound) return cart;

        var user = await _context.Users.FindAsync(userId);
        cart = new Order
        {
            AppUser = user!
        };
        _context.Order.Add(cart);
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
        var product = await _context.Product.FindAsync(productId);
        if (product == null) return ServiceResult.Fail("Produto não encontrado.", GetOrderTotal(cart));

        var line = await _context.OrderLine.FirstOrDefaultAsync(ol => ol.Order.Id == cart.Id && ol.ProductId == productId);
        if (line != null)
        {
            line.Quantity += quantity;
            cart.TotalValue += quantity * line.ProductValue;
        }
        else
        {
            var discount = await _context.Discount
                .Where(d => d.IsActive && !d.IsGlobal && d.StartDate <= DateTime.Now && d.EndDate > DateTime.Now)
                .FirstOrDefaultAsync(d => d.Products.Any(p => p.Id == productId));
            var productValue = ApplyDiscount(product.Price, discount);
            _context.OrderLine.Add(
                new OrderLine
                {
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
        var line = await _context.OrderLine
            .FirstOrDefaultAsync(ol => ol.Order.Id == cart.Id && ol.ProductId == productId);

        if (line == null) return ServiceResult.Fail("Produto não encontrado no carrinho.");
        cart.TotalValue -= (line.Quantity * line.ProductValue);
        if (cart.TotalValue < 0) cart.TotalValue = 0;

        _context.OrderLine.Remove(line);

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Produto removido do carrinho.", GetOrderTotal(cart));
    }

    public async Task<ServiceResult> SubmitOrderAsync(AppUser user, bool receiveNow, string? pickupTime)
    {
        var cart = await GetCartAsync(user.Id);
        if (cart.ProductPurchases.Count == 0) return ServiceResult.Fail("O carrinho está vazio.");

        TimeSpan timeToValidate;
        TimeSpan? deliveryTime = null;

        if (receiveNow)
        {
            timeToValidate = DateTime.Now.TimeOfDay;
        }
        else
        {
            if (TimeSpan.TryParse(pickupTime, out var parsedTime))
            {
                if (DateTime.Today.Add(parsedTime) < DateTime.Now)
                    return ServiceResult.Fail("Não é possível agendar para o passado.");

                deliveryTime = parsedTime;
                timeToValidate = parsedTime;
            }
            else
            {
                return ServiceResult.Fail("Horário de pickup inválido.");
            }
        }

        if (!await _adminService.IsBarOpenAsync(timeToValidate))
        {
            var open = await _adminService.GetOpenBarTimeAsync();
            var close = await _adminService.GetCloseBarTimesAsync();
            return ServiceResult.Fail($"O Bar encontra-se encerrado para o horário selecionado. Funcionamento: {open:hh\\:mm} às {close:hh\\:mm}.");
        }

        decimal total = ApplyDiscount(cart.TotalValue, cart.Discount);
        if (user.Balance < total) return ServiceResult.Fail("Saldo insuficiente.");

        foreach (var item in cart.ProductPurchases)
        {
            if (item.Product.Stock < item.Quantity)
                return ServiceResult.Fail($"Stock insuficiente para o produto: {item.Product.Name}");
        }

        while (_context.Order.Any(o => o.RedemptionCode == cart.RedemptionCode))
        {
            cart.RedemptionCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.Now;
            cart.Status = OrderStatus.Pending;
            cart.TotalValue = total;
            cart.OrderDate = now;
            cart.DeliveryTime = deliveryTime;

            foreach (var item in cart.ProductPurchases)
            {
                item.Product.Stock -= item.Quantity;
                item.ProductValue = ApplyDiscount(item.Product.Price, item.Discount);
            }

            user.Balance -= total;

            var barTransaction = new Projeto_SEGUES.Models.Payment.Transaction
            {
                User = user,
                Amount = -total,
                Description = $"Consumo Bar - Pedido #{cart.RedemptionCode}",
                Reference = "CONSUMO BAR",
                IsPaid = true,
                CreatedAt = now
            };
            _context.Transaction.Add(barTransaction);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Envio de email após sucesso total
            try
            {
                await SendStatusUpdateEmailAsync(cart);
                // Se correr bem, mensagem normal de sucesso
                return ServiceResult.Ok("Encomenda realizada com sucesso!");
            }
            catch (Exception)
            {
                // Se falhar o email (sem net), avisamos o utilizador mas confirmamos o sucesso do pedido
                return ServiceResult.Ok("Encomenda realizada com sucesso! (Nota: Não foi possível enviar o email de confirmação devido a uma falha de rede).");
            }
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            return ServiceResult.Fail("Ocorreu um erro ao processar a encomenda.");
        }
    }

    public async Task<ServiceResult> CancelOrderAsync(int id)
    {
        var order = await GetOrderByIdAsync(id);
        if (order == null) return ServiceResult.Fail("Pedido não encontrado.");

        if (order.Status != OrderStatus.Pending)
            return ServiceResult.Fail("O pedido já está em processamento ou finalizado e não pode ser cancelado.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.Now;

            order.Status = OrderStatus.Cancelled;

            foreach (var item in order.ProductPurchases)
            {
                item.Product.Stock += item.Quantity;
            }


            order.AppUser.Balance += order.TotalValue;

            var refundTransaction = new Projeto_SEGUES.Models.Payment.Transaction
            {
                User = order.AppUser,
                Amount = order.TotalValue,
                Description = $"Reembolso Bar - Cancelamento Pedido #{order.RedemptionCode}",
                Reference = "REEMBOLSO BAR",
                IsPaid = true,
                CreatedAt = now

            };
            _context.Transaction.Add(refundTransaction);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            try
            {
                await SendStatusUpdateEmailAsync(order);
                return ServiceResult.Ok("Pedido cancelado com sucesso e saldo reembolsado.");
            }
            catch (Exception)
            {
                // Se falhar a net aqui, o user sabe que o cancelamento foi feito mas o email falhou
                return ServiceResult.Ok("Pedido cancelado com sucesso e saldo reembolsado! (Nota: O email de confirmação não pôde ser enviado por falha de rede).");
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return ServiceResult.Fail("Ocorreu um erro ao cancelar o pedido.");
        }
    }

    public async Task<List<Order>> GetActiveOrdersAsync(string userId)
    {
        return await _context.Order
            .Where(o => o.AppUser.Id == userId && _activeStatus.Contains(o.Status))
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        // Includes products info and user
        return await _context.Order
            .Include(o => o.ProductPurchases)
            .ThenInclude(ol => ol.Product)
            .ThenInclude(p => p.Category)
            .Include(o => o.AppUser)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetOrderHistoryAsync(string userId)
    {
        // Doesn't include products info
        return await _context.Order
            .Where(o => o.AppUser.Id == userId && o.Status != OrderStatus.Cart)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<List<Order>> GetUndeliveredOrdersAsync()
    {
        // Doesn't include products info, includes User, active orders
        return await _context.Order
            .Include(o => o.AppUser)
            .Where(o => _activeStatus.Contains(o.Status))
            .OrderBy(o => o.PickupTime == TimeSpan.Zero ? o.OrderDate.TimeOfDay : o.PickupTime)
            .ToListAsync();
    }

    public async Task<List<Order>> GetAdminOrderHistoryAsync()
    {
        // Removido o parâmetro 'string userId' pois o Admin quer ver TUDO
        return await _context.Order
            .Include(o => o.AppUser) // Importante para saber quem fez o pedido
            .Include(o => o.ProductPurchases) // Adicionado para poderes ver os produtos na View/PDF
                .ThenInclude(p => p.Product)
            .Where(o => o.Status != OrderStatus.Cart)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<ServiceResult> UpdateOrderStatusAsync(int id, int newStatusId, AppUser staffMember)
    {
        var order = await GetOrderByIdAsync(id);
        if (order == null || !_activeStatus.Contains(order.Status)) return ServiceResult.Fail("Pedido não encontrado.");

        OrderStatus newStatus = (OrderStatus)newStatusId;

        if (!Enum.IsDefined(typeof(OrderStatus), newStatus) || newStatus == OrderStatus.Cart)
            return ServiceResult.Fail("Status inválido.");

        if (!_activeStatus.Contains(newStatus) && newStatus != OrderStatus.Delivered)
        {
            return ServiceResult.Fail("Não é possível mudar para este estado.");
        }

        if (newStatus != order.Status + 1 && newStatus != order.Status - 1)
            return ServiceResult.Fail("Transição de status inválida.");

        if (newStatus == OrderStatus.Cancelled)
            return ServiceResult.Fail("Use a função específica para cancelar pedidos.");

        try
        {
            var oldStatus = order.Status;
            order.Status = newStatus;

            // --- ADICIONADO: REGISTO DE LOG NO HISTÓRICO ---
            var log = new UserLog
            {
                UserAction = UserAction.UpdateStatus,
                Message = $"Alterou o estado do pedido #{order.RedemptionCode} de {oldStatus} para {newStatus}.",
                TimeStamp = DateTime.Now,
                AppUser = staffMember,
                RequestPath = "/Order/UpdateStatus"
            };
            _context.UserLog.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            return ServiceResult.Fail("Erro ao atualizar o estado na base de dados.");
        }

        try
        {
            await SendStatusUpdateEmailAsync(order);
            return ServiceResult.Ok("Status do pedido atualizado com sucesso.");
        }
        catch (Exception)
        {
            return ServiceResult.Ok("Status atualizado com sucesso! (Nota: Falha ao enviar email de notificação ao cliente).");
        }
    }

    public async Task<ServiceResult> ValidateOrderCodeAsync(int id, string enteredCode, AppUser staffMember)
    {
        enteredCode = enteredCode.Trim();
        if (string.IsNullOrWhiteSpace(enteredCode))
            return ServiceResult.Fail("Por favor, insira o código de levantamento.");

        var order = await GetOrderByIdAsync(id);
        if (order == null) return ServiceResult.Fail("Pedido não encontrado.");
        
        if (!string.Equals(order.RedemptionCode, enteredCode, StringComparison.CurrentCultureIgnoreCase))
            return ServiceResult.Fail("Código inválido!");

        try
        {
            order.Status = OrderStatus.Delivered;
            order.PickupTime = DateTime.Now.TimeOfDay;

            var log = new UserLog
            {
                UserAction = UserAction.ValidateOrder,
                Message = $"Entregou o pedido #{order.RedemptionCode} ao utilizador {order.AppUser.UserName}.",
                TimeStamp = DateTime.Now,
                AppUser = staffMember,
                RequestPath = "/Order/ValidateCode"
            };
            _context.UserLog.Add(log);

            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            return ServiceResult.Fail("Erro ao registar a entrega na base de dados.");
        }

        try
        {
            await SendStatusUpdateEmailAsync(order);
            return ServiceResult.Ok("Código validado e pedido marcado como entregue.");
        }
        catch (Exception)
        {
            return ServiceResult.Ok("Código validado e pedido entregue! (Nota: Falha ao enviar o email de confirmação de entrega).");
        }
    }

    private async Task SendStatusUpdateEmailAsync(Order order)
    {
        try
        {
            if (order.AppUser == null || string.IsNullOrEmpty(order.AppUser.Email)) return;

            // Obtém o nome amigável do Enum (ex: "Em Preparação")
            var displayStatus = order.Status.GetType()
                .GetField(order.Status.ToString())?
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .Cast<DisplayAttribute>()
                .FirstOrDefault()?.Name ?? order.Status.ToString();

            string title = "Atualização do Pedido";
            string name = order.AppUser.UserName ?? "Cliente";

            // Mensagem personalizada conforme o estado
            string customMessage = order.Status switch
            {
                OrderStatus.Pending => "Recebemos o teu pedido e em breve começaremos a prepará-lo.",
                OrderStatus.Preparing => "O teu pedido já está na cozinha e a ser preparado com todo o cuidado.",
                OrderStatus.ReadyToDeliver => "Boas notícias! O teu pedido está pronto. Podes passar no bar para levantar.",
                OrderStatus.Delivered => "Pedido entregue. Esperamos que gostes!",
                OrderStatus.Cancelled => "O teu pedido foi cancelado e o respetivo valor foi reembolsado no teu saldo.",
                _ => $"O estado do teu pedido foi alterado para: {displayStatus}."
            };

            string content = $"""
        <p>O estado do teu pedido <strong>#{order.RedemptionCode}</strong> foi atualizado.</p>
        <p style='font-size: 16px; color: #009697;'><strong>Estado Atual: {displayStatus}</strong></p>
        <p>{customMessage}</p>
        """;

            // Se estiver pronto, destaca o código de levantamento para facilitar a vida ao utilizador
            if (order.Status == OrderStatus.ReadyToDeliver)
            {
                content += $"""
            <div style='background:#f4f7f6; padding:15px; border-left: 4px solid #009697; margin-top: 20px;'>
                <p style='margin:0; font-weight:bold;'>Código de Levantamento:</p>
                <span style='font-size:24px; letter-spacing: 2px; color: #009697;'>{order.RedemptionCode}</span>
            </div>
            """;
            }

            var emailService = (EmailSender)_emailSender;
            var body = ((EmailSender)_emailSender).GetEmailBody(title, name, content);

            // Envio assíncrono
            await _emailSender.SendEmailAsync(order.AppUser.Email, $"SEGUES - Pedido #{order.RedemptionCode}", body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Email Service Failure]: {ex.Message}");
            throw;
        }
    }
}
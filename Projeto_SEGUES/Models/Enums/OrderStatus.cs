using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

public enum OrderStatus : byte
{
    [Display(Name = "Carrinho")]
    Cart,
    [Display(Name = "Pendente")]
    Pending,
    [Display(Name = "Em Preparação")]
    Preparing,
    [Display(Name = "Pronto para Entrega")]
    ReadyToDeliver,
    [Display(Name = "Entregue")]
    Delivered,
    [Display(Name = "Cancelado")]
    Cancelled
}

public static class OrderStatusExtensions
{
    private static readonly OrderStatus[] ActiveStatus = 
    [
        OrderStatus.Pending, 
        OrderStatus.Preparing, 
        OrderStatus.ReadyToDeliver
    ];

    public static bool IsActive(this OrderStatus status) => ActiveStatus.Contains(status);
}
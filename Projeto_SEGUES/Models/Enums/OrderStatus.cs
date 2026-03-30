using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// Represents the various stages of an order within the SEGUES fulfillment pipeline.
/// </summary>
/// <remarks>
/// This enum tracks the transition from the initial shopping phase to the final delivery or cancellation.
/// Inherits from <see cref="byte"/> for database performance and storage optimization.
/// </remarks>
public enum OrderStatus : byte
{
    /// <summary>Initial state where items are being collected but the order is not yet finalized.</summary>
    [Display(Name = "Carrinho")]
    Cart,

    /// <summary>The order has been submitted and is awaiting confirmation or payment validation.</summary>
    [Display(Name = "Pendente")]
    Pending,

    /// <summary>The Bar/Canteen staff is currently assembling or preparing the items.</summary>
    [Display(Name = "Em Preparação")]
    Preparing,

    /// <summary>Items are ready and awaiting the user at the collection point.</summary>
    [Display(Name = "Pronto para Entrega")]
    ReadyToDeliver,

    /// <summary>The transaction is completed and items have been handed over to the user.</summary>
    [Display(Name = "Entregue")]
    Delivered,

    /// <summary>The order was voided either by the user, an administrator, or a system timeout.</summary>
    [Display(Name = "Cancelado")]
    Cancelled
}

/// <summary>
/// Logic extensions for the <see cref="OrderStatus"/> enum to facilitate business rule checks.
/// </summary>
public static class OrderStatusExtensions
{
    /// <summary>
    /// Collection of statuses representing orders that are "In-Progress" and require staff attention.
    /// </summary>
    private static readonly OrderStatus[] ActiveStatus =
    [
        OrderStatus.Pending,
        OrderStatus.Preparing,
        OrderStatus.ReadyToDeliver
    ];

    /// <summary>
    /// Checks if the current order status is considered "Active" (not yet Delivered or Cancelled).
    /// </summary>
    /// <param name="status">The status to evaluate.</param>
    /// <returns>True if the order is still being processed; otherwise, false.</returns>
    public static bool IsActive(this OrderStatus status) => ActiveStatus.Contains(status);
}
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Ticket;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Interface for the PDF Generation Service.
/// Defines the methods required to generate formatted administrative reports 
/// in PDF format, including branding and professional layout.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generates a PDF document containing the historical record of bar orders.
    /// </summary>
    /// <param name="orders">The list of orders to be included in the report.</param>
    /// <param name="logoPath">The physical server path to the institutional logo image.</param>
    /// <returns>A byte array representing the generated PDF file.</returns>
    byte[] GenerateAdminOrderHistoryPdfAsync(List<Order> orders, string logoPath);

    /// <summary>
    /// Generates a PDF document containing the history of canteen meal tickets.
    /// </summary>
    /// <param name="tickets">The list of tickets (used or active) to be reported.</param>
    /// <param name="logoPath">The physical server path to the institutional logo image.</param>
    /// <returns>A byte array representing the generated PDF file.</returns>
    byte[] GenerateAdminTicketHistoryPdfAsync(List<Ticket> tickets, string logoPath);

    /// <summary>
    /// Generates a PDF document listing registered users based on current filters.
    /// </summary>
    /// <param name="users">The list of user DTOs to be exported.</param>
    /// <param name="logoPath">The physical server path to the institutional logo image.</param>
    /// <returns>A byte array representing the generated PDF file.</returns>
    byte[] GenerateAdminUsersListPdfAsync(List<UserDto> users, string logoPath);

    /// <summary>
    /// Generates a PDF document for staff audit logs (Employee actions).
    /// </summary>
    /// <param name="toList">The list of staff log DTOs to be reported.</param>
    /// <param name="logoPath">The physical server path to the institutional logo image.</param>
    /// <returns>A byte array representing the generated PDF file.</returns>
    byte[] GenerateAdminStaffLogPdfAsync(List<StaffLogDto> toList, string logoPath);
}
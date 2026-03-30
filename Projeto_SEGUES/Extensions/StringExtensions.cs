using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Projeto_SEGUES.Extensions;

/// <summary>
/// Static utility class providing extension methods for string and enum transformations.
/// </summary>
/// <remarks>
/// Focuses on UI-related conversions, such as mapping status strings to CSS classes 
/// and retrieving localized display names from DataAnnotations.
/// </remarks>
public static class StringExtensions
{
    /// <summary>
    /// Maps a string value (Role, Category, or Status) to its corresponding Bootstrap badge CSS class.
    /// </summary>
    /// <param name="value">The status or role string to be styled.</param>
    /// <returns>A string containing one or more CSS classes (e.g., "bg-success").</returns>
    public static string ToBadgeClass(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "bg-secondary";

        return value.Trim() switch
        {
            // Roles
            "Admin" => "bg-danger",
            "Employee" => "bg-warning text-dark",

            // User Category
            "Estudante" => "bg-info text-dark",
            "Trabalhador IPS" => "bg-primary",
            "Externo" => "bg-secondary",

            // User Status
            "Ative" => "bg-success-subtle text-success border-success",
            "Suspended" => "bg-secondary text-dark border-secondary",
            "Inative" => "bg-danger-subtle text-danger border-danger",

            // Ticket State
            "Available" => "bg-color-ips",
            "Used" => "bg-success",
            "Expired" => "bg-danger",

            // Order Status
            "Pending" => "bg-secondary",
            "Preparing" => "bg-info",
            "ReadyToDeliver" => "bg-warning",
            "Delivered" => "bg-success",
            "Cancelled" => "bg-danger",

            // Default
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Retrieves the value of the [Display(Name = "...")] attribute for a given Enum value.
    /// </summary>
    /// <remarks>
    /// This uses Reflection to read metadata at runtime. If no Display attribute is found, 
    /// it returns the standard string representation of the Enum.
    /// </remarks>
    /// <param name="enumValue">The Enum value to inspect.</param>
    /// <returns>The localized or descriptive name of the enum member.</returns>
    public static string ToDisplayName(this Enum enumValue)
    {
        var displayAttribute = enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>();

        return displayAttribute?.Name ?? enumValue.ToString();
    }
}
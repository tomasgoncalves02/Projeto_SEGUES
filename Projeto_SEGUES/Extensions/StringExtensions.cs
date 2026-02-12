using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Projeto_SEGUES.Extensions;

public static class StringExtensions
{
    public static string ToBadgeClass(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return "bg-secondary";

        return value.Trim() switch
        {
            // Roles
            "Admin" => "bg-danger",
            "Employee" => "bg-warning text-dark",
            
            // Database Categories
            "Estudante" => "bg-info text-dark",
            "Trabalhador IPS" => "bg-primary",
            "Externo" => "bg-secondary",
            
            // TicketState
            "Available" => "bg-color-ips",
            "Used" => "bg-secondary",
            "Expired" => "bg-danger",
            
            // Default
            _ => "bg-secondary"
        };
    }
    
    // Read Display(Name) do Enum value
    public static string ToDisplayName(this Enum enumValue)
    {
        var displayAttribute = enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>();

        return displayAttribute?.Name ?? enumValue.ToString();
    }
}
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Json;

namespace Projeto_SEGUES.Extensions;

/// <summary>
/// Static utility class providing extension methods for <see cref="ITempDataDictionary"/>.
/// </summary>
/// <remarks>
/// This class enables complex object storage in TempData via JSON serialization and 
/// provides standardized helpers for triggering SweetAlert2 notifications from controllers.
/// </remarks>
public static class TempDataExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes an object into JSON and stores it in TempData.
    /// </summary>
    /// <typeparam name="T">The type of the object to store.</typeparam>
    /// <param name="tempData">The TempData dictionary.</param>
    /// <param name="key">The key for storage.</param>
    /// <param name="value">The object instance.</param>
    /// <param name="toJs">If true, uses CamelCase naming for JavaScript compatibility.</param>
    public static void SetJson<T>(this ITempDataDictionary tempData, string key, T value, bool toJs = false) where T : class
    {
        tempData[key] = JsonSerializer.Serialize(value, toJs ? JsonOptions : null);
    }

    /// <summary>
    /// Retrieves and deserializes a JSON object from TempData.
    /// </summary>
    public static T? GetJson<T>(this ITempDataDictionary tempData, string key, bool fromJs = false) where T : class
    {
        tempData.TryGetValue(key, out var value);
        if (value is not string json || string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json, fromJs ? JsonOptions : null);
        }
        catch
        {
            return null;
        }
    }

    // Key used by the Layout view to check for pending notifications
    private const string SwalKey = "SwalData";

    /// <summary>
    /// Internal Data Transfer Object representing a SweetAlert2 configuration.
    /// </summary>
    private class SwalDto
    {
        public string? Icon { get; set; }
        public string? Title { get; set; }
        public string? Text { get; set; }
        public string? Html { get; set; }
        public string? Footer { get; set; }
        public int? Timer { get; set; }
        public bool AllowOutsideClick { get; set; } = true;
        public bool AllowEscapeKey { get; set; } = true;
        public bool ShowCloseButton { get; set; } = false;
        public string? Backdrop { get; set; } = "var(--ips-shadow-soft)";

        // Confirm Button Configuration
        public bool ShowConfirmButton = true;
        public string? ConfirmButtonText = "OK";
        public string? ConfirmButtonAriaLabel = "OK";
        public string? ConfirmButtonColor { get; set; } = "var(--ips)";

        // Deny Button Configuration
        public bool ShowDenyButton = false;
        public string? DenyButtonText = "Não";
        public string? DenyButtonAriaLabel = "Não";
        public string? DenyButtonColor { get; set; } = "var(--deny)";

        // Cancel Button Configuration
        public bool ShowCancelButton = false;
        public string? CancelButtonText = "Cancelar";
        public string? CancelButtonAriaLabel = "Cancelar";
        public string? CancelButtonColor { get; set; } = "var(--cancel)";
    }

    /// <summary>
    /// Triggers a success notification that disappears automatically.
    /// </summary>
    public static void SetSwalSuccess(this ITempDataDictionary tempData, string message)
    {
        SetSwal(tempData, new SwalDto
        {
            Icon = "success",
            Title = "Operação Concluída",
            Text = message,
            Timer = 3000,
            ShowConfirmButton = false,
            ShowCloseButton = true
        });
    }

    /// <summary>
    /// Triggers a persistent error notification with support contact information.
    /// </summary>
    public static void SetSwalError(this ITempDataDictionary tempData, string message)
    {
        SetSwal(tempData, new SwalDto
        {
            Icon = "error",
            Title = "Erro",
            Text = message,
            AllowOutsideClick = false,
            AllowEscapeKey = false,
            Footer = "Se o erro persistir, contacte o <a href='mailto:segues2026@gmail.com'>suporte</a>.",
        });
    }

    /// <summary>
    /// Triggers a warning notification that requires user acknowledgment.
    /// </summary>
    public static void SetSwalWarning(this ITempDataDictionary tempData, string message)
    {
        SetSwal(tempData, new SwalDto
        {
            Icon = "warning",
            Title = "Aviso",
            Text = message,
            AllowOutsideClick = false,
            AllowEscapeKey = false
        });
    }

    /// <summary>
    /// Triggers a brief informational notification.
    /// </summary>
    public static void SetSwalInfo(this ITempDataDictionary tempData, string message)
    {
        SetSwal(tempData, new SwalDto
        {
            Icon = "info",
            Title = "Informação",
            Text = message,
            Timer = 4000,
            ShowConfirmButton = false,
            ShowCloseButton = true
        });
    }

    /// <summary>
    /// Triggers a confirmation dialog with Yes/No options.
    /// </summary>
    public static void SetSwalConfirmation(this ITempDataDictionary tempData, string message)
    {
        SetSwal(tempData, new SwalDto
        {
            Icon = "question",
            Title = "Confirma Operação?",
            Text = message,
            ShowCancelButton = true,
            ConfirmButtonText = "Sim",
            ConfirmButtonAriaLabel = "Sim",
            CancelButtonText = "Não",
            CancelButtonAriaLabel = "Não",
            AllowOutsideClick = false,
            AllowEscapeKey = false
        });
    }

    private static void SetSwal(ITempDataDictionary tempData, SwalDto dto)
    {
        tempData.SetJson(SwalKey, dto, true);
    }
}
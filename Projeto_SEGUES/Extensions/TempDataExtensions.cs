using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Projeto_SEGUES.Extensions;

public static class TempDataExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    
    // Json Helpers
    public static void SetJson<T>(this ITempDataDictionary tempData, string key, T value, bool toJs = false) where T : class
    {
        tempData[key] = JsonSerializer.Serialize(value, toJs ? JsonOptions : null);
    }

    public static T? GetJson<T>(this ITempDataDictionary tempData, string key, bool fromJs = false) where T : class
    {
        tempData.TryGetValue(key, out var value);
        if (value is not string json || string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json, fromJs ? JsonOptions : null);
        } catch
        {
            return null;
        }
    }
    
    // Swal Helpers
    private const string SwalKey = "SwalData";

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
        // Confirm Button
        public bool ShowConfirmButton = true;
        public string? ConfirmButtonText = "OK";
        public string? ConfirmButtonAriaLabel = "OK";
        public string? ConfirmButtonColor { get; set; } = "var(--ips)";
        // Deny Button
        public bool ShowDenyButton = false;
        public string? DenyButtonText = "Não";
        public string? DenyButtonAriaLabel = "Não";
        public string? DenyButtonColor { get; set; } = "var(--deny)";
        // Cancel Button
        public bool ShowCancelButton = false;
        public string? CancelButtonText = "Cancelar";
        public string? CancelButtonAriaLabel = "Cancelar";
        public string? CancelButtonColor { get; set; } = "var(--cancel)";
    }

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

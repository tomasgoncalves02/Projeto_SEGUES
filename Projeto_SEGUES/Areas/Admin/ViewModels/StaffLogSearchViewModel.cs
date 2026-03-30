using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Projeto_SEGUES.Models.Enums;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel used for filtering and searching staff logs.
/// </summary>
public class StaffLogSearchViewModel
{
    /// <summary>
    /// Search string used to filter logs by employee name, action, or date.
    /// </summary>
    [Display(Name = "Pesquisar")]
    public string? SearchString { get; set; }
    
    /// <summary>
    /// Filter logs by employee ID.
    /// </summary>
    [Display(Name = "Ação")]
    public UserAction? ActionFilter { get; set; }
    
    /// <summary>
    /// Filter logs by date.
    /// </summary>
    [Display(Name = "Data")]
    [DataType(DataType.Date, ErrorMessage = "Insira uma data válida.")]
    public DateTime? DateFilter { get; set; }
    
    /// <summary>
    /// Collection of staff log entities that match the specified search and filter criteria.
    /// </summary>
    [ValidateNever]
    public List<StaffLogDto> Results { get; set; } = [];
}
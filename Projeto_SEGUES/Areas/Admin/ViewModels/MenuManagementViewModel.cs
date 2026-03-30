using System.ComponentModel.DataAnnotations;

namespace Projeto_SEGUES.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel responsible for carrying and validating menu links within the administrative area.
/// </summary>
/// <remarks>
/// This model ensures that the URLs entered for the canteen and bar meet the formatting 
/// requirements necessary to be correctly displayed to end users.
/// </remarks>
public class MenuManagementViewModel
{
    /// <summary>
    /// Gets or sets the URL address for the canteen menu.
    /// </summary>
    /// <value>Must be a valid URL (e.g., https://domain.com/menu.pdf). Supports null values.</value>
    [Url(ErrorMessage = "Introduza um URL válido (ex: https://...)")]
    [Display(Name = "Link da Ementa do Refeitório")]
    public string? CanteenUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL address for the bar menu.
    /// </summary>
    /// <value>Must be a valid URL (e.g., https://domain.com/bar.pdf). Supports null values.</value>
    [Url(ErrorMessage = "Introduza um URL válido (ex: https://...)")]
    [Display(Name = "Link da Ementa do Bar")]
    public string? BarUrl { get; set; }
}
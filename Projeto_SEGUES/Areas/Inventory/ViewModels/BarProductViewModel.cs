using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Models.Inventory;
using System.Diagnostics.CodeAnalysis;

namespace Projeto_SEGUES.Areas.Inventory.ViewModels
{
    public class BarProductViewModel
    {
        public Product Product { get; set; }

        public IEnumerable<SelectListItem>? Categories { get; set; }

        // O atributo SetsRequiredMembers deve estar no construtor
        [SetsRequiredMembers]
        public BarProductViewModel()
        {
            Product = new Product
            {
                Name = string.Empty,
                Description = string.Empty,
                //ImageUrl = string.Empty,
                Category = null! 
            };
        }
    }
}
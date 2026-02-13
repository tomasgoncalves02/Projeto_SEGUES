using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Projeto_SEGUES.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var viewModel = new BarProductViewModel
            {
                Categories = await _context.ProductCategories
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync()
            };

            ViewBag.Products = await _context.Products.Include(p => p.Category).ToListAsync();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(BarProductViewModel model, int CategoryId)
        {
            var category = await _context.ProductCategories.FindAsync(CategoryId);
            if (category == null) return BadRequest();

            model.Product.Category = category;

            // Remove validação da Category para não dar erro no ModelState (visto ser objeto complexo)
            ModelState.Remove("Product.Category");

            if (ModelState.IsValid)
            {
                _context.Products.Add(model.Product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View("Index", model);
        }
    }
}

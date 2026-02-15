using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BarProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                _context.Add(model.Product);
                await _context.SaveChangesAsync();

                // Esta linha cria o JSON que o teu site.js lê no DOMContentLoaded
                TempData.SetSwalSuccess("O produto '" + model.Product.Name + "' foi registado com sucesso!");

                return RedirectToAction(nameof(Index));
            }

            TempData.SetSwalError("Não foi possível registar o produto. Verifique os campos.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData.SetSwalSuccess("Produto eliminado com sucesso!");
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Carrega a página com os dados
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var viewModel = new BarProductViewModel { Product = product };
            return View(viewModel);
        }

        // POST: Guarda as alterações (Aqui é onde dava o erro 405)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BarProductViewModel model)
        {
            if (id != model.Product.Id) return NotFound();

            // Removemos validações de campos que não estão no form para evitar o erro de validação anterior
            ModelState.Remove("Product.ImageUrl");
            ModelState.Remove("Product.Category");

            if (ModelState.IsValid)
            {
                _context.Update(model.Product);
                await _context.SaveChangesAsync();

                TempData.SetSwalSuccess("Produto atualizado com sucesso!");
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}

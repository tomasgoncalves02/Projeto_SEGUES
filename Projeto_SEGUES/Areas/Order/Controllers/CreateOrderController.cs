using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Order.ViewModels;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace Projeto_SEGUES.Areas.Order;

/// <summary>
/// Controller responsible for creating new orders and managing the shopping cart lifecycle.
/// </summary>
/// <remarks>
/// This controller coordinates the interaction between product inventory and the order service, 
/// allowing item addition/removal and the checkout process with balance and schedule validations.
/// It implements a hybrid approach using traditional MVC actions and AJAX/HTMX for dynamic UI updates.
/// </remarks>
[Area("Order")]
[Authorize]
public class CreateOrderController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IAdminService _adminService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOrderController"/> class.
    /// </summary>
    /// <param name="inventoryService">Service for product and stock management.</param>
    /// <param name="orderService">Service for order processing and cart persistence.</param>
    /// <param name="userManager">ASP.NET Core Identity manager for user data.</param>
    /// <param name="adminService">Service for global system configurations and schedules.</param>
    public CreateOrderController(
        IInventoryService inventoryService,
        IOrderService orderService,
        UserManager<AppUser> userManager,
        IAdminService adminService)
    {
        _inventoryService = inventoryService;
        _orderService = orderService;
        _userManager = userManager;
        _adminService = adminService;
    }

    /// <summary>
    /// Displays the product selection page (Storefront) for a new order.
    /// </summary>
    /// <remarks>
    /// Validates if the service window is open before allowing access. 
    /// Populates the ViewModel with categories, current cart totals, and available products.
    /// </remarks>
    /// <returns>A View with the storefront interface or a redirect if the bar is closed.</returns>
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        // Validate business hours
        bool isOpen = await _adminService.IsBarOpenAsync(DateTime.Now.TimeOfDay);
        if (!isOpen)
        {
            TempData.SetSwalInfo("O bar está fechado. Por favor, volte mais tarde.");
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        var cart = await _orderService.GetCartAsync(userId);
        bool isStaff = User.IsInRole("Admin") || User.IsInRole("Employee");

        var rawProducts = await _inventoryService.GetAvailableProductsAsync();

        CreateOrderViewModel vm = new CreateOrderViewModel
        {
            Categories = await _inventoryService.GetAllCategoriesForDropdownAsync(),
            CartTotal = cart != null
                ? _orderService.GetOrderTotal(cart)
                : new OrderTotalViewModel { TotalQuantity = 0, TotalValue = 0m },
            SearchModel = new OrderProductSearchViewModel
            {
                Results = rawProducts.Select(p => new OrderProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    CategoryId = p.Category.Id,
                    CategoryName = p.Category.Name,
                    ModalInfo = new
                    {
                        name = p.Name,
                        description = p.Description,
                        price = p.Price.ToString("C"),
                        categoryName = p.Category.Name,
                        categoryDescription = p.Category.Description,
                        stock = isStaff ? (int?)p.Stock : null,
                        minStock = isStaff ? (int?)p.MinimumStock : null
                    }
                }).ToList()
            }
        };

        return View(vm);
    }

    /// <summary>
    /// Retrieves a filtered list of products based on search criteria.
    /// </summary>
    /// <param name="searchModel">The search and filter parameters (Category, Search String).</param>
    /// <remarks>This method is typically called via AJAX to update the product grid partially.</remarks>
    /// <returns>A Partial View containing the filtered product cards.</returns>
    [HttpGet]
    public async Task<IActionResult> GetFilteredProducts([Bind(Prefix = "SearchModel")] OrderProductSearchViewModel searchModel)
    {
        bool isStaff = User.IsInRole("Admin") || User.IsInRole("Employee");
        var products = await _inventoryService.GetFilteredProductsAsync(
            new InventorySearchViewModel
            {
                CategoryId = searchModel.CategoryId,
                SearchString = searchModel.SearchString,
                StockLevel = StockLevel.InStock,
                ActiveOnly = true
            });

        searchModel.Results = products.Select(p => new OrderProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            CategoryId = p.Category.Id,
            CategoryName = p.Category.Name,
            ModalInfo = new
            {
                name = p.Name,
                description = p.Description,
                price = p.Price.ToString("C"),
                categoryName = p.Category.Name,
                categoryDescription = p.Category.Description,
                stock = isStaff ? (int?)p.Stock : null,
                minStock = isStaff ? (int?)p.MinimumStock : null
            }
        }).ToList();

        return PartialView("_ProductListPartial", searchModel.Results);
    }

    /// <summary>
    /// Adds a specific product and quantity to the user's active cart.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="qty">The quantity to be added.</param>
    /// <remarks>If the user is not authenticated, triggers a redirect header for HTMX/AJAX compatibility.</remarks>
    /// <returns>A JSON object with the success status and updated cart totals (Count and Value).</returns>
    [HttpPost]
    public async Task<IActionResult> AddToCart(int id, int qty)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Challenge();
        }

        var result = await _orderService.AddToCartAsync(userId, id, qty);
        if (!result.Success) return NotFound(new { failMessage = result.Message });

        OrderTotalViewModel orderTotal = result.Data!;
        return Ok(new { successMessage = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
    }

    /// <summary>
    /// Completely removes a product line from the user's active cart.
    /// </summary>
    /// <param name="id">The unique identifier of the product to be removed.</param>
    /// <returns>A JSON object with the updated cart totals after removal.</returns>
    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            Response.Headers["HX-Redirect"] = Url.Page("/Account/Login", new { area = "Identity" });
            return Unauthorized();
        }

        var result = await _orderService.RemoveFromCartAsync(userId, id);
        if (!result.Success) return NotFound(new { failMessage = result.Message });

        OrderTotalViewModel orderTotal = result.Data!;
        return Ok(new { successMessage = result.Message, count = orderTotal.TotalQuantity, value = orderTotal.TotalValue });
    }

    /// <summary>
    /// Displays the final checkout summary before payment.
    /// </summary>
    /// <remarks>
    /// Re-validates the business hours and calculates the user's purchasing power (Balance vs. Total).
    /// </remarks>
    /// <returns>The Checkout view with the full cart summary.</returns>
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        bool isOpen = await _adminService.IsBarOpenAsync(DateTime.Now.TimeOfDay);
        if (!isOpen)
        {
            TempData.SetSwalInfo("O bar está fechado. Por favor, volte mais tarde.");
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        ViewBag.Balance = user.Balance;
        var cart = await _orderService.GetCartAsync(user.Id);

        if (cart == null) return RedirectToAction(nameof(Index));

        ViewBag.TotalQuantity = _orderService.GetOrderTotal(cart).TotalQuantity;
        return View(cart);
    }

    /// <summary>
    /// Finalizes the order, deducting balance and moving the state from 'Cart' to 'Submitted'.
    /// </summary>
    /// <param name="receiveNow">If true, the order is marked for immediate consumption.</param>
    /// <param name="pickupTime">A scheduled time for the user to collect the order.</param>
    /// <remarks>This is the primary transactional endpoint of the ordering module.</remarks>
    /// <returns>A redirect to Active Orders on success, or back to Checkout with an error message.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOrder(bool receiveNow, string? pickupTime)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _orderService.SubmitOrderAsync(user, receiveNow, pickupTime);

        if (result.Success)
        {
            TempData.SetSwalSuccess(result.Message);
            return RedirectToAction("Index", "ActiveOrder", new { area = "Order" });
        }

        TempData.SetSwalError(result.Message);
        return RedirectToAction(nameof(Checkout));
    }
}
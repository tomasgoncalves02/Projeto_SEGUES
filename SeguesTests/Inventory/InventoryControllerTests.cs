using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Inventory.Controllers;
using Xunit;

namespace SeguesTests.Inventory
{
    public class InventoryControllerTests
    {
        private readonly InventoryController _controller;

        public InventoryControllerTests()
        {
            _controller = new InventoryController();
        }

        // Confirms that the Index action successfully returns the default inventory view
        [Fact]
        public async Task Index_ReturnsView()
        {
            var result = _controller.Index();

            Assert.IsType<ViewResult>(result);
        }
    }
}
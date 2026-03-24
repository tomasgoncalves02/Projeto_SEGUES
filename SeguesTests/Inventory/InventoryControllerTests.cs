using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Inventory.Controllers;

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
        public void Index_ReturnsView()
        {
            var result = _controller.Index();

            Assert.IsType<ViewResult>(result);
        }
    }
}
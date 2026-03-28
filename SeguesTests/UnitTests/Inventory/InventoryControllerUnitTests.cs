using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Inventory.Controllers;
using Xunit;

namespace SeguesTests.UnitTests.Inventory
{
    public class InventoryControllerUnitTests
    {
        private readonly InventoryController _controller;

        public InventoryControllerUnitTests()
        {
            _controller = new InventoryController();
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            var result = _controller.Index();

            Assert.IsType<ViewResult>(result);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Statistics.Controllers;

namespace SeguesTests.Statistics
{
    public class StatisticsControllerTests
    {
        private readonly StatisticsController _controller;

        public StatisticsControllerTests()
        {
            _controller = new StatisticsController();
        }

        // Confirms that the main statistics navigation page is correctly returned
        [Fact]
        public void Index_ReturnsView()
        {
            var result = _controller.Index();

            Assert.IsType<ViewResult>(result);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Statistics.Controllers;
using Xunit;

namespace SeguesTests.Statistics
{
    public class StatisticsPageControllerTests
    {
        private readonly StatisticsPageController _controller;

        public StatisticsPageControllerTests()
        {
            _controller = new StatisticsPageController();
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
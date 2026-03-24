using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Report;

namespace SeguesTests.Statistics
{
    public class ReportStatisticsControllerTests
    {
        private readonly ReportStatisticsController _controller;

        public ReportStatisticsControllerTests()
        {
            _controller = new ReportStatisticsController();
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
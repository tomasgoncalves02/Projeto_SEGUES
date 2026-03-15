using Microsoft.AspNetCore.Mvc;
using Projeto_SEGUES.Areas.Report;
using Xunit;

namespace SeguesTests.Report
{
    public class ReportControllerTests
    {
        private readonly ReportController _controller;

        public ReportControllerTests()
        {
            _controller = new ReportController();
        }

        // Confirms that the main report navigation page is correctly returned to the user
        [Fact]
        public void Index_ReturnsView()
        {
            var result = _controller.Index();

            Assert.IsType<ViewResult>(result);
        }
    }
}
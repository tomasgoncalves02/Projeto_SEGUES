using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Report.ViewModels;
using Projeto_SEGUES.Models.Order; // ✅ Importante para reconhecer 'Order'
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.UnitTests.Admin
{
    public class AdminOrderManagementUnitTests
    {
        private readonly Mock<IAdminService> _adminServiceMock;
        private readonly Mock<IReportService> _reportServiceMock;
        private readonly Mock<IPdfService> _pdfServiceMock;
        private readonly AdminOrderManagementController _controller;

        public AdminOrderManagementUnitTests()
        {
            _adminServiceMock = new Mock<IAdminService>();
            _reportServiceMock = new Mock<IReportService>();
            _pdfServiceMock = new Mock<IPdfService>();

            _controller = new AdminOrderManagementController(
                _adminServiceMock.Object,
                _reportServiceMock.Object,
                _pdfServiceMock.Object);

            _controller.TempData = new Mock<ITempDataDictionary>().Object;
        }

        [Fact]
        public async Task Index_ReturnsView_WithOrdersFromService()
        {
            var config = new BarCanteenConfigViewModel
            {
                BarOpeningTimeString = "08:00",
                BarClosingTimeString = "20:00"
            };

            var orders = new List<Order> { new Order { Id = 1, OrderDate = DateTime.Now, AppUser = new AppUser
        {
            Id = "user-1",
            FirstName = "Utilizador",
            LastName = "Teste",
            Email = "teste@segues.pt",
            UserName = "teste@segues.pt",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
            UserCategory = new UserCategory { Name = "Externo" }
        } } };

            _adminServiceMock.Setup(s => s.GetScheduleAsync()).ReturnsAsync(config);
            _reportServiceMock.Setup(s => s.GetAdminOrderHistoryAsync(It.IsAny<ReportOrderSearchViewModel>(), false))
                .ReturnsAsync(orders);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminOrderManagementViewModel>(viewResult.Model);
            Assert.Equal(orders, model.SearchModel.Results);
            Assert.True(_controller.ViewBag.ShowUser);
        }

        [Fact]
        public async Task UpdateOpenAndCloseTime_RedirectsToIndex_OnServiceResult()
        {
            var open = new TimeSpan(8, 0, 0);
            var close = new TimeSpan(18, 0, 0);
            _adminServiceMock.Setup(s => s.UpdateScheduleAsync(It.IsAny<BarCanteenConfigViewModel>()))
                .ReturnsAsync(ServiceResult.Ok("Sucesso"));

            var result = await _controller.UpdateOpenAndCloseTime(open, close);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task ExportOrdersPdf_ReturnsFileResult_WithCorrectMimeType()
        {
            var orders = new List<Order>();

            _reportServiceMock.Setup(s => s.GetAdminOrderHistoryAsync(It.IsAny<ReportOrderSearchViewModel>(), true))
                .ReturnsAsync(orders);

            _pdfServiceMock.Setup(s => s.GenerateAdminOrderHistoryPdfAsync(It.IsAny<List<Order>>(), It.IsAny<string>()))
                .Returns(new byte[] { 1, 2, 3 });

            var result = await _controller.ExportOrdersPdf(new ReportOrderSearchViewModel());

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Contains("Historico_Pedidos", fileResult.FileDownloadName);
        }
    }
}
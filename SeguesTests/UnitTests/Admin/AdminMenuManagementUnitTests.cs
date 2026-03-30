using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.UnitTests.Admin
{
    public class AdminMenuManagementUnitTests
    {
        [Fact]
        public async Task Index_ReturnsViewWithMappedLinks()
        {
            var adminMock = new Mock<IAdminService>();
            var loggerMock = new Mock<ILogger<AdminMenuManagementController>>();

            var configFromService = new BarCanteenConfigViewModel
            {
                CanteenMenuLink = "https://pedro.ementa.pt/almoco",
                BarMenuLink = "https://pedro.ementa.pt/bar"
            };

            adminMock.Setup(s => s.GetMenuLinksAsync()).ReturnsAsync(configFromService);
            var controller = new AdminMenuManagementController(adminMock.Object, loggerMock.Object);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MenuManagementViewModel>(viewResult.Model);

            Assert.Equal(configFromService.CanteenMenuLink, model.CanteenUrl);
            Assert.Equal(configFromService.BarMenuLink, model.BarUrl);
        }

        [Fact]
        public async Task SaveLinks_ValidModel_RedirectsToIndex()
        {
            var adminMock = new Mock<IAdminService>();
            var loggerMock = new Mock<ILogger<AdminMenuManagementController>>();
            var tempData = new Mock<ITempDataDictionary>();

            var controller = new AdminMenuManagementController(adminMock.Object, loggerMock.Object)
            {
                TempData = tempData.Object
            };

            var model = new MenuManagementViewModel
            {
                CanteenUrl = "http://pedro-novo.link",
                BarUrl = "http://pedro-novo.bar"
            };

            var result = await controller.SaveLinks(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            adminMock.Verify(s => s.UpdateMenuLinksAsync(model.CanteenUrl, model.BarUrl), Times.Once);
        }
    }
}
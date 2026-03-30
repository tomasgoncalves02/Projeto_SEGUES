using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Services
{
    public class AdminServiceUnitTests
    {
        private AppDbContext GetContext() =>
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [Fact]
        public async Task UpdateScheduleAsync_ClosingBeforeOpening_ReturnsFailure()
        {
            var context = GetContext();
            context.AppConfig.Add(new AppConfig { Id = 1 });
            await context.SaveChangesAsync();

            var service = new AdminService(context, null!, null!, null!, null!, null!, null!);

            var model = new BarCanteenConfigViewModel
            {
                BarOpeningTime = new TimeSpan(18, 0, 0),
                BarClosingTime = new TimeSpan(08, 0, 0)
            };

            var result = await service.UpdateScheduleAsync(model);

            Assert.False(result.Success);
            Assert.Equal("A hora de fecho não pode ser anterior à hora de abertura.", result.Message);
        }

        [Fact]
        public async Task IsBarOpenAsync_ConditionalValidation_BasedOnCurrentDay()
        {
            var context = GetContext();
            var config = new AppConfig
            {
                BarOpeningTime = new TimeSpan(8, 0, 0),
                BarClosingTime = new TimeSpan(20, 0, 0),
                IsOpenSaturday = false, 
                IsOpenSunday = false    
            };
            context.AppConfig.Add(config);
            await context.SaveChangesAsync();

            var service = new AdminService(context, null!, null!, null!, null!, null!, null!);
            var horaTeste = new TimeSpan(10, 0, 0); 

            var result = await service.IsBarOpenAsync(horaTeste);
            var hoje = DateTime.Now.DayOfWeek;

            
            if (hoje == DayOfWeek.Saturday || hoje == DayOfWeek.Sunday)
            {
                Assert.False(result, $"Hoje é {hoje}, o bar deveria estar FECHADO.");
            }
            else
            {
                Assert.True(result, $"Hoje é {hoje}, o bar deveria estar ABERTO às {horaTeste}.");
            }
        }
    }
}
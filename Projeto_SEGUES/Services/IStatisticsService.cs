namespace Projeto_SEGUES.Services;

public interface IStatisticsService
{
    Task<object> GetTicketsStats(int period = 1);
    Task<object> GetBarStats(int period = 1);


}


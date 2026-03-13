namespace Projeto_SEGUES.Services;

public interface IStatisticsService
{
    Task<object> GetTicketsStats(string period = "Dia");


}


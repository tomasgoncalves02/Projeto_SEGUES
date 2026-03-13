namespace Projeto_SEGUES.Services;

public interface IStatisticsService
{
    Task<object> GetTicketsStatsAsync(string period = "Dia");
}


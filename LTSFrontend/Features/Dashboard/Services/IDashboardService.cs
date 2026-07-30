using LTSFrontend.Features.Dashboard.Models;

namespace LTSFrontend.Features.Dashboard.Services
{
    public interface IDashboardService
    {
        Task<DashboardDTO?> GetStatsAsync();
    }
}

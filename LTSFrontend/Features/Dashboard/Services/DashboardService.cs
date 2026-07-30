using LTSFrontend.Core.Http;
using LTSFrontend.Features.Dashboard.Models;

namespace LTSFrontend.Features.Dashboard.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApiClient _api;

        public DashboardService(ApiClient api)
        {
            _api = api;
        }

        public Task<DashboardDTO?> GetStatsAsync() =>
            _api.GetAsync<DashboardDTO>(ApiEndpoints.Dashboard.Stats);
    }
}

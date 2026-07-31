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

        // Same endpoint for everyone - the backend (DashboardController)
        // decides which DTO shape to return based on the caller's own JWT
        // role claim. The two methods here just deserialize into the
        // shape appropriate for whichever role the page already knows
        // it's rendering for.
        public Task<SuperAdminDashboardDTO?> GetSuperAdminStatsAsync() =>
            _api.GetAsync<SuperAdminDashboardDTO>(ApiEndpoints.Dashboard.Stats);

        public Task<FirmDashboardDTO?> GetFirmStatsAsync() =>
            _api.GetAsync<FirmDashboardDTO>(ApiEndpoints.Dashboard.Stats);
    }
}

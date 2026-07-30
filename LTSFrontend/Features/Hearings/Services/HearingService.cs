using LTSFrontend.Core.Http;
using LTSFrontend.Features.Hearings.Models;

namespace LTSFrontend.Features.Hearings.Services
{
    public class HearingService : IHearingService
    {
        private readonly ApiClient _api;

        public HearingService(ApiClient api)
        {
            _api = api;
        }

        public async Task<PagedHearingResult<HearingDTO>> GetByCaseAsync(long caseId, int pageNumber = 1, int pageSize = 10)
        {
            var url = $"{ApiEndpoints.Hearings.ByCase(caseId)}?pageNumber={pageNumber}&pageSize={pageSize}";
            var result = await _api.GetAsync<PagedHearingResult<HearingDTO>>(url);
            return result ?? new PagedHearingResult<HearingDTO> { PageNumber = pageNumber, PageSize = pageSize };
        }

        public async Task<PagedHearingResult<HearingDTO>> GetUpcomingAsync(int pageNumber = 1, int pageSize = 10, long? caseId = null, int? courtId = null)
        {
            var query = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
            if (caseId.HasValue) query.Add($"caseId={caseId.Value}");
            if (courtId.HasValue) query.Add($"courtId={courtId.Value}");

            var url = ApiEndpoints.Hearings.Upcoming + "?" + string.Join("&", query);
            var result = await _api.GetAsync<PagedHearingResult<HearingDTO>>(url);
            return result ?? new PagedHearingResult<HearingDTO> { PageNumber = pageNumber, PageSize = pageSize };
        }

        public Task<HearingDTO?> GetByIdAsync(long id) =>
            _api.GetAsync<HearingDTO>(ApiEndpoints.Hearings.ById(id));

        public Task<long> CreateAsync(HearingFormDTO form) =>
            _api.PostAsync<long>(ApiEndpoints.Hearings.Base_, new
            {
                form.CaseId,
                CourtId = form.CourtId!.Value,
                HearingDate = form.HearingDate!.Value,
                CourtRoom = Norm(form.CourtRoom),
                JudgeName = Norm(form.JudgeName),
                HearingPurpose = Norm(form.HearingPurpose),
                Remarks = Norm(form.Remarks)
            });

        public Task<bool> UpdateAsync(HearingFormDTO form) =>
            _api.PutAsync<bool>(ApiEndpoints.Hearings.ById(form.HearingId), new
            {
                form.HearingId,
                HearingDate = form.HearingDate!.Value,
                CourtRoom = Norm(form.CourtRoom),
                JudgeName = Norm(form.JudgeName),
                HearingPurpose = Norm(form.HearingPurpose),
                HearingOutcome = Norm(form.HearingOutcome),
                form.NextHearingDate,
                Remarks = Norm(form.Remarks)
            });

        public Task<bool> DeleteAsync(long id) =>
            _api.DeleteAsync<bool>(ApiEndpoints.Hearings.ById(id));

        private static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}

using LTSFrontend.Core.Models;
using LTSFrontend.Features.Cases.Models;

namespace LTSFrontend.Features.Cases.Services
{
    public interface ICaseService
    {
        Task<PagedResult<CaseDTO>> GetAllAsync(
            string? searchText = null,
            int? courtID = null,
            int? statusID = null,
            string? priority = null,
            int pageNumber = 1,
            int pageSize = 10);

        Task<CaseDTO?> GetByIdAsync(long id);
        Task<long> CreateAsync(CreateCaseDTO form);
        Task<bool> UpdateAsync(UpdateCaseDTO form);
        Task<bool> DeleteAsync(long id);
        Task<bool> UpdateStatusAsync(long id, int newStatusID, string? remarks);
    }
}

using LTSFrontend.Features.Masters.DTOs;

namespace LTSFrontend.Features.Masters.Services
{
    public interface ICaseStatusService
    {
        Task<List<CaseStatusDTO>> GetAllAsync(string? searchText = null, bool activeOnly = false);
        Task<CaseStatusDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CaseStatusFormDTO form);
        Task<bool> UpdateAsync(CaseStatusFormDTO form);
        Task<bool> DeleteAsync(int id);
    }
}

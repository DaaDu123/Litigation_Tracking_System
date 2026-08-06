using LTSFrontend.Features.Masters.DTOs;

namespace LTSFrontend.Features.Masters.Services
{
    public interface ICaseStageService
    {
        Task<List<CaseStageDTO>> GetAllAsync(string? searchText = null, bool activeOnly = false);
        Task<CaseStageDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CaseStageFormDTO form);
        Task<bool> UpdateAsync(CaseStageFormDTO form);
        Task<bool> DeleteAsync(int id);
    }
}

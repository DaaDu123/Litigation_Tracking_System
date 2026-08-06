using LTSFrontend.Features.Masters.DTOs;

namespace LTSFrontend.Features.Masters.Services
{
    public interface ICourtService
    {
        Task<List<CourtDTO>> GetAllAsync(string? searchText = null, bool activeOnly = false);
        Task<CourtDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CourtFormDTO form);
        Task<bool> UpdateAsync(CourtFormDTO form);
        Task<bool> DeleteAsync(int id);
    }
}

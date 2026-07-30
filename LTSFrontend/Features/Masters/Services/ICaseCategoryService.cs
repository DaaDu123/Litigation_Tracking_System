using LTSFrontend.Features.Masters.Models;

namespace LTSFrontend.Features.Masters.Services
{
    public interface ICaseCategoryService
    {
        Task<List<CaseCategoryDTO>> GetAllAsync(string? searchText = null, bool activeOnly = false);
        Task<CaseCategoryDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CaseCategoryFormDTO form);
        Task<bool> UpdateAsync(CaseCategoryFormDTO form);
        Task<bool> DeleteAsync(int id);
    }
}

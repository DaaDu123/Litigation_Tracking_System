using LTSFrontend.Features.Masters.Models;

namespace LTSFrontend.Features.Masters.Services
{
    public interface IDocumentTypeService
    {
        Task<List<DocumentTypeDTO>> GetAllAsync(string? searchText = null, bool activeOnly = false);
        Task<DocumentTypeDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(DocumentTypeFormDTO form);
        Task<bool> UpdateAsync(DocumentTypeFormDTO form);
        Task<bool> DeleteAsync(int id);
    }
}

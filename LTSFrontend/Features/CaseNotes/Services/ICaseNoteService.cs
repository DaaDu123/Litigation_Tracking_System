using LTSFrontend.Features.CaseNotes.Models;

namespace LTSFrontend.Features.CaseNotes.Services
{
    public interface ICaseNoteService
    {
        Task<List<CaseNoteDTO>> GetByCaseAsync(long caseId);
        Task<long> CreateAsync(CaseNoteFormDTO dto);
        Task<bool> UpdateAsync(CaseNoteFormDTO dto);
        Task<bool> DeleteAsync(long noteId);
    }
}

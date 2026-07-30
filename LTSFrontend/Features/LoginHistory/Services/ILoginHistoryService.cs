using LTSFrontend.Core.Models;
using LTSFrontend.Features.LoginHistory.Models;

namespace LTSFrontend.Features.LoginHistory.Services
{
    public interface ILoginHistoryService
    {
        Task<PagedResult<LoginHistoryDTO>> GetAllAsync(LoginHistoryFilterDTO filter);
        Task<List<MyLoginHistoryDTO>> GetMyHistoryAsync();
        Task<bool> DeleteAsync(int id);
        Task<int> CleanupAsync(int days = 90);
    }
}

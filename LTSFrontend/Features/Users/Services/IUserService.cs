using LTSFrontend.Features.Users.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace LTSFrontend.Features.Users.Services
{
    public interface IUserService
    {
        Task<List<UserDTO>> GetAllAsync();
        Task<UserDTO?> GetByIdAsync(int id);
        Task<UserDTO?> GetMyProfileAsync();
        Task<int> CreateAsync(CreateUserDTO dto, IBrowserFile? profileImage = null);
        Task<bool> UpdateAsync(CreateUserDTO dto, IBrowserFile? profileImage = null);
        Task<bool> DeleteAsync(int id);
    }
}

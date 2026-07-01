using Microsoft.AspNetCore.Http;
namespace LTSBackend.Services.ProfileService
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        void DeleteFile(string? relativePath);
    }
}
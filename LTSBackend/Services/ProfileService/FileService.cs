using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace LTSBackend.Services.ProfileService
{
    public class FileService(IWebHostEnvironment environment) : IFileService
    {
        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            string rootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            string uploadsFolder = Path.Combine(rootPath, "uploads", folderName);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string ext = Path.GetExtension(file.FileName);
            string uniqueFileName = $"{Guid.NewGuid()}{ext}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public void DeleteFile(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            string rootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            string fullPath = Path.Combine(rootPath, relativePath.TrimStart('/'));

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch (IOException)
                {
                    // Non-fatal — cleanup of the old file is best-effort.
                }
            }
        }
    }
}


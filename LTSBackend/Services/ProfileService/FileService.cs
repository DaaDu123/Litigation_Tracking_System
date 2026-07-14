namespace LTSBackend.Services.ProfileService;
public class FileService(IWebHostEnvironment _environment, ILogger<FileService> _logger) : IFileService
{
    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Attempted to save null or empty file");
            return string.Empty;
        }

        string rootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        string uploadsFolder = Path.Combine(rootPath, "uploads", folderName);

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
            _logger.LogInformation("Created uploads folder: {Folder}", uploadsFolder);
        }

        string ext = Path.GetExtension(file.FileName);
        string uniqueFileName = $"{Guid.NewGuid()}{ext}";
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation("File saved successfully: {FileName}", uniqueFileName);
            return $"/uploads/{folderName}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {FileName}", file.FileName);
            throw;
        }
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return;
        }

        try
        {
            string rootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            string fullPath = Path.Combine(rootPath, relativePath.TrimStart('/'));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted successfully: {FilePath}", relativePath);
            }
        }
        catch (IOException ex)
        {
            // Log but don't throw — file cleanup is best-effort
            _logger.LogWarning(ex, "Failed to delete file: {FilePath}", relativePath);
        }
    }
}
namespace LTSBackend.Services.ProfileService;

public class FileService(IWebHostEnvironment _environment, ILogger<FileService> _logger) : IFileService
{
    // ============================================================
    // SECURITY (SRS "File Upload Security"): extensions that must never be
    // accepted for ANY upload, public or secure - executable/script types
    // that could enable remote code execution or stored-XSS if ever served,
    // downloaded and run, or opened by a browser that sniffs content type
    // rather than trusting a spoofed extension. This is deliberately
    // enforced here, at the storage layer, as defense-in-depth: the
    // per-command FluentValidation validators (e.g.
    // UploadDocumentValidator) are the primary allow-list gate for case
    // documents, but this blocklist also protects call sites that have NO
    // validator of their own today (e.g. profile picture upload via
    // CreateUserCommand/UpdateUserCommand), so a future/forgotten caller
    // can't accidentally accept a dangerous file type.
    // ============================================================
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".msi", ".bat", ".cmd", ".sh", ".ps1", ".psm1",
        ".php", ".php3", ".php4", ".php5", ".phtml",
        ".js", ".mjs", ".vbs", ".jar", ".jse", ".wsf", ".wsh",
        ".html", ".htm", ".svg", ".swf", ".scr", ".com", ".cpl", ".apk"
    };

    public Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        string publicRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return SaveFileInternalAsync(file, folderName, publicRoot, isPublic: true);
    }

    public void DeleteFile(string? relativePath)
    {
        string publicRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        DeleteFileInternal(relativePath, publicRoot, isPublic: true);
    }

    // ================================================================
    // SECURITY FIX (CRITICAL - SRS "Document Security" / "Restricted
    // Moharrirs must never receive document content through any
    // endpoint"): case documents were previously saved via SaveFileAsync
    // into wwwroot/uploads/case_documents, which app.UseStaticFiles()
    // (Program.cs) serves to ANYONE with no authentication at all -
    // completely bypassing tenant isolation, RBAC, and the Moharrir
    // blind-upload restriction the rest of this codebase carefully
    // enforces. Anyone who ever saw, logged, or leaked the GUID filename
    // (browser history, proxy/access logs, a backup, a Referer header)
    // could download the file directly forever, with zero authorization
    // check. SaveSecureFileAsync stores under
    // {ContentRootPath}/SecureStorage/{folderName} instead - a directory
    // app.UseStaticFiles() can never reach regardless of middleware
    // ordering or future configuration changes - so the ONLY way to read
    // the bytes back is via ReadSecureFileAsync, which every caller in
    // this codebase gates behind an explicit permission check
    // (IDocumentPermissionService.CanUserAccessDocumentAsync) first.
    // ================================================================
    public Task<string> SaveSecureFileAsync(IFormFile file, string folderName)
    {
        string secureRoot = Path.Combine(_environment.ContentRootPath, "SecureStorage");
        return SaveFileInternalAsync(file, folderName, secureRoot, isPublic: false);
    }

    public async Task<byte[]> ReadSecureFileAsync(string relativePath)
    {
        string fullPath = ResolveSecurePath(relativePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogError("Secure file not found on disk: {FullPath}", fullPath);
            throw new FileNotFoundException("File not found on server.", relativePath);
        }

        return await File.ReadAllBytesAsync(fullPath);
    }

    public bool SecureFileExists(string relativePath)
    {
        try
        {
            return File.Exists(ResolveSecurePath(relativePath));
        }
        catch
        {
            return false;
        }
    }

    public void DeleteSecureFile(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return;
        }

        try
        {
            string fullPath = ResolveSecurePath(relativePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Secure file deleted successfully: {FilePath}", relativePath);
            }
        }
        catch (IOException ex)
        {
            // Log but don't throw — file cleanup is best-effort
            _logger.LogWarning(ex, "Failed to delete secure file: {FilePath}", relativePath);
        }
    }

    // ================================================================
    // Shared save logic for both the public (wwwroot) and secure
    // (SecureStorage) stores. The random GUID filename (not the original
    // client filename) is what actually prevents path traversal on save -
    // Path.GetExtension only ever returns the suffix after the last '.',
    // so even a malicious original name like "../../evil.jpg" safely
    // yields just ".jpg" here and can never break out of uploadsFolder.
    // ================================================================
    private async Task<string> SaveFileInternalAsync(IFormFile file, string folderName, string root, bool isPublic)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Attempted to save null or empty file");
            return string.Empty;
        }

        string ext = Path.GetExtension(file.FileName);

        if (BlockedExtensions.Contains(ext))
        {
            _logger.LogWarning("Rejected upload with disallowed extension: {Extension} (original name: {FileName})", ext, file.FileName);
            throw new InvalidOperationException($"File type '{ext}' is not permitted for upload.");
        }

        string uploadsFolder = isPublic
            ? Path.Combine(root, "uploads", folderName)
            : Path.Combine(root, folderName);

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
            _logger.LogInformation("Created uploads folder: {Folder}", uploadsFolder);
        }

        string uniqueFileName = $"{Guid.NewGuid()}{ext}";
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation("File saved successfully: {FileName} (secure={IsSecure})", uniqueFileName, !isPublic);

            // Public files are returned as a web-relative URL path
            // ("/uploads/{folder}/{name}"); secure files are returned as a
            // storage-relative path ("{folder}/{name}") with no leading
            // slash, since it is never meant to be used as a URL.
            return isPublic ? $"/uploads/{folderName}/{uniqueFileName}" : $"{folderName}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {FileName}", file.FileName);
            throw;
        }
    }

    private void DeleteFileInternal(string? relativePath, string root, bool isPublic)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return;
        }

        try
        {
            string fullPath = Path.Combine(root, relativePath.TrimStart('/'));

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

    // Resolves a stored secure-relative path (e.g. "case_documents/{guid}.pdf")
    // to an absolute path under SecureStorage, and defensively rejects
    // anything that would resolve outside that root (defense-in-depth: the
    // value normally only ever comes from our own SaveSecureFileAsync via
    // Document.FilePath in the database, never directly from user input, but
    // this guard costs nothing and closes off any future/indirect path-
    // traversal vector).
    private string ResolveSecurePath(string relativePath)
    {
        string secureRoot = Path.Combine(_environment.ContentRootPath, "SecureStorage");
        string fullPath = Path.GetFullPath(Path.Combine(secureRoot, relativePath.TrimStart('/', '\\')));
        string normalizedRoot = Path.GetFullPath(secureRoot);

        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Rejected secure file path outside SecureStorage root: {RelativePath}", relativePath);
            throw new UnauthorizedAccessException("Invalid file path.");
        }

        return fullPath;
    }
}
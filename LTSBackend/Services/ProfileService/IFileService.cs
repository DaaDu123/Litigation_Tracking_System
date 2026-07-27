using Microsoft.AspNetCore.Http;
namespace LTSBackend.Services.ProfileService
{
    public interface IFileService
    {
        /// <summary>
        /// Saves a file under wwwroot/uploads/{folderName} - PUBLICLY reachable
        /// via app.UseStaticFiles() with no authentication. Use ONLY for content
        /// that is meant to be publicly viewable without login, e.g. profile
        /// pictures. Never use this for confidential/tenant-owned content such
        /// as case documents - use SaveSecureFileAsync for those.
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string folderName);

        /// <summary>
        /// Deletes a file previously saved via SaveFileAsync (public wwwroot/uploads store).
        /// </summary>
        void DeleteFile(string? relativePath);

        /// <summary>
        /// Saves a file OUTSIDE wwwroot, in a location app.UseStaticFiles() can
        /// never serve, regardless of middleware ordering or configuration.
        /// Required for any tenant-owned/confidential content (case documents,
        /// evidence, etc.) so the ONLY way to retrieve the bytes is through an
        /// authenticated, authorization-checked API endpoint
        /// (e.g. DownloadDocumentHandler), never a raw static URL.
        /// </summary>
        Task<string> SaveSecureFileAsync(IFormFile file, string folderName);

        /// <summary>
        /// Reads the raw bytes of a file previously saved via SaveSecureFileAsync.
        /// Callers must have already performed their own authorization check
        /// (e.g. IDocumentPermissionService.CanUserAccessDocumentAsync) before
        /// calling this - this method itself does not check permissions, only
        /// resolves and reads the file safely.
        /// </summary>
        Task<byte[]> ReadSecureFileAsync(string relativePath);

        /// <summary>
        /// True if a file previously saved via SaveSecureFileAsync still exists on disk.
        /// </summary>
        bool SecureFileExists(string relativePath);

        /// <summary>
        /// Deletes a file previously saved via SaveSecureFileAsync (secure, non-web-servable store).
        /// </summary>
        void DeleteSecureFile(string? relativePath);
    }
}
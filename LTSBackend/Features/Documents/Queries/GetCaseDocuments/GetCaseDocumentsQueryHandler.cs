using LTSBackend.Data;
using LTSBackend.Features.Documents.DTOs;
using LTSBackend.Services.DocumentPermissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Documents.Queries.GetCaseDocuments
{
    /// <summary>
    /// Lists every document attached to a case, filtered down to only the
    /// documents the requesting user is actually permitted to view. This
    /// mirrors GetDocumentHandler's per-document check (IDocumentPermissionService)
    /// so a restricted-mode Moharrir's own blind uploads correctly disappear
    /// from their own list view/download, exactly as they do for a single-
    /// document GET, rather than exposing a shortcut that bypasses that rule.
    /// </summary>
    public class GetCaseDocumentsQueryHandler : IRequestHandler<GetCaseDocumentsQuery, List<DocumentDetailDTO>>
    {
        private readonly AppDbContext _context;
        private readonly IDocumentPermissionService _permissionService;

        public GetCaseDocumentsQueryHandler(AppDbContext context, IDocumentPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public async Task<List<DocumentDetailDTO>> Handle(GetCaseDocumentsQuery request, CancellationToken cancellationToken)
        {
            var documents = await _context.Documents
                .AsNoTracking()
                .Include(d => d.DocumentType)
                .Include(d => d.Case)
                .Where(d => d.CaseID == request.CaseID && d.IsLatest)
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync(cancellationToken);

            if (documents.Count == 0)
                return new List<DocumentDetailDTO>();

            var uploaderIds = documents.Select(d => d.UploadedBy).Distinct().ToList();
            var uploaderNames = await _context.Users
                .AsNoTracking()
                .Where(u => uploaderIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => u.FullName, cancellationToken);

            var result = new List<DocumentDetailDTO>();

            foreach (var document in documents)
            {
                bool canView = await _permissionService.CanUserAccessDocumentAsync(
                    request.UserID, document.DocumentID, "View", cancellationToken);

                if (!canView) continue;

                result.Add(new DocumentDetailDTO
                {
                    DocumentID = document.DocumentID,
                    CaseID = document.CaseID,
                    CaseNumber = document.Case?.CaseNumber ?? "Unknown",
                    DocumentName = document.DocumentName,
                    FileName = document.FileName,
                    DocumentType = document.DocumentType?.TypeName ?? "Unknown",
                    FileSize = document.FileSize,
                    VersionNo = document.VersionNo,
                    IsLatest = document.IsLatest,
                    UploadedBy = uploaderNames.TryGetValue(document.UploadedBy, out var name) ? name : "Unknown User",
                    UploadedDate = document.UploadedDate,
                    Remarks = document.Remarks ?? string.Empty
                });
            }

            return result;
        }
    }
}

using LTSBackend.Comman.Enum;
using LTSBackend.Data;
using LTSBackend.Features.Documents.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Documents.Queries.GetCaseDocuments
{
    /// <summary>
    /// Lists every document attached to a case, filtered down to only the
    /// documents the requesting user is actually permitted to view.
    ///
    /// PERFORMANCE NOTE: this intentionally does NOT call
    /// IDocumentPermissionService.CanUserAccessDocumentAsync() per document.
    /// That method re-loads the user (with Role+Firm) and re-checks firm
    /// isolation from scratch on every single call - calling it once per
    /// document turned a list of N documents into roughly 2N+ sequential
    /// DB round trips and made the Documents tab visibly slow to load.
    /// Every access rule that method enforces (user active, firm not
    /// blocked, cross-firm isolation, role rule, case-assignment) is the
    /// SAME for every document in this result set because they all belong
    /// to the same case - so it's resolved once here. The only thing that
    /// can legitimately vary per document is a Moharrir's per-document
    /// permission override, which is fetched in a single batched query
    /// instead of one query per document.
    /// </summary>
    public class GetCaseDocumentsQueryHandler : IRequestHandler<GetCaseDocumentsQuery, List<DocumentDetailDTO>>
    {
        private readonly AppDbContext _context;

        public GetCaseDocumentsQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DocumentDetailDTO>> Handle(GetCaseDocumentsQuery request, CancellationToken cancellationToken)
        {
            // ================================================
            // 1. Resolve the case's FirmID directly (works even if the case
            //    has zero documents yet) and the requesting user, in parallel.
            // ================================================
            var caseFirmIdTask = _context.Cases
                .AsNoTracking()
                .Where(c => c.CaseID == request.CaseID)
                .Select(c => (int?)c.FirmID)
                .FirstOrDefaultAsync(cancellationToken);

            var userTask = _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Firm)
                .FirstOrDefaultAsync(u => u.UserID == request.UserID, cancellationToken);

            await Task.WhenAll(caseFirmIdTask, userTask);

            var caseFirmId = caseFirmIdTask.Result;
            var user = userTask.Result;

            if (caseFirmId == null || user == null || user.IsDeleted || !user.IsActive)
                return new List<DocumentDetailDTO>();

            if (user.Firm != null && (user.Firm.IsBlocked || user.Firm.IsDeleted))
                return new List<DocumentDetailDTO>();

            var role = user.GetRole();

            // ================================================
            // 2. Multi-tenant isolation (CRITICAL, same rule as
            //    CanUserAccessDocumentAsync): a user must never see
            //    documents belonging to another firm's case.
            // ================================================
            bool isSuperAdmin = role == UserRole.SuperAdmin;
            if (!isSuperAdmin && caseFirmId != user.FirmID)
                return new List<DocumentDetailDTO>();

            // ================================================
            // 3. Role-level gate, resolved ONCE for the whole case rather
            //    than once per document (all documents share the same
            //    CaseID, so the "is this user assigned to this case" answer
            //    cannot differ between them).
            // ================================================
            bool fullFirmAccess = isSuperAdmin || role == UserRole.FirmAdmin || role == UserRole.Partner;

            bool isAssignedToCase = false;
            if (!fullFirmAccess && role is UserRole.AssociateLawyer or UserRole.InternParalegal or UserRole.Moharrir)
            {
                var now = DateTime.UtcNow;
                isAssignedToCase = await _context.CaseAssignments
                    .AsNoTracking()
                    .AnyAsync(a => a.CaseID == request.CaseID && a.UserID == request.UserID
                        && (a.EndDate == null || a.EndDate > now), cancellationToken);
            }

            if (!fullFirmAccess && role is UserRole.AssociateLawyer or UserRole.InternParalegal && !isAssignedToCase)
                return new List<DocumentDetailDTO>();

            if (role == UserRole.Moharrir && !isAssignedToCase)
                return new List<DocumentDetailDTO>();

            if (!fullFirmAccess && role != UserRole.AssociateLawyer && role != UserRole.InternParalegal && role != UserRole.Moharrir)
                return new List<DocumentDetailDTO>(); // unrecognized role - deny by default, same as CanUserAccessDocumentAsync

            // ================================================
            // 4. Load the case's latest documents.
            // ================================================
            var documents = await _context.Documents
                .AsNoTracking()
                .Include(d => d.DocumentType)
                .Where(d => d.CaseID == request.CaseID && d.IsLatest)
                .OrderByDescending(d => d.UploadedDate)
                .ToListAsync(cancellationToken);

            if (documents.Count == 0)
                return new List<DocumentDetailDTO>();

            // ================================================
            // 5. Moharrir only: figure out per-document View permission in
            //    ONE batched query (user-specific override > role-based
            //    override > elevated/restricted role default), instead of
            //    a separate DocumentPermissions query per document.
            // ================================================
            HashSet<long>? moharrirVisibleDocIds = null;
            if (role == UserRole.Moharrir)
            {
                var documentIds = documents.Select(d => d.DocumentID).ToList();

                var overrides = await _context.DocumentPermissions
                    .AsNoTracking()
                    .Where(p => documentIds.Contains(p.DocumentID) && (p.UserID == request.UserID || p.RoleID == user.RoleID))
                    .ToListAsync(cancellationToken);

                bool isElevatedByDefault = user.Role != null && await _context.Users
                    .AsNoTracking()
                    .Where(u => u.UserID == request.UserID)
                    .Include(u => u.Role!).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                    .AnyAsync(u => u.Role!.RolePermissions.Any(rp => rp.Permission!.PermissionName == "ViewDocumentsIfPermitted"), cancellationToken);

                moharrirVisibleDocIds = new HashSet<long>();
                foreach (var docId in documentIds)
                {
                    var userOverride = overrides.FirstOrDefault(p => p.DocumentID == docId && p.UserID == request.UserID);
                    var roleOverride = overrides.FirstOrDefault(p => p.DocumentID == docId && p.RoleID == user.RoleID && p.UserID == null);

                    bool canView = userOverride != null ? userOverride.CanView
                        : roleOverride != null ? roleOverride.CanView
                        : isElevatedByDefault;

                    if (canView) moharrirVisibleDocIds.Add(docId);
                }
            }

            // ================================================
            // 6. Map to DTOs, applying the Moharrir per-document filter
            //    where applicable.
            // ================================================
            var uploaderIds = documents.Select(d => d.UploadedBy).Distinct().ToList();
            var approverIds = documents.Where(d => d.ApprovedBy.HasValue).Select(d => d.ApprovedBy!.Value).Distinct().ToList();
            var allNameIds = uploaderIds.Union(approverIds).ToList();
            var uploaderNames = await _context.Users
                .AsNoTracking()
                .Where(u => allNameIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => u.FullName, cancellationToken);

            var caseNumber = await _context.Cases
                .AsNoTracking()
                .Where(c => c.CaseID == request.CaseID)
                .Select(c => c.CaseNumber)
                .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

            // ================================================
            // DRAFT WORKFLOW (SRS - Intern/Paralegal): a draft document is
            // only listed for its own uploader and for Partner/FirmAdmin
            // (who approve it) - hidden from everyone else, including other
            // case-team members, until approved.
            // ================================================
            bool canSeeDrafts = role == UserRole.FirmAdmin || role == UserRole.Partner;

            var result = new List<DocumentDetailDTO>();
            foreach (var document in documents)
            {
                if (moharrirVisibleDocIds != null && !moharrirVisibleDocIds.Contains(document.DocumentID))
                    continue;

                if (document.IsDraft && document.UploadedBy != request.UserID && !canSeeDrafts)
                    continue;

                result.Add(new DocumentDetailDTO
                {
                    DocumentID = document.DocumentID,
                    CaseID = document.CaseID,
                    CaseNumber = caseNumber,
                    DocumentName = document.DocumentName,
                    FileName = document.FileName,
                    DocumentType = document.DocumentType?.TypeName ?? "Unknown",
                    FileSize = document.FileSize,
                    VersionNo = document.VersionNo,
                    IsLatest = document.IsLatest,
                    UploadedBy = uploaderNames.TryGetValue(document.UploadedBy, out var name) ? name : "Unknown User",
                    UploadedDate = document.UploadedDate,
                    Remarks = document.Remarks ?? string.Empty,
                    IsDraft = document.IsDraft,
                    ApprovedByName = document.ApprovedBy.HasValue && uploaderNames.TryGetValue(document.ApprovedBy.Value, out var approverName) ? approverName : null,
                    ApprovedDate = document.ApprovedDate
                });
            }

            return result;
        }
    }
}

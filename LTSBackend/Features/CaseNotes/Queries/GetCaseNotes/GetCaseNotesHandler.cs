using LTSBackend.Data;
using LTSBackend.Features.CaseNotes.DTOs;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.CaseNotes.Queries.GetCaseNotes
{
    /// <summary>
    /// SRS: "Confidential" notes are excluded for users below Associate/Moharrir level
    /// unless they are the note's author, to protect internal legal opinions.
    /// </summary>
    public class GetCaseNotesHandler(AppDbContext _context, ICurrentUserService _currentUser, IHttpContextAccessor _httpContextAccessor)
        : IRequestHandler<GetCaseNotesQuery, List<CaseNoteDetailDTO>>
    {
        public async Task<List<CaseNoteDetailDTO>> Handle(GetCaseNotesQuery request, CancellationToken cancellationToken)
        {
            int currentUserId = GetCurrentUserId();
            var currentUserRoleId = await _context.Users.Where(u => u.UserID == currentUserId).Select(u => u.RoleID).FirstOrDefaultAsync(cancellationToken);
            bool isElevated = currentUserRoleId is 1 or 2 or 3 or 4; // SuperAdmin..AssociateLawyer can see confidential notes

            var query = _context.CaseNotes
                .AsNoTracking()
                .Include(n => n.User)
                .Include(n => n.Case)
                .Where(n => n.CaseID == request.CaseID);

            // FIX: multi-tenant isolation - don't leak another firm's case notes
            if (!_currentUser.IsSuperAdmin)
                query = query.Where(n => n.Case.FirmID == _currentUser.FirmID);

            var notes = await query
                .Where(n => n.NoteType != "Confidential" || isElevated || n.UserID == currentUserId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync(cancellationToken);

            return notes.Select(n => new CaseNoteDetailDTO
            {
                NoteID = n.NoteID,
                CaseID = n.CaseID,
                UserID = n.UserID,
                UserName = n.User?.FullName,
                NoteType = n.NoteType,
                Notes = n.Notes,
                CreatedDate = n.CreatedDate
            }).ToList();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
using LTSBackend.Data;
using LTSBackend.Features.CaseNotes.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.CaseNotes.Queries.GetCaseNotes
{
    /// <summary>
    /// SRS: "Confidential" notes are excluded for users below Associate/Moharrir level
    /// unless they are the note's author, to protect internal legal opinions.
    /// </summary>
    public class GetCaseNotesHandler(AppDbContext _context, IHttpContextAccessor _httpContextAccessor)
        : IRequestHandler<GetCaseNotesQuery, List<CaseNoteDetailDTO>>
    {
        public async Task<List<CaseNoteDetailDTO>> Handle(GetCaseNotesQuery request, CancellationToken cancellationToken)
        {
            int currentUserId = GetCurrentUserId();
            var currentUserRoleId = await _context.Users.Where(u => u.UserID == currentUserId).Select(u => u.RoleID).FirstOrDefaultAsync(cancellationToken);
            bool isElevated = currentUserRoleId is 1 or 2 or 3 or 4; // SuperAdmin..AssociateLawyer can see confidential notes

            var notes = await _context.CaseNotes
                .AsNoTracking()
                .Include(n => n.User)
                .Where(n => n.CaseID == request.CaseID)
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

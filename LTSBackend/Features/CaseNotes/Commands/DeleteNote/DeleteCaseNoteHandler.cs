using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.CaseNotes.Commands.DeleteNote
{
    public class DeleteCaseNoteHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<DeleteCaseNoteCommand, bool>
    {
        public async Task<bool> Handle(DeleteCaseNoteCommand request, CancellationToken cancellationToken)
        {
            var note = await _context.CaseNotes.FirstOrDefaultAsync(n => n.NoteID == request.NoteID, cancellationToken);
            if (note == null)
                throw new NotFoundException($"Note ID {request.NoteID} nahi mila");

            int currentUserId = GetCurrentUserId();
            var currentUserRoleId = await _context.Users.Where(u => u.UserID == currentUserId).Select(u => u.RoleID).FirstOrDefaultAsync(cancellationToken);
            bool isElevated = currentUserRoleId is 1 or 2 or 3;

            if (note.UserID != currentUserId && !isElevated)
                throw new UnauthorizedException("Sirf note ka author ya senior staff isay delete kar sakta hai");

            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Case Note Deleted: NoteID {note.NoteID}"));

            _context.CaseNotes.Remove(note);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}

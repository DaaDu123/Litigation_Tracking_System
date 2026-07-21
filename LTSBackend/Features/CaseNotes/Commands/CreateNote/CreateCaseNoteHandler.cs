using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.CaseNotes.Commands.CreateNote
{
    public class CreateCaseNoteHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<CreateCaseNoteCommand, long>
    {
        public async Task<long> Handle(CreateCaseNoteCommand request, CancellationToken cancellationToken)
        {
            var caseExists = await _context.Cases.AnyAsync(c => c.CaseID == request.Note.CaseID, cancellationToken);
            if (!caseExists)
                throw new NotFoundException($"Case ID {request.Note.CaseID} nahi mila");

            int currentUserId = GetCurrentUserId();

            var note = new CaseNote
            {
                CaseID = request.Note.CaseID,
                UserID = currentUserId,
                NoteType = request.Note.NoteType,
                Notes = request.Note.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.CaseNotes.Add(note);
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Case Note Added ({note.NoteType}) for Case {request.Note.CaseID}"));

            await _context.SaveChangesAsync(cancellationToken);
            return note.NoteID;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}

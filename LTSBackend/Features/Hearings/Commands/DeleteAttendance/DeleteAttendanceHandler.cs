using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Hearings.Commands.DeleteAttendance
{
    public class DeleteAttendanceHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<DeleteAttendanceCommand, bool>
    {
        public async Task<bool> Handle(DeleteAttendanceCommand request, CancellationToken cancellationToken)
        {
            var attendance = await _context.HearingAttendances.FirstOrDefaultAsync(a => a.AttendanceID == request.AttendanceId, cancellationToken);
            if (attendance == null)
                throw new NotFoundException($"Attendance ID {request.AttendanceId} nahi mila");

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Hearing Attendance Deleted: AttendanceID {attendance.AttendanceID}"));

            _context.HearingAttendances.Remove(attendance);
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

using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Hearings.Commands.UpdateAttendance
{
    public class UpdateAttendanceHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<UpdateAttendanceCommand, bool>
    {
        public async Task<bool> Handle(UpdateAttendanceCommand request, CancellationToken cancellationToken)
        {
            var attendance = await _context.HearingAttendances.FirstOrDefaultAsync(a => a.AttendanceID == request.AttendanceId, cancellationToken);
            if (attendance == null)
                throw new NotFoundException($"Attendance ID {request.AttendanceId} nahi mila");

            attendance.Present = request.IsPresent;
            attendance.AttendanceRole = request.AttendanceRole;
            attendance.ArrivalTime = request.ArrivalTime;
            attendance.DepartureTime = request.DepartureTime;
            attendance.Remarks = request.Remarks;

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Hearing Attendance Updated: AttendanceID {attendance.AttendanceID}"));

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

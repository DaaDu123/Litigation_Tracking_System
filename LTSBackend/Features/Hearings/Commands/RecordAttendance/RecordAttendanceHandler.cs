using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Hearings.Commands.RecordAttendance
{
    /// <summary>
    /// SRS Reference: Complete Database Schema - HearingAttendance table
    /// (attendance tracking for lawyers/officers present at a hearing)
    /// </summary>
    public class RecordAttendanceHandler(AppDbContext _context,IAuditService _auditService,ICurrentUserService _currentUser,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<RecordAttendanceCommand, long>
    {
        public async Task<long> Handle(RecordAttendanceCommand request, CancellationToken cancellationToken)
        {
            var hearing = await _context.Hearings
                .Include(h => h.Case)
                .FirstOrDefaultAsync(h => h.HearingID == request.Attendance.HearingId, cancellationToken);

            if (hearing == null || (!_currentUser.IsSuperAdmin && hearing.Case.FirmID != _currentUser.FirmID))
                throw new NotFoundException($"Hearing ID {request.Attendance.HearingId} nahi mila");

            var userExists = await _context.Users.AnyAsync(u => u.UserID == request.Attendance.UserId, cancellationToken);
            if (!userExists)
                throw new NotFoundException($"User ID {request.Attendance.UserId} nahi mila");

            var duplicate = await _context.HearingAttendances.AnyAsync(a =>
                a.HearingID == request.Attendance.HearingId && a.UserID == request.Attendance.UserId, cancellationToken);
            if (duplicate)
                throw new ValidationException(new List<string> { "Is user ki attendance is hearing ke liye pehle se record hai" });

            var attendance = new HearingAttendance
            {
                HearingID = request.Attendance.HearingId,
                UserID = request.Attendance.UserId,
                Present = request.Attendance.IsPresent,
                Remarks = request.Attendance.Remarks
            };

            _context.HearingAttendances.Add(attendance);

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId,
                $"Hearing Attendance Recorded: UserID {request.Attendance.UserId} for HearingID {request.Attendance.HearingId} - Present: {request.Attendance.IsPresent}"));

            await _context.SaveChangesAsync(cancellationToken);
            return attendance.AttendanceID;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
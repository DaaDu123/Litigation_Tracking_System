using LTSBackend.Data;
using LTSBackend.Features.Hearings.DTOs;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Hearings.Queries.GetHearingAttendance
{
    public class GetHearingAttendanceHandler(AppDbContext _context, ICurrentUserService _currentUser) : IRequestHandler<GetHearingAttendanceQuery, List<HearingAttendanceDTO>>
    {
        public async Task<List<HearingAttendanceDTO>> Handle(GetHearingAttendanceQuery request, CancellationToken cancellationToken)
        {
            var query = _context.HearingAttendances
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Hearing)
                .Where(a => a.HearingID == request.HearingId);

            if (!_currentUser.IsSuperAdmin)
                query = query.Where(a => a.Hearing.Case.FirmID == _currentUser.FirmID);

            return await query.Select(a => new HearingAttendanceDTO
                {
                    AttendanceId = a.AttendanceID,
                    HearingId = a.HearingID,
                    UserId = a.UserID,
                    UserName = a.User.FullName,
                    AttendanceRole = a.AttendanceRole,
                    IsPresent = a.Present,
                    ArrivalTime = a.ArrivalTime,
                    DepartureTime = a.DepartureTime,
                    Remarks = a.Remarks
                })
                .ToListAsync(cancellationToken);
        }
    }
}
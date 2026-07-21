using LTSBackend.Data;
using LTSBackend.Features.Hearings.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Hearings.Queries.GetHearingAttendance
{
    public class GetHearingAttendanceHandler(AppDbContext _context) : IRequestHandler<GetHearingAttendanceQuery, List<HearingAttendanceDTO>>
    {
        public async Task<List<HearingAttendanceDTO>> Handle(GetHearingAttendanceQuery request, CancellationToken cancellationToken)
        {
            return await _context.HearingAttendances
                .AsNoTracking()
                .Include(a => a.User)
                .Where(a => a.HearingID == request.HearingId)
                .Select(a => new HearingAttendanceDTO
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

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LTSBackend.Data;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Services.CurrentUser;

namespace LTSBackend.Features.Hearings.Commands.UpdateHearing
{
    public class UpdateHearingCommandHandler : IRequestHandler<UpdateHearingCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateHearingCommandHandler(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(UpdateHearingCommand request, CancellationToken cancellationToken)
        {
            var hearing = await _context.Hearings
                .Include(h => h.Case)
                .FirstOrDefaultAsync(h => h.HearingID == request.Hearing.HearingId, cancellationToken);

            if (hearing == null || (!_currentUser.IsSuperAdmin && hearing.Case.FirmID != _currentUser.FirmID))
                throw new NotFoundException("Hearing not found");

            // Note: entity has "Purpose"/"Outcome" (not HearingPurpose/HearingOutcome), and NO ModifiedDate field
            hearing.HearingDate = request.Hearing.HearingDate;
            hearing.CourtRoom = request.Hearing.CourtRoom;
            hearing.JudgeName = request.Hearing.JudgeName;
            hearing.Purpose = request.Hearing.HearingPurpose;
            hearing.Outcome = request.Hearing.HearingOutcome;
            hearing.NextHearingDate = request.Hearing.NextHearingDate;
            hearing.Remarks = request.Hearing.Remarks;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
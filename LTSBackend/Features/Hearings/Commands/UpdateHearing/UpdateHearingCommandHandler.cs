using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LTSBackend.Data;
using LTSBackend.Comman.Exceptions;

namespace LTSBackend.Features.Hearings.Commands.UpdateHearing
{
    public class UpdateHearingCommandHandler (AppDbContext _context) : IRequestHandler<UpdateHearingCommand, bool>
    {      
        public async Task<bool> Handle(UpdateHearingCommand request, CancellationToken cancellationToken)
        {
            var hearing = await _context.Hearings
                .FirstOrDefaultAsync(h => h.HearingID == request.Hearing.HearingId, cancellationToken);

            if (hearing == null)
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
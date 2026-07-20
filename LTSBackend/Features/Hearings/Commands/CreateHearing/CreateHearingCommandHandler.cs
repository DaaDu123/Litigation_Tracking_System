using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Comman.Exceptions;

namespace LTSBackend.Features.Hearings.Commands.CreateHearing
{
    public class CreateHearingCommandHandler : IRequestHandler<CreateHearingCommand, long>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateHearingCommandHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<long> Handle(CreateHearingCommand request, CancellationToken cancellationToken)
        {
            // Validate case exists
            var caseExists = await _context.Cases.AnyAsync(c => c.CaseID == request.Hearing.CaseId, cancellationToken);
            if (!caseExists)
                throw new NotFoundException("Case not found");

            // Validate court exists
            var courtExists = await _context.Courts.AnyAsync(c => c.CourtID == request.Hearing.CourtId, cancellationToken);
            if (!courtExists)
                throw new NotFoundException("Court not found");

            // Get current user ID from JWT claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out int userId))
                throw new UnauthorizedException("User not authenticated");

            // Create hearing (Note: entity property names are "Purpose" and "Outcome", not "HearingPurpose"/"HearingOutcome")
            var hearing = new Hearing
            {
                CaseID = request.Hearing.CaseId,
                CourtID = request.Hearing.CourtId,
                HearingDate = request.Hearing.HearingDate,
                CourtRoom = request.Hearing.CourtRoom,
                JudgeName = request.Hearing.JudgeName,
                Purpose = request.Hearing.HearingPurpose,
                Remarks = request.Hearing.Remarks,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Hearings.Add(hearing);
            await _context.SaveChangesAsync(cancellationToken);

            return hearing.HearingID;
        }
    }
}
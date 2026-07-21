using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.CaseParties.Commands.UpdateCaseParty
{
    public class UpdateCasePartyHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<UpdateCasePartyCommand, bool>
    {
        public async Task<bool> Handle(UpdateCasePartyCommand request, CancellationToken cancellationToken)
        {
            var party = await _context.CaseParties.FirstOrDefaultAsync(p => p.PartyID == request.Party.PartyID, cancellationToken);
            if (party == null)
                throw new NotFoundException($"Party ID {request.Party.PartyID} nahi mila");

            party.PartyType = request.Party.PartyType;
            party.PartyName = request.Party.PartyName;
            party.Organization = request.Party.Organization;
            party.CNIC = request.Party.CNIC;
            party.NTN = request.Party.NTN;
            party.ContactNo = request.Party.ContactNo;
            party.Email = request.Party.Email;
            party.Address = request.Party.Address;
            party.LawyerName = request.Party.LawyerName;
            party.Remarks = request.Party.Remarks;

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Case Party Updated: {party.PartyName} (PartyID {party.PartyID})"));

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

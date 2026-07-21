using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.CaseParties.Commands.CreateCaseParty
{
    public class CreateCasePartyHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor,
        ILogger<CreateCasePartyHandler> _logger) : IRequestHandler<CreateCasePartyCommand, long>
    {
        public async Task<long> Handle(CreateCasePartyCommand request, CancellationToken cancellationToken)
        {
            var caseExists = await _context.Cases.AnyAsync(c => c.CaseID == request.Party.CaseID, cancellationToken);
            if (!caseExists)
                throw new NotFoundException($"Case ID {request.Party.CaseID} nahi mila");

            int currentUserId = GetCurrentUserId();

            var party = new CaseParty
            {
                CaseID = request.Party.CaseID,
                PartyType = request.Party.PartyType,
                PartyName = request.Party.PartyName,
                Organization = request.Party.Organization,
                CNIC = request.Party.CNIC,
                NTN = request.Party.NTN,
                ContactNo = request.Party.ContactNo,
                Email = request.Party.Email,
                Address = request.Party.Address,
                LawyerName = request.Party.LawyerName,
                Remarks = request.Party.Remarks
            };

            _context.CaseParties.Add(party);

            _context.AuditLogs.Add(_auditService.Create(currentUserId,
                $"Case Party Created: {party.PartyName} ({party.PartyType}) for Case {request.Party.CaseID}"));

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Case party created: {PartyID} for Case {CaseID}", party.PartyID, request.Party.CaseID);

            return party.PartyID;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}

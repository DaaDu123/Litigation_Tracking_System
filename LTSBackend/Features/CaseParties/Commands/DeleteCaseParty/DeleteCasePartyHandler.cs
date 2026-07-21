using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.CaseParties.Commands.DeleteCaseParty
{
    public class DeleteCasePartyHandler(
        AppDbContext _context,
        IAuditService _auditService,
        IHttpContextAccessor _httpContextAccessor) : IRequestHandler<DeleteCasePartyCommand, bool>
    {
        public async Task<bool> Handle(DeleteCasePartyCommand request, CancellationToken cancellationToken)
        {
            var party = await _context.CaseParties.FirstOrDefaultAsync(p => p.PartyID == request.PartyID, cancellationToken);
            if (party == null)
                throw new NotFoundException($"Party ID {request.PartyID} nahi mila");

            int currentUserId = GetCurrentUserId();
            _context.AuditLogs.Add(_auditService.Create(currentUserId, $"Case Party Deleted: {party.PartyName} (PartyID {party.PartyID})"));

            _context.CaseParties.Remove(party);
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

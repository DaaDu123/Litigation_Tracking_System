using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.CaseParties.DTOs;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.CaseParties.Queries.GetCasePartyById
{
    public class GetCasePartyByIdHandler(AppDbContext _context, ICurrentUserService _currentUser) : IRequestHandler<GetCasePartyByIdQuery, CasePartyDetailDTO>
    {
        public async Task<CasePartyDetailDTO> Handle(GetCasePartyByIdQuery request, CancellationToken cancellationToken)
        {
            var party = await _context.CaseParties.AsNoTracking()
                .Include(p => p.Case)
                .FirstOrDefaultAsync(p => p.PartyID == request.PartyID, cancellationToken);

            if (party == null || (!_currentUser.IsSuperAdmin && party.Case.FirmID != _currentUser.FirmID))
                throw new NotFoundException($"Party ID {request.PartyID} nahi mila");

            return new CasePartyDetailDTO
            {
                PartyID = party.PartyID,
                CaseID = party.CaseID,
                PartyType = party.PartyType,
                PartyName = party.PartyName,
                Organization = party.Organization,
                CNIC = party.CNIC,
                NTN = party.NTN,
                ContactNo = party.ContactNo,
                Email = party.Email,
                Address = party.Address,
                LawyerName = party.LawyerName,
                Remarks = party.Remarks
            };
        }
    }
}
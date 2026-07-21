using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.CaseParties.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.CaseParties.Queries.GetCasePartyById
{
    public class GetCasePartyByIdHandler(AppDbContext _context) : IRequestHandler<GetCasePartyByIdQuery, CasePartyDetailDTO>
    {
        public async Task<CasePartyDetailDTO> Handle(GetCasePartyByIdQuery request, CancellationToken cancellationToken)
        {
            var party = await _context.CaseParties.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PartyID == request.PartyID, cancellationToken);

            if (party == null)
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

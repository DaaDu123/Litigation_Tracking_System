using LTSBackend.Data;
using LTSBackend.Features.CaseParties.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.CaseParties.Queries.GetCaseParties
{
    public class GetCasePartiesHandler(AppDbContext _context) : IRequestHandler<GetCasePartiesQuery, List<CasePartyDetailDTO>>
    {
        public async Task<List<CasePartyDetailDTO>> Handle(GetCasePartiesQuery request, CancellationToken cancellationToken)
        {
            return await _context.CaseParties
                .AsNoTracking()
                .Where(p => p.CaseID == request.CaseID)
                .Select(p => new CasePartyDetailDTO
                {
                    PartyID = p.PartyID,
                    CaseID = p.CaseID,
                    PartyType = p.PartyType,
                    PartyName = p.PartyName,
                    Organization = p.Organization,
                    CNIC = p.CNIC,
                    NTN = p.NTN,
                    ContactNo = p.ContactNo,
                    Email = p.Email,
                    Address = p.Address,
                    LawyerName = p.LawyerName,
                    Remarks = p.Remarks
                })
                .ToListAsync(cancellationToken);
        }
    }
}

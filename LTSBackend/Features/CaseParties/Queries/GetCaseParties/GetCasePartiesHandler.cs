using LTSBackend.Data;
using LTSBackend.Features.CaseParties.DTOs;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.CaseParties.Queries.GetCaseParties
{
    public class GetCasePartiesHandler(AppDbContext _context, ICurrentUserService _currentUser) : IRequestHandler<GetCasePartiesQuery, List<CasePartyDetailDTO>>
    {
        public async Task<List<CasePartyDetailDTO>> Handle(GetCasePartiesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.CaseParties
                .AsNoTracking()
                .Where(p => p.CaseID == request.CaseID);

            // FIX: multi-tenant isolation
            if (!_currentUser.IsSuperAdmin)
                query = query.Where(p => p.Case.FirmID == _currentUser.FirmID);

            return await query
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
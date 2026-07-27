using LTSBackend.Data;
using LTSBackend.Features.CaseParties.DTOs;
using LTSBackend.Services.CurrentUser;
using LTSBackend.Services.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.CaseParties.Queries.GetCaseParties
{
    public class GetCasePartiesHandler(AppDbContext _context, ICurrentUserService _currentUser, IPermissionService _permissionService) : IRequestHandler<GetCasePartiesQuery, List<CasePartyDetailDTO>>
    {
        public async Task<List<CasePartyDetailDTO>> Handle(GetCasePartiesQuery request, CancellationToken cancellationToken)
        {
            // ================================================================
            // SECURITY FIX (IDOR / broken access control): this previously only
            // checked firm (tenant) scoping, which let ANY authenticated user in
            // the firm - including AssociateLawyer, Moharrir, and
            // InternParalegal - read party details for a case they are not
            // assigned to. Per the roles spec ("Cannot: Access unassigned
            // cases"), only SuperAdmin/FirmAdmin/Partner (full case-directory
            // visibility) or a user actually assigned to THIS case may see it -
            // mirrors the check already done in GetCaseAssignmentsHandler.
            // ================================================================
            if (!_currentUser.IsSuperAdmin && _currentUser.UserID.HasValue)
            {
                bool hasFullVisibility = await _permissionService.HasFullCaseDirectoryVisibilityAsync(_currentUser.UserID.Value, cancellationToken);

                if (!hasFullVisibility)
                {
                    bool isAssignedToCase = await _permissionService.IsUserAssignedToCaseAsync(_currentUser.UserID.Value, request.CaseID, cancellationToken);
                    if (!isAssignedToCase)
                    {
                        return new List<CasePartyDetailDTO>();
                    }
                }
            }

            var query = _context.CaseParties
                .AsNoTracking()
                .Where(p => p.CaseID == request.CaseID);

            // Multi-tenant isolation
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
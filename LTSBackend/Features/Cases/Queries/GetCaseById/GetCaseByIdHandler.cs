using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Cases.DTOs;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Cases.Queries.GetCaseById;

public class GetCaseByIdHandler(AppDbContext _context, ICurrentUserService _currentUser, ILogger<GetCaseByIdHandler> _logger) : IRequestHandler<GetCaseByIdQuery, CaseDTO?>
{
    public async Task<CaseDTO?> Handle(GetCaseByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Case fetch kia ja raha hai: {CaseID}", request.CaseID);

        // ================================================
        // 1. Find case with all relations (firm-scoped)
        // ================================================
        var query = _context.Cases
            .AsNoTracking()
            .Include(x => x.Court)
            .Include(x => x.Category)
            .Include(x => x.Status)
            .Include(x => x.Stage)
            .Include(x => x.Department)
            .Include(x => x.LegalOfficer)
            .Where(x => x.CaseID == request.CaseID);

        if (!_currentUser.IsSuperAdmin)
        {
            query = query.Where(x => x.FirmID == _currentUser.FirmID);
        }

        var caseRecord = await query.FirstOrDefaultAsync(cancellationToken);

        if (caseRecord == null)
        {
            _logger.LogWarning("Case nahi mila: {CaseID}", request.CaseID);
            throw new NotFoundException($"Case ID {request.CaseID} nahi mila");
        }

        // ================================================
        // 2. Map to DTO
        //    FIX: Department / LegalOfficer are nullable FKs (per schema),
        //    accessed with null-conditional to avoid NullReferenceException.
        // ================================================
        var caseDto = new CaseDTO
        {
            CaseID = caseRecord.CaseID,
            InternalReferenceNo = caseRecord.InternalReferenceNo,
            CaseNumber = caseRecord.CaseNumber,
            CaseTitle = caseRecord.CaseTitle,
            CaseDescription = caseRecord.CaseDescription,
            CourtName = caseRecord.Court.CourtName,
            CategoryName = caseRecord.Category.CategoryName,
            StatusName = caseRecord.Status.StatusName,
            StageName = caseRecord.Stage.StageName,
            DepartmentName = caseRecord.Department?.DepartmentName ?? "Not Assigned",
            LegalOfficerName = caseRecord.LegalOfficer?.FullName ?? "Not Assigned",
            Priority = caseRecord.Priority,
            SubjectMatter = caseRecord.SubjectMatter,
            FilingDate = caseRecord.FilingDate,
            InstitutionDate = caseRecord.InstitutionDate,
            RegistrationDate = caseRecord.RegistrationDate,
            ExpectedDisposalDate = caseRecord.ExpectedDisposalDate,
            ClaimedAmount = caseRecord.ClaimedAmount,
            PotentialLiability = caseRecord.PotentialLiability,
            IsArchived = caseRecord.IsArchived,
            CreatedDate = caseRecord.CreatedDate
        };

        _logger.LogInformation("Case successfully fetched: {CaseID}", request.CaseID);

        return caseDto;
    }
}
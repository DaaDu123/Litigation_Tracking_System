using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Features.Cases.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Cases.Queries.GetCaseById;

public class GetCaseByIdHandler(AppDbContext _context, ILogger<GetCaseByIdHandler> _logger) : IRequestHandler<GetCaseByIdQuery, CaseDTO?>
{
    public async Task<CaseDTO?> Handle(GetCaseByIdQuery request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Case fetch kia ja raha hai: {CaseID}", request.CaseID);

        // ================================================
        // 1. Find case with all relations
        // ================================================
        var caseRecord = await _context.Cases
            .AsNoTracking()
            .Include(x => x.Court)
            .Include(x => x.Category)
            .Include(x => x.Status)
            .Include(x => x.Stage)
            .Include(x => x.Department)
            .Include(x => x.LegalOfficer)
            .FirstOrDefaultAsync(x => x.CaseID == request.CaseID, cancellationToken);

        if (caseRecord == null)
        {
            _logger.LogWarning("Case nahi mila: {CaseID}", request.CaseID);
            throw new NotFoundException($"Case ID {request.CaseID} nahi mila");
        }

        // ================================================
        // 2. Map to DTO
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
            DepartmentName = caseRecord.Department.DepartmentName,
            LegalOfficerName = caseRecord.LegalOfficer.FullName,
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
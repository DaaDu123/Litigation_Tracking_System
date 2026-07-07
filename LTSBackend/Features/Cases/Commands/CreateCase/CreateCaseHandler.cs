using LTSBackend.Comman.Exceptions;
using LTSBackend.Common.Middleware;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LTSBackend.Features.Cases.Commands.CreateCase;

public class CreateCaseHandler(AppDbContext _context, IAuditService _auditService, ILogger<CreateCaseHandler> _logger) : IRequestHandler<CreateCaseCommand, long>
{
    public async Task<long> Handle(
        CreateCaseCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Naya Case create kia ja raha hai: {CaseNumber}", request.CaseNumber);

        // ================================================
        // 1. Check agar Case Number pehle se exist karta hai
        // ================================================
        bool caseExists = await _context.Cases
            .AsNoTracking()
            .AnyAsync(x => x.CaseNumber == request.CaseNumber, cancellationToken);

        if (caseExists)
        {
            _logger.LogWarning("Case Number pehle se exist karta hai: {CaseNumber}", request.CaseNumber);
            throw new ValidationException(new List<string>
            {
                $"Case Number '{request.CaseNumber}' pehle se exist karta hai"
            });
        }

        // ================================================
        // 2. Verify ke Court exist karta hai
        // ================================================
        bool courtExists = await _context.Courts
            .AsNoTracking()
            .AnyAsync(x => x.CourtID == request.CourtID, cancellationToken);

        if (!courtExists)
        {
            _logger.LogWarning("Court nahi mila: {CourtID}", request.CourtID);
            throw new NotFoundException($"Court ID {request.CourtID} nahi mila");
        }

        // ================================================
        // 3. Verify ke Category exist karta hai
        // ================================================
        bool categoryExists = await _context.CaseCategories
            .AsNoTracking()
            .AnyAsync(x => x.CategoryID == request.CategoryID, cancellationToken);

        if (!categoryExists)
        {
            _logger.LogWarning("Category nahi mila: {CategoryID}", request.CategoryID);
            throw new NotFoundException($"Category ID {request.CategoryID} nahi mila");
        }

        // ================================================
        // 4. Verify ke Department exist karta hai
        // ================================================
        bool deptExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(x => x.DepartmentID == request.ResponsibleDepartmentID, cancellationToken);

        if (!deptExists)
        {
            _logger.LogWarning("Department nahi mila: {DepartmentID}", request.ResponsibleDepartmentID);
            throw new NotFoundException($"Department ID {request.ResponsibleDepartmentID} nahi mila");
        }

        // ================================================
        // 5. Verify ke Legal Officer exist karta hai
        // ================================================
        bool officerExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.UserID == request.CurrentLegalOfficerID && x.IsActive && !x.IsDeleted,
                cancellationToken);

        if (!officerExists)
        {
            _logger.LogWarning("Legal Officer nahi mila: {LegalOfficerID}", request.CurrentLegalOfficerID);
            throw new NotFoundException($"Legal Officer ID {request.CurrentLegalOfficerID} nahi mila");
        }

        // ================================================
        // 6. Get default status (New)
        // ================================================
        var defaultStatus = await _context.CaseStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StatusName == "New", cancellationToken);

        if (defaultStatus == null)
        {
            _logger.LogError("Default status 'New' nahi mila database mein");
            throw new NotFoundException("Default status 'New' nahi mila");
        }

        // ================================================
        // 7. Get default stage (Filing)
        // ================================================
        var defaultStage = await _context.CaseStages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StageName == "Filing", cancellationToken);

        if (defaultStage == null)
        {
            _logger.LogError("Default stage 'Filing' nahi mila database mein");
            throw new NotFoundException("Default stage 'Filing' nahi mila");
        }

        // ================================================
        // 8. Generate unique Internal Reference Number
        // ================================================
        string internalRefNo = GenerateInternalReferenceNo();

        // ================================================
        // 9. Create new Case
        // ================================================
        var newCase = new Case
        {
            InternalReferenceNo = internalRefNo,
            CaseNumber = request.CaseNumber,
            CaseTitle = request.CaseTitle,
            CaseDescription = request.CaseDescription,
            CourtID = request.CourtID,
            CategoryID = request.CategoryID,
            StatusID = defaultStatus.StatusID,
            StageID = defaultStage.StageID,
            Priority = request.Priority,
            SubjectMatter = request.SubjectMatter,
            FilingDate = request.FilingDate,
            InstitutionDate = request.InstitutionDate,
            RegistrationDate = request.RegistrationDate,
            ExpectedDisposalDate = request.ExpectedDisposalDate,
            ClaimedAmount = request.ClaimedAmount,
            PotentialLiability = request.PotentialLiability,
            FinancialImplication = request.FinancialImplication,
            ResponsibleDepartmentID = request.ResponsibleDepartmentID,
            CurrentLegalOfficerID = request.CurrentLegalOfficerID,
            CreatedBy = 1, // TODO: Logged-in user se le na
            CreatedDate = DateTime.UtcNow,
            IsArchived = false
        };

        _context.Cases.Add(newCase);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Case create ho gaya: {CaseID} with RefNo: {InternalRefNo}",
            newCase.CaseID, internalRefNo);

        // ================================================
        // 10. Create initial Status History
        // ================================================
        var statusHistory = new CaseStatusHistory
        {
            CaseID = newCase.CaseID,
            OldStatusID = null,
            NewStatusID = defaultStatus.StatusID,
            ChangedBy = 1, // TODO: Logged-in user se le na
            ChangedDate = DateTime.UtcNow,
            Remarks = "Case create ho gaya"
        };

        _context.CaseStatusHistories.Add(statusHistory);

        // ================================================
        // 11. Create Audit Log
        // ================================================
        _context.AuditLogs.Add(
            _auditService.Create(1, $"Case Create: {newCase.CaseNumber}"));

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Case successfully created with ID: {CaseID}", newCase.CaseID);

        return newCase.CaseID;
    }

    // Internal Reference Number format: CASE-20240115-5D8K
    private static string GenerateInternalReferenceNo()
    {
        var now = DateTime.UtcNow;
        var randomPart = GenerateRandomString(4);
        return $"CASE-{now:yyyyMMdd}-{randomPart}";
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}
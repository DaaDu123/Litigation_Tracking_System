using LTSBackend.Comman.Exceptions;
using LTSBackend.Common.Middleware;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Cases.Commands.UpdateCase;

public class UpdateCaseHandler : IRequestHandler<UpdateCaseCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateCaseHandler> _logger;

    public UpdateCaseHandler(
        AppDbContext context,
        IAuditService auditService,
        ILogger<UpdateCaseHandler> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        UpdateCaseCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Case update kia ja raha hai: {CaseID}", request.CaseID);

        // ================================================
        // 1. Find Case
        // ================================================
        var caseToUpdate = await _context.Cases
            .FirstOrDefaultAsync(x => x.CaseID == request.CaseID, cancellationToken);

        if (caseToUpdate == null)
        {
            _logger.LogWarning("Case nahi mila: {CaseID}", request.CaseID);
            throw new NotFoundException($"Case ID {request.CaseID} nahi mila");
        }

        // ================================================
        // 2. Validate Court agar change ho raha hai
        // ================================================
        if (request.CourtID.HasValue && request.CourtID > 0)
        {
            bool courtExists = await _context.Courts
                .AsNoTracking()
                .AnyAsync(x => x.CourtID == request.CourtID, cancellationToken);

            if (!courtExists)
            {
                _logger.LogWarning("Court nahi mila: {CourtID}", request.CourtID);
                throw new NotFoundException($"Court ID {request.CourtID} nahi mila");
            }

            caseToUpdate.CourtID = request.CourtID.Value;
        }

        // ================================================
        // 3. Validate Category agar change ho raha hai
        // ================================================
        if (request.CategoryID.HasValue && request.CategoryID > 0)
        {
            bool categoryExists = await _context.CaseCategories
                .AsNoTracking()
                .AnyAsync(x => x.CategoryID == request.CategoryID, cancellationToken);

            if (!categoryExists)
            {
                _logger.LogWarning("Category nahi mila: {CategoryID}", request.CategoryID);
                throw new NotFoundException($"Category ID {request.CategoryID} nahi mila");
            }

            caseToUpdate.CategoryID = request.CategoryID.Value;
        }

        // ================================================
        // 4. Validate Stage agar change ho raha hai
        // ================================================
        if (request.StageID.HasValue && request.StageID > 0)
        {
            bool stageExists = await _context.CaseStages
                .AsNoTracking()
                .AnyAsync(x => x.StageID == request.StageID, cancellationToken);

            if (!stageExists)
            {
                _logger.LogWarning("Stage nahi mila: {StageID}", request.StageID);
                throw new NotFoundException($"Stage ID {request.StageID} nahi mila");
            }

            caseToUpdate.StageID = request.StageID.Value;
        }

        // ================================================
        // 5. Validate Legal Officer agar change ho raha hai
        // ================================================
        if (request.CurrentLegalOfficerID.HasValue && request.CurrentLegalOfficerID > 0)
        {
            bool officerExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.UserID == request.CurrentLegalOfficerID &&
                               x.IsActive && !x.IsDeleted,
                    cancellationToken);

            if (!officerExists)
            {
                _logger.LogWarning("Legal Officer nahi mila: {LegalOfficerID}",
                    request.CurrentLegalOfficerID);
                throw new NotFoundException(
                    $"Legal Officer ID {request.CurrentLegalOfficerID} nahi mila");
            }

            caseToUpdate.CurrentLegalOfficerID = request.CurrentLegalOfficerID.Value;
        }

        // ================================================
        // 6. Update optional fields
        // ================================================
        if (!string.IsNullOrEmpty(request.CaseNumber))
        {
            // Check agar doosra case same number use kar raha hai
            bool duplicateExists = await _context.Cases
                .AsNoTracking()
                .AnyAsync(x => x.CaseNumber == request.CaseNumber &&
                               x.CaseID != request.CaseID,
                    cancellationToken);

            if (duplicateExists)
            {
                throw new ValidationException(new List<string>
                {
                    $"Case Number '{request.CaseNumber}' pehle se use ho raha hai"
                });
            }

            caseToUpdate.CaseNumber = request.CaseNumber;
        }

        if (!string.IsNullOrEmpty(request.CaseTitle))
            caseToUpdate.CaseTitle = request.CaseTitle;

        if (!string.IsNullOrEmpty(request.CaseDescription))
            caseToUpdate.CaseDescription = request.CaseDescription;

        if (!string.IsNullOrEmpty(request.Priority))
            caseToUpdate.Priority = request.Priority;

        if (!string.IsNullOrEmpty(request.SubjectMatter))
            caseToUpdate.SubjectMatter = request.SubjectMatter;

        if (request.ExpectedDisposalDate.HasValue)
            caseToUpdate.ExpectedDisposalDate = request.ExpectedDisposalDate;

        if (request.ClaimedAmount.HasValue)
            caseToUpdate.ClaimedAmount = request.ClaimedAmount.Value;

        if (request.PotentialLiability.HasValue)
            caseToUpdate.PotentialLiability = request.PotentialLiability.Value;

        if (request.IsArchived.HasValue)
            caseToUpdate.IsArchived = request.IsArchived.Value;

        // ================================================
        // 7. Update timestamps
        // ================================================
        caseToUpdate.ModifiedBy = 1; // TODO: Logged-in user se le na
        caseToUpdate.ModifiedDate = DateTime.UtcNow;

        // ================================================
        // 8. Create Audit Log
        // ================================================
        _context.AuditLogs.Add(
            _auditService.Create(1, $"Case Update: {caseToUpdate.CaseNumber}"));

        // ================================================
        // 9. Save changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Case successfully updated: {CaseID}", request.CaseID);

        return true;
    }
}
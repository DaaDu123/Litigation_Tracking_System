using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Services.Audit;
using LTSBackend.Services.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Cases.Commands.CreateCase;

public class CreateCaseHandler(AppDbContext _context, IAuditService _auditService, ILogger<CreateCaseHandler> _logger, IHttpContextAccessor _httpContextAccessor, ICurrentUserService _currentUser) : IRequestHandler<CreateCaseCommand, long>
{
    public async Task<long> Handle(CreateCaseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Naya Case create kia ja raha hai: {CaseNumber}", request.CaseNumber);

        // ================================================
        // Get logged-in user ID + firm (multi-tenancy)
        // ================================================
        int currentUserId = GetCurrentUserId();

        if (_currentUser.FirmID == null)
        {
            _logger.LogWarning("User {UserId} without a firm attempted to create a case", currentUserId);
            throw new ValidationException(["Aap kisi firm se mansoob nahi hain, is liye case create nahi kar sakte."]);
        }
        int firmId = _currentUser.FirmID.Value;

        // ================================================
        // 1. Check agar Case Number pehle se exist karta hai
        //    (scoped to this firm - other firms may reuse numbers)
        // ================================================
        bool caseExists = await _context.Cases
            .AsNoTracking()
            .AnyAsync(x => x.CaseNumber == request.CaseNumber && x.FirmID == firmId, cancellationToken);

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
        var court = await _context.Courts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CourtID == request.CourtID, cancellationToken);

        if (court == null)
        {
            _logger.LogWarning("Court nahi mila: {CourtID}", request.CourtID);
            throw new NotFoundException($"Court ID {request.CourtID} nahi mila");
        }

        // ================================================
        // 3. Verify ke Category exist karta hai
        // ================================================
        var category = await _context.CaseCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CategoryID == request.CategoryID, cancellationToken);

        if (category == null)
        {
            _logger.LogWarning("Category nahi mila: {CategoryID}", request.CategoryID);
            throw new NotFoundException($"Category ID {request.CategoryID} nahi mila");
        }

        // ================================================
        // 4. Verify ke Department exist karta hai
        // ================================================
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentID == request.ResponsibleDepartmentID, cancellationToken);

        if (department == null)
        {
            _logger.LogWarning("Department nahi mila: {DepartmentID}", request.ResponsibleDepartmentID);
            throw new NotFoundException($"Department ID {request.ResponsibleDepartmentID} nahi mila");
        }

        // ================================================
        // 5. Verify ke Legal Officer exist karta hai
        // ================================================
        var legalOfficer = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserID == request.CurrentLegalOfficerID &&
                     x.IsActive &&
                     !x.IsDeleted &&
                     x.FirmID == firmId,
                cancellationToken);

        if (legalOfficer == null)
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
        //    FIX: previously used `new Random()` per call, which can
        //    produce colliding sequences under concurrent requests
        //    (time-based seeding). Now uses Random.Shared (thread-safe)
        //    plus a DB uniqueness retry loop, since InternalReferenceNo
        //    has a UNIQUE constraint at the database level.
        // ================================================
        string internalRefNo = await GenerateUniqueInternalReferenceNoAsync(cancellationToken);

        // ================================================
        // 9. Create new Case
        // ================================================
        var newCase = new Case
        {
            FirmID = firmId,
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
            CreatedBy = currentUserId,
            CreatedDate = DateTime.UtcNow,
            IsArchived = false
        };

        _context.Cases.Add(newCase);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Case created: {CaseID} with RefNo: {InternalRefNo}",
            newCase.CaseID, internalRefNo);

        // ================================================
        // 10. Create initial Status History
        // ================================================
        var statusHistory = new CaseStatusHistory
        {
            CaseID = newCase.CaseID,
            OldStatusID = null,
            NewStatusID = defaultStatus.StatusID,
            ChangedBy = currentUserId,
            ChangedDate = DateTime.UtcNow,
            Remarks = "Case create ho gaya"
        };

        _context.CaseStatusHistories.Add(statusHistory);

        // ================================================
        // 11. Create Audit Log
        // ================================================
        var auditLog = _auditService.Create(currentUserId, $"Case Create: {newCase.CaseNumber}");
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Case successfully created with ID: {CaseID}", newCase.CaseID);

        return newCase.CaseID;
    }

    private int GetCurrentUserId()
    {
        int currentUserId = 1; // Default fallback

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    currentUserId = userId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current user ID from context");
        }

        return currentUserId;
    }

    /// <summary>
    /// Generates an InternalReferenceNo and verifies against the DB that
    /// it isn't already taken, retrying a few times before giving up.
    /// InternalReferenceNo is UNIQUE at the DB level, so a collision here
    /// would otherwise surface as an unhandled DbUpdateException.
    /// </summary>
    private async Task<string> GenerateUniqueInternalReferenceNoAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var candidate = GenerateInternalReferenceNo();

            bool alreadyExists = await _context.Cases
                .AsNoTracking()
                .AnyAsync(x => x.InternalReferenceNo == candidate, cancellationToken);

            if (!alreadyExists)
            {
                return candidate;
            }

            _logger.LogWarning("InternalReferenceNo collision detected on attempt {Attempt}: {Candidate}", attempt, candidate);
        }

        // Extremely unlikely fallback: append a GUID fragment to guarantee uniqueness
        return $"CASE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24];
    }
    private static string GenerateInternalReferenceNo()
    {
        var now = DateTime.UtcNow;
        var randomPart = GenerateRandomString(4);
        return $"CASE-{now:yyyyMMdd}-{randomPart}";
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        // FIX: Random.Shared is thread-safe and avoids the identical-seed
        // collision risk of creating `new Random()` on every call under
        // concurrent requests.
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }
}
using LTSBackend.Comman.Responses;
using LTSBackend.Data;
using LTSBackend.Features.Cases.DTOs;
using LTSBackend.Features.Cases.Queries.GetAllCases;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Cases.Queries.GetAllCases;

public class GetAllCasesHandler(AppDbContext _context, ILogger<GetAllCasesHandler> _logger) : IRequestHandler<GetAllCasesQuery, PagedResult<CaseDTO>>
{
    public async Task<PagedResult<CaseDTO>> Handle(GetAllCasesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cases fetch kiye ja rahe hain - Page: {PageNumber}, Size: {PageSize}",
            request.PageNumber,
            request.PageSize);

        // ================================================
        // 1. Base query with relations
        // ================================================
        var query = _context.Cases
            .AsNoTracking()
            .Include(x => x.Court)
            .Include(x => x.Category)
            .Include(x => x.Status)
            .Include(x => x.Stage)
            .Include(x => x.Department)
            .Include(x => x.LegalOfficer)
            .AsQueryable();

        // ================================================
        // 2. Search filter - Case Number, Title, or Reference
        // ================================================
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim().ToLower();

            query = query.Where(x =>
                x.CaseNumber.ToLower().Contains(searchText) ||
                x.CaseTitle.ToLower().Contains(searchText) ||
                x.InternalReferenceNo.ToLower().Contains(searchText));

            _logger.LogInformation("Search filter apply kia: {SearchText}", searchText);
        }

        // ================================================
        // 3. Filter by Court
        // ================================================
        if (request.CourtID.HasValue && request.CourtID > 0)
        {
            query = query.Where(x => x.CourtID == request.CourtID.Value);
            _logger.LogInformation("Court filter apply kia: {CourtID}", request.CourtID);
        }

        // ================================================
        // 4. Filter by Status
        // ================================================
        if (request.StatusID.HasValue && request.StatusID > 0)
        {
            query = query.Where(x => x.StatusID == request.StatusID.Value);
            _logger.LogInformation("Status filter apply kia: {StatusID}", request.StatusID);
        }

        // ================================================
        // 5. Filter by Priority
        // ================================================
        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            query = query.Where(x => x.Priority == request.Priority);
            _logger.LogInformation("Priority filter apply kia: {Priority}", request.Priority);
        }

        // ================================================
        // 6. Exclude archived cases by default
        // ================================================
        query = query.Where(x => !x.IsArchived);

        // ================================================
        // 7. Get total records count
        // ================================================
        var totalRecords = await query.CountAsync(cancellationToken);

        // ================================================
        // 8. Apply pagination
        //    FIX: ResponsibleDepartmentID / CurrentLegalOfficerID are
        //    nullable FKs (per schema), so Department / LegalOfficer can
        //    be null. Guard against NullReferenceException here.
        // ================================================
        var cases = await query
            .OrderByDescending(x => x.CreatedDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new CaseDTO
            {
                CaseID = x.CaseID,
                InternalReferenceNo = x.InternalReferenceNo,
                CaseNumber = x.CaseNumber,
                CaseTitle = x.CaseTitle,
                CaseDescription = x.CaseDescription,
                CourtName = x.Court.CourtName,
                CategoryName = x.Category.CategoryName,
                StatusName = x.Status.StatusName,
                StageName = x.Stage.StageName,
                DepartmentName = x.Department != null ? x.Department.DepartmentName : "Not Assigned",
                LegalOfficerName = x.LegalOfficer != null ? x.LegalOfficer.FullName : "Not Assigned",
                Priority = x.Priority,
                SubjectMatter = x.SubjectMatter,
                FilingDate = x.FilingDate,
                InstitutionDate = x.InstitutionDate,
                RegistrationDate = x.RegistrationDate,
                ExpectedDisposalDate = x.ExpectedDisposalDate,
                ClaimedAmount = x.ClaimedAmount,
                PotentialLiability = x.PotentialLiability,
                IsArchived = x.IsArchived,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "{Count} cases fetch hoye of {Total} total",
            cases.Count,
            totalRecords);

        return new PagedResult<CaseDTO>
        {
            Items = cases,
            TotalRecords = totalRecords,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
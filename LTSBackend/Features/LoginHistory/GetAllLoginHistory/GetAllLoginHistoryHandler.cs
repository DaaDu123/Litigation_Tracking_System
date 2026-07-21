using LTSBackend.Comman.Responses;
using LTSBackend.Data;
using LTSBackend.Features.LoginHistory.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.LoginHistory.Queries.GetAllLoginHistory;

public class GetAllLoginHistoryHandler(AppDbContext context) : IRequestHandler<GetAllLoginHistoryQuery, PagedResult<LoginHistoryDTO>>
{
    public async Task<PagedResult<LoginHistoryDTO>> Handle(GetAllLoginHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = context.LoginHistories.AsNoTracking().Include(x => x.User).AsQueryable();

        //----------------------------------------
        // Search
        //----------------------------------------

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.User.FullName.Contains(search) || x.User.Email.Contains(search));
        }

        //----------------------------------------
        // Date Filter
        //----------------------------------------

        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.LoginTime >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.LoginTime <= request.ToDate.Value);
        }

        //----------------------------------------
        // Status Filter
        //----------------------------------------

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        //----------------------------------------
        // Total Records
        //----------------------------------------

        var totalRecords = await query.CountAsync(cancellationToken);

        //----------------------------------------
        // Data
        //    FIX: LoginHistory model has no CreatedDate column — the
        //    record is created at login time, so LoginTime is the
        //    closest equivalent. Mapped here instead of a non-existent
        //    x.CreatedDate (which was a compile error: 'LoginHistory'
        //    does not contain a definition for 'CreatedDate').
        //----------------------------------------

        var items = await query
            .OrderByDescending(x => x.LoginTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new LoginHistoryDTO
            {
                LoginID = x.LoginID,
                UserID = x.UserID,
                FullName = x.User.FullName,
                Email = x.User.Email,
                LoginTime = x.LoginTime,
                LogoutTime = x.LogoutTime,
                IPAddress = x.IPAddress,
                UserAgent = x.UserAgent,
                Status = x.Status,
                IsLoggedOut = x.IsLoggedOut,
                CreatedDate = x.LoginTime
            }).ToListAsync(cancellationToken);

        return new PagedResult<LoginHistoryDTO>
        {
            Items = items,
            TotalRecords = totalRecords,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
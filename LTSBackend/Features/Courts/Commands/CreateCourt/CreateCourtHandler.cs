using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Masters;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Courts.Commands.CreateCourt;

public sealed class CreateCourtHandler(AppDbContext _context, ILogger<CreateCourtHandler> _logger) : IRequestHandler<CreateCourtCommand, int>
{
    public async Task<int> Handle(CreateCourtCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating court: {CourtName}", request.CourtName);

        request = request with
        {
            CourtName = request.CourtName.Trim(),
            CourtType = request.CourtType?.Trim(),
            Jurisdiction = request.Jurisdiction?.Trim(),
            Address = request.Address?.Trim()
        };

        // ================================================
        // 1. Ensure court name is unique
        // ================================================
        bool exists = await _context.Courts.AnyAsync(x => x.CourtName.ToLower() == request.CourtName.ToLower(), cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Create failed: Court already exists: {CourtName}", request.CourtName);
            throw new ValidationException(new()
            {
                $"Court '{request.CourtName}' already exists."
            });
        }

        // ================================================
        // 2. Create court
        // ================================================
        var court = new Court
        {
            CourtName = request.CourtName,
            CourtType = request.CourtType,
            Jurisdiction = request.Jurisdiction,
            Address = request.Address,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow
        };

        _context.Courts.Add(court);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Court created successfully: {CourtID}", court.CourtID);

        return court.CourtID;
    }
}

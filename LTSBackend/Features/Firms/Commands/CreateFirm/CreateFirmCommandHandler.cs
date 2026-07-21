using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Security;
using LTSBackend.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Firms.Commands.CreateFirm;

public class CreateFirmCommandHandler(
    AppDbContext _context,
    IPasswordService _passwordService,
    ILogger<CreateFirmCommandHandler> _logger) : IRequestHandler<CreateFirmCommand, int>
{
    public async Task<int> Handle(CreateFirmCommand request, CancellationToken cancellationToken)
    {
        // 1. FirmCode must be unique
        bool codeExists = await _context.Firms
            .AsNoTracking()
            .AnyAsync(x => x.FirmCode == request.FirmCode, cancellationToken);

        if (codeExists)
            throw new ValidationException([$"Firm code '{request.FirmCode}' pehle se istemal ho raha hai."]);

        // 2. Admin email must be unique across the whole platform
        bool emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email == request.AdminEmail, cancellationToken);

        if (emailExists)
            throw new ValidationException([$"Email '{request.AdminEmail}' pehle se exist karta hai."]);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // 3. Create the firm
        var firm = new Firm
        {
            FirmName = request.FirmName,
            FirmCode = request.FirmCode.Trim().ToUpperInvariant(),
            Address = request.Address,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            CreatedBy = request.ActingUserID,
            CreatedAt = DateTime.UtcNow
        };
        _context.Firms.Add(firm);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Bootstrap the firm's first Firm Admin account
        var admin = new User
        {
            EmployeeNo = $"EMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
            FullName = request.AdminFullName,
            Email = request.AdminEmail,
            PasswordHash = _passwordService.HashPassword(request.AdminPassword),
            RoleID = (int)UserRole.FirmAdmin,
            FirmID = firm.FirmID,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Firm {FirmName} ({FirmCode}) created with admin {AdminEmail}",
            firm.FirmName, firm.FirmCode, admin.Email);

        return firm.FirmID;
    }
}

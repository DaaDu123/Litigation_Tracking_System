using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.Jwt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = LTSBackend.Models.RefreshToken;

namespace LTSBackend.Features.Auth.Login;

public class LoginHandler(
    AppDbContext context,
    IPasswordService passwordService,
    IJwtService jwtService,
    IAuditService auditService)
    : IRequestHandler<LoginCommand, LoginResponseDTO>
{
    public async Task<LoginResponseDTO> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (user is null)
            throw new UnauthorizedException("Invalid credentials.");

        bool valid = passwordService.VerifyPassword(request.Password, user.PasswordHash);
        if (!valid)
            throw new UnauthorizedException("Invalid credentials.");

        if (!user.IsActive)
            throw new ValidationException(new List<string> {
                "Account is not verified yet. Please verify the OTP sent to your email before logging in."
            });

        context.AuditLogs.Add(auditService.Create(user.UserID, "Login"));
        await context.SaveChangesAsync(cancellationToken);

        var accessToken = jwtService.GenerateToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        context.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserID = user.UserID,
            Token = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        });

        await context.SaveChangesAsync(cancellationToken);

        return new LoginResponseDTO
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }
}
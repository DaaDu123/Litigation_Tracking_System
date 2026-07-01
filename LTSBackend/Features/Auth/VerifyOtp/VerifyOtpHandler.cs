using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Jwt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = LTSBackend.Models.RefreshToken;

namespace LTSBackend.Features.Auth.VerifyOtp;

public class VerifyOtpHandler(
    AppDbContext context,
    IJwtService jwtService)
    : IRequestHandler<VerifyOtpCommand, VerifyOtpResponseDTO>
{
    public async Task<VerifyOtpResponseDTO> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        var otp = await context.UserOtps
            .FirstOrDefaultAsync(x =>
                x.Email == request.Email &&
                x.OtpCode == request.OtpCode &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        // FIXED: Use proper List<string> syntax
        if (otp is null)
            throw new ValidationException(new List<string> { "Invalid or expired OTP code." });

        var user = await context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Email == request.Email,
                cancellationToken);

        // FIXED: Use proper List<string> syntax
        if (user is null)
            throw new NotFoundException("User not found.");

        user.IsActive = true;
        otp.IsUsed = true;

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

        return new VerifyOtpResponseDTO
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Message = "Email verified successfully. You are now logged in."
        };
    }
}
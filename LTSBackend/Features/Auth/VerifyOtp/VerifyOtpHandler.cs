using LTSBackend.Comman.Enum;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Middleware;
using LTSBackend.Data;
using LTSBackend.Features.Auth.VerifyOtp;
using LTSBackend.Services.Jwt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = LTSBackend.Models.Security.RefreshToken;

namespace LTSBackend.Features.Auth.VerifyOtp;
public class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, VerifyOtpResponseDTO>
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<VerifyOtpHandler> _logger;

    public VerifyOtpHandler(
        AppDbContext context,
        IJwtService jwtService,
        ILogger<VerifyOtpHandler> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<VerifyOtpResponseDTO> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("OTP verification attempt for email: {Email}", request.Email);

        // ================================================
        // 1. Find and validate OTP (Purpose = Registration zaroori hai)
        // ================================================
        var otp = await _context.UserOtps
            .FirstOrDefaultAsync(x =>
                x.Email == request.Email &&
                x.OtpCode == request.OtpCode &&
                x.Purpose == OtpPurpose.Registration &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (otp == null)
        {
            _logger.LogWarning("OTP verification failed: Invalid or expired OTP for email: {Email}", request.Email);
            throw new ValidationException(
                new List<string> { "Invalid or expired OTP code." });
        }

        // ================================================
        // 2. Find user and load role
        // ================================================
        var user = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogError("OTP verification failed: User not found for email: {Email}", request.Email);
            throw new NotFoundException("User not found.");
        }

        // ================================================
        // 3. Activate user account and mark OTP as used
        // ================================================
        user.IsActive = true;
        otp.IsUsed = true;

        // ================================================
        // 4. Generate tokens
        // ================================================
        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // ================================================
        // 5. Save refresh token
        // ================================================
        _context.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserID = user.UserID,
            Token = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        });

        // ================================================
        // 6. Save all changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OTP verified successfully for user: {UserId}", user.UserID);

        return new VerifyOtpResponseDTO
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            AccessToken = accessToken,
            Message = "Email verified successfully! You can now login."
        };
    }
}
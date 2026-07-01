using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models;
using LTSBackend.Services.Email;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LTSBackend.Features.Auth.ResendOtp;

public class ResendOtpHandler(
    AppDbContext context,
    IEmailService emailService)
    : IRequestHandler<ResendOtpCommand, ResendOtpResponseDTO>
{
    public async Task<ResendOtpResponseDTO> Handle(
        ResendOtpCommand request,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var oldOtps = await context.UserOtps
            .Where(x => x.Email == request.Email && !x.IsUsed)
            .ToListAsync(cancellationToken);

        if (oldOtps.Count > 0)
        {
            context.UserOtps.RemoveRange(oldOtps);
        }

        string otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        context.UserOtps.Add(new UserOtp
        {
            Email = request.Email,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            UserID = user.UserID
        });

        await context.SaveChangesAsync(cancellationToken);
        await emailService.SendOtpEmailAsync(user.Email, user.FullName, otpCode);

        return new ResendOtpResponseDTO
        {
            Email = user.Email,
            // Same Spam/Junk reminder as Register, for consistency.
            Message = "OTP sent successfully. Please check your email (including the Spam/Junk folder)."
        };
    }
}
using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models;
using LTSBackend.Services.Jwt;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Auth.RefreshToken;

public class RefreshTokenHandler(AppDbContext context,IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, RefreshTokenResponseDTO>
{
    public async Task<RefreshTokenResponseDTO> Handle(RefreshTokenCommand request,CancellationToken cancellationToken)
    {
        var storedToken = await context.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken,cancellationToken);

        if (storedToken == null)
            throw new UnauthorizedException("Invalid refresh token.");

        if (storedToken.IsRevoked)
            throw new UnauthorizedException("Refresh token has been revoked.");

        if (storedToken.ExpiryDate <= DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired.");

        if (!storedToken.User.IsActive)
            throw new UnauthorizedException("User account is inactive.");

        // Purana token revoke
        storedToken.IsRevoked = true;

        // Naya Access Token
        var newAccessToken = jwtService.GenerateToken(storedToken.User);

        // Naya Refresh Token
        var newRefreshToken = jwtService.GenerateRefreshToken();

        context.RefreshTokens.Add(new Models.RefreshToken
        {
            UserID = storedToken.UserID,
            Token = newRefreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        });

        await context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponseDTO
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }
}
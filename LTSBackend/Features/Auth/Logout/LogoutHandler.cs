using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Auth.Logout;

public class LogoutHandler(AppDbContext context) : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request,CancellationToken cancellationToken)
    {
        var refreshToken = await context.RefreshTokens.FirstOrDefaultAsync
            (x => x.Token == request.RefreshToken,cancellationToken);

        if (refreshToken == null)
            throw new UnauthorizedException("Invalid refresh token.");

        if (refreshToken.IsRevoked)
            return true;

        refreshToken.IsRevoked = true;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
using MediatR;
namespace LTSBackend.Features.Auth.RefreshToken;
public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResponseDTO>;
using MediatR;
namespace LTSBackend.Features.Auth.Logout;
public record LogoutCommand(string RefreshToken) : IRequest<bool>;
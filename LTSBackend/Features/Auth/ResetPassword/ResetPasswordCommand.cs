using MediatR;
namespace LTSBackend.Features.Auth.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<ResetPasswordResponseDTO>;

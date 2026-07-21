using FluentValidation;
namespace LTSBackend.Features.Auth.Logout;

public class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        // Validation happens in handler — checks if refresh token exists in cookie
    }
}
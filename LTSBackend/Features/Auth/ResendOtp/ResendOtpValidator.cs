using FluentValidation;

namespace LTSBackend.Features.Auth.ResendOtp;

public class ResendOtpValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
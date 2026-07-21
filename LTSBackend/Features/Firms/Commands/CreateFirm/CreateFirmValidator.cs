using FluentValidation;

namespace LTSBackend.Features.Firms.Commands.CreateFirm;

public class CreateFirmValidator : AbstractValidator<CreateFirmCommand>
{
    public CreateFirmValidator()
    {
        RuleFor(x => x.FirmName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.FirmCode)
            .NotEmpty().MaximumLength(30)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("Firm code sirf letters, numbers aur hyphen par mushtamil ho sakta hai.");

        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.AdminFullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword)
            .NotEmpty().MinimumLength(8)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$")
            .WithMessage("Password mein uppercase, lowercase, digit aur symbol shamil hona chahiye.");
    }
}

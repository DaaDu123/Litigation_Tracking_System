using FluentValidation;

namespace LTSBackend.Features.Courts.Commands.CreateCourt;

public class CreateCourtValidator : AbstractValidator<CreateCourtCommand>
{
    public CreateCourtValidator()
    {
        RuleFor(x => x.CourtName)
            .NotEmpty()
            .WithMessage("Court name is required.")
            .MaximumLength(150)
            .WithMessage("Court name cannot exceed 150 characters.");

        RuleFor(x => x.CourtType)
            .NotEmpty()
            .WithMessage("Court type is required.")
            .MaximumLength(100)
            .WithMessage("Court type cannot exceed 100 characters.");

        RuleFor(x => x.Jurisdiction)
            .NotEmpty()
            .WithMessage("Jurisdiction is required.")
            .MaximumLength(150)
            .WithMessage("Jurisdiction cannot exceed 150 characters.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required.")
            .MaximumLength(255)
            .WithMessage("Address cannot exceed 255 characters.");
    }
}

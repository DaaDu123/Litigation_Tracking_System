using FluentValidation;

namespace LTSBackend.Features.Cases.Commands.UpdateCaseStatus;

public class UpdateCaseStatusValidator : AbstractValidator<UpdateCaseStatusCommand>
{
    public UpdateCaseStatusValidator()
    {
        RuleFor(x => x.CaseID)
            .GreaterThan(0)
            .WithMessage("Valid Case ID zaroori hai");

        RuleFor(x => x.NewStatusID)
            .GreaterThan(0)
            .WithMessage("Valid Status zaroori hai");

        RuleFor(x => x.Remarks)
            .MaximumLength(1000)
            .WithMessage("Remarks 1000 characters se zyada nahi ho sakta");
    }
}
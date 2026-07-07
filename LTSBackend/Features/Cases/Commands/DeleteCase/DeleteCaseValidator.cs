using FluentValidation;

namespace LTSBackend.Features.Cases.Commands.DeleteCase;

public class DeleteCaseValidator : AbstractValidator<DeleteCaseCommand>
{
    public DeleteCaseValidator()
    {
        RuleFor(x => x.CaseID)
            .GreaterThan(0)
            .WithMessage("Valid Case ID zaroori hai");
    }
}
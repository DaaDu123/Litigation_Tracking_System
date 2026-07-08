using FluentValidation;

namespace LTSBackend.Features.LoginHistory.Commands.DeleteOldHistory;

public class DeleteOldHistoryValidator : AbstractValidator<DeleteOldHistoryCommand>
{
    public DeleteOldHistoryValidator()
    {
        RuleFor(x => x.Days)
            .GreaterThan(0)
            .LessThanOrEqualTo(3650)
            .WithMessage("Days must be between 1 and 3650.");
    }
}
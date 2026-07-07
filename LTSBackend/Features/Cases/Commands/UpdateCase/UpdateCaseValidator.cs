using FluentValidation;

namespace LTSBackend.Features.Cases.Commands.UpdateCase;

public class UpdateCaseValidator : AbstractValidator<UpdateCaseCommand>
{
    public UpdateCaseValidator()
    {
        RuleFor(x => x.CaseID)
            .GreaterThan(0)
            .WithMessage("Valid Case ID zaroori hai");

        RuleFor(x => x.CaseNumber)
            .MaximumLength(100)
            .WithMessage("Case Number 100 characters se zyada nahi ho sakta")
            .When(x => !string.IsNullOrEmpty(x.CaseNumber));

        RuleFor(x => x.CaseTitle)
            .MaximumLength(255)
            .WithMessage("Case Title 255 characters se zyada nahi ho sakta")
            .When(x => !string.IsNullOrEmpty(x.CaseTitle));

        RuleFor(x => x.SubjectMatter)
            .MaximumLength(255)
            .WithMessage("Subject Matter 255 characters se zyada nahi ho sakta")
            .When(x => !string.IsNullOrEmpty(x.SubjectMatter));

        RuleFor(x => x.Priority)
            .Must(x => x == "High" || x == "Medium" || x == "Low")
            .WithMessage("Priority sirf High, Medium ya Low ho sakta hai")
            .When(x => !string.IsNullOrEmpty(x.Priority));

        RuleFor(x => x.CourtID)
            .GreaterThan(0)
            .WithMessage("Valid Court zaroori hai")
            .When(x => x.CourtID.HasValue);

        RuleFor(x => x.CategoryID)
            .GreaterThan(0)
            .WithMessage("Valid Category zaroori hai")
            .When(x => x.CategoryID.HasValue);

        RuleFor(x => x.CurrentLegalOfficerID)
            .GreaterThan(0)
            .WithMessage("Valid Legal Officer zaroori hai")
            .When(x => x.CurrentLegalOfficerID.HasValue);

        RuleFor(x => x.ClaimedAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Claimed Amount 0 se zyada ya equal hona chahiye")
            .When(x => x.ClaimedAmount.HasValue);

        RuleFor(x => x.PotentialLiability)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Potential Liability 0 se zyada ya equal hona chahiye")
            .When(x => x.PotentialLiability.HasValue);
    }
}
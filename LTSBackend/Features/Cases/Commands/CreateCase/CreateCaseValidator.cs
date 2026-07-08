using FluentValidation;

namespace LTSBackend.Features.Cases.Commands.CreateCase;

public class CreateCaseValidator : AbstractValidator<CreateCaseCommand>
{
    public CreateCaseValidator()
    {
        RuleFor(x => x.CaseNumber)
            .NotEmpty()
            .WithMessage("Case Number zaroori hai")
            .MaximumLength(100)
            .WithMessage("Case Number 100 characters se zyada nahi ho sakta");

        RuleFor(x => x.CaseTitle)
            .NotEmpty()
            .WithMessage("Case Title zaroori hai")
            .MaximumLength(255)
            .WithMessage("Case Title 255 characters se zyada nahi ho sakta");

        RuleFor(x => x.SubjectMatter)
            .NotEmpty()
            .WithMessage("Subject Matter zaroori hai")
            .MaximumLength(255)
            .WithMessage("Subject Matter 255 characters se zyada nahi ho sakta");

        RuleFor(x => x.Priority)
            .NotEmpty()
            .WithMessage("Priority zaroori hai")
            .Must(x => x == "High" || x == "Medium" || x == "Low")
            .WithMessage("Priority sirf High, Medium ya Low ho sakta hai");

        RuleFor(x => x.CourtID)
            .GreaterThan(0)
            .WithMessage("Valid Court zaroori hai");

        RuleFor(x => x.CategoryID)
            .GreaterThan(0)
            .WithMessage("Valid Category zaroori hai");

        RuleFor(x => x.ResponsibleDepartmentID)
            .GreaterThan(0)
            .WithMessage("Valid Department zaroori hai");

        RuleFor(x => x.CurrentLegalOfficerID)
            .GreaterThan(0)
            .WithMessage("Valid Legal Officer zaroori hai");

        RuleFor(x => x.FilingDate)
            .NotEmpty()
            .WithMessage("Filing Date zaroori hai")
            .LessThanOrEqualTo(DateTime.Now)
            .WithMessage("Filing Date aaj ya us se pehle hona chahiye");

        RuleFor(x => x.InstitutionDate)
            .NotEmpty()
            .WithMessage("Institution Date zaroori hai")
            .GreaterThanOrEqualTo(x => x.FilingDate)
            .WithMessage("Institution Date Filing Date se equal ya baad mein hona chahiye");

        RuleFor(x => x.RegistrationDate)
            .NotEmpty()
            .WithMessage("Registration Date zaroori hai")
            .GreaterThanOrEqualTo(x => x.InstitutionDate)
            .WithMessage("Registration Date Institution Date se equal ya baad mein hona chahiye");

        RuleFor(x => x.ExpectedDisposalDate)
            .GreaterThan(x => x.RegistrationDate)
            .WithMessage("Expected Disposal Date Registration Date se baad mein hona chahiye")
            .When(x => x.ExpectedDisposalDate.HasValue);

        RuleFor(x => x.ClaimedAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Claimed Amount 0 se zyada ya equal hona chahiye");

        RuleFor(x => x.PotentialLiability)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Potential Liability 0 se zyada ya equal hona chahiye");
    }
}
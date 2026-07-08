using FluentValidation;

namespace LTSBackend.Features.Cases.Queries.GetAllCases;

public class GetAllCasesValidator : AbstractValidator<GetAllCasesQuery>
{
    public GetAllCasesValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page Number 0 se zyada hona chahiye");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page Size 1 aur 100 ke beech hona chahiye");

        RuleFor(x => x.Priority)
            .Must(x => x == null || x == "High" || x == "Medium" || x == "Low")
            .WithMessage("Priority sirf High, Medium ya Low ho sakta hai");
    }
}
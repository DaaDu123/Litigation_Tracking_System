using FluentValidation;
namespace LTSBackend.Features.Profile.Commands;
public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Phone)
            .MaximumLength(20);

        RuleFor(x => x.Department)
            .MaximumLength(100);
        RuleFor(x => x.ProfileImage)
            .Must(file =>
             {
             if (file == null)
                 return true;

                  return file.Length <= 5 * 1024 * 1024;
             })
             .WithMessage("Maximum file size is 5 MB.");
        RuleFor(x => x.ProfileImage)
            .Must(file =>
            {
                if (file == null)
                    return true;
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                return allowed.Contains(Path.GetExtension(file.FileName).ToLower());
            }).WithMessage("Only jpg, jpeg, png and webp files are allowed.");
    }
}
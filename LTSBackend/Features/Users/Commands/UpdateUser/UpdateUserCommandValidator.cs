using FluentValidation;
using LTSBackend.Comman.Enum;
using LTSBackend.Models;

namespace LTSBackend.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserID)
            .GreaterThan(0);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Phone)
            .MaximumLength(20);

        RuleFor(x => x.Department)
            .MaximumLength(100);

        // BUG FIX: RoleID had no validation rule at all here previously.
        RuleFor(x => x.RoleID)
            .NotNull().WithMessage("Role is required.")
            .Must(roleId => roleId.HasValue && Enum.IsDefined(typeof(UserRole), roleId.Value))
            .WithMessage("RoleID must be a valid role (Admin=1, Lawyer=2, Clerk=3, Operator=4).");
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

        var allowed = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        return allowed.Contains(
            Path.GetExtension(file.FileName)
            .ToLowerInvariant());
    })
    .WithMessage("Only jpg, jpeg, png and webp files are allowed.");
    }
}

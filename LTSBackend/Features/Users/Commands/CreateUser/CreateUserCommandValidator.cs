using FluentValidation;
using LTSBackend.Comman.Enum;
using LTSBackend.Models;
namespace LTSBackend.Features.Users.Commands.CreateUser;
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.Phone)
            .MaximumLength(20);

        RuleFor(x => x.Department)
            .MaximumLength(100);

        RuleFor(x => x.RoleID)
            .NotNull().WithMessage("Role is required.")
            .Must(roleId => roleId.HasValue && Enum.IsDefined(typeof(UserRole), roleId.Value))
            .WithMessage("RoleID must be a valid role (Admin=1, Lawyer=2, Clerk=3, Operator=4).");

        RuleFor(x => x.ProfileImage)
            .Must(file =>
            {
                if (file == null)
                    return true;
                var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                return extensions.Contains(ext);
            })
            .WithMessage("Only jpg, jpeg, png and webp allowed.");
    }
}

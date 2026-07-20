using FluentValidation;
using LTSBackend.Comman.Enum;

namespace LTSBackend.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name zaroori hai")
            .MaximumLength(150)
            .WithMessage("Full name 150 characters se zyada nahi ho sakta");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email zaroori hai")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(150)
            .WithMessage("Email 150 characters se zyada nahi ho sakta");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password zaroori hai")
            .MinimumLength(8)
            .WithMessage("Password minimum 8 characters ka hona chahiye")
            .Matches("[A-Z]")
            .WithMessage("Password mein kam az kam aik uppercase letter hona chahiye")
            .Matches("[a-z]")
            .WithMessage("Password mein kam az kam aik lowercase letter hona chahiye")
            .Matches("[0-9]")
            .WithMessage("Password mein kam az kam aik digit hona chahiye")
            .Matches(@"[!@#$%^&*(),.?"":{}|<>_\-+=\[\]\\/;'~`]")
            .WithMessage("Password mein kam az kam aik symbol (!@#$%^&* etc.) hona chahiye");

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .WithMessage("Phone 20 characters se zyada nahi ho sakta")
            .Matches(@"^\+?[0-9\-\(\)\s]*$")
            .WithMessage("Phone format invalid hai")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Department)
            .MaximumLength(100)
            .WithMessage("Department 100 characters se zyada nahi ho sakta");

        RuleFor(x => x.RoleID)
            .NotNull()
            .WithMessage("Role zaroori hai")
            .GreaterThan(0)
            .WithMessage("Valid role zaroori hai")
            .Must(roleId => roleId.HasValue && Enum.IsDefined(typeof(UserRole), roleId.Value))
            .WithMessage("Invalid role");

        RuleFor(x => x.ProfileImage)
            .Must(file =>
            {
                if (file == null)
                    return true;

                return file.Length <= 5 * 1024 * 1024; // 5MB max
            })
            .WithMessage("Profile image 5 MB se zyada nahi ho sakta")
            .Must(file =>
            {
                if (file == null)
                    return true;

                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                return allowed.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
            })
            .WithMessage("Sirf JPG, JPEG, PNG, aur WebP formats allowed hain");
    }
}
using FluentValidation;

namespace LTSBackend.Features.Roles.Commands.CreateRole;

public class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.PermissionIds)
            .NotEmpty();
    }
}
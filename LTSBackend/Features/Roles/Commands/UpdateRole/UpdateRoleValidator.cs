using FluentValidation;

namespace LTSBackend.Features.Roles.Commands.UpdateRole;

public class UpdateRoleValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.RoleID)
            .GreaterThan(0);

        RuleFor(x => x.RoleName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.PermissionIds)
            .NotEmpty();
    }
}
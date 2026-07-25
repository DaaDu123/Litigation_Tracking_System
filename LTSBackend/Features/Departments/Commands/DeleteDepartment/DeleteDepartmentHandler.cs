using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentHandler(AppDbContext _context, ILogger<DeleteDepartmentHandler> _logger) : IRequestHandler<DeleteDepartmentCommand, bool>
{
    public async Task<bool> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting department: {DepartmentID}", request.DepartmentID);

        // ================================================
        // 1. Find department
        // ================================================
        var department = await _context.Departments.FirstOrDefaultAsync(x => x.DepartmentID == request.DepartmentID, cancellationToken);

        if (department == null)
        {
            _logger.LogWarning("Delete failed: Department not found: {DepartmentID}", request.DepartmentID);
            throw new NotFoundException("Department not found.");
        }

        // ================================================
        // 2. Block delete if cases reference this department
        // (Cases.ResponsibleDepartmentID has an FK with DeleteBehavior.NoAction,
        // so an unchecked delete would otherwise fail with a raw SQL FK error)
        // ================================================
        int caseCount = await _context.Cases.CountAsync(x => x.ResponsibleDepartmentID == request.DepartmentID, cancellationToken);

        if (caseCount > 0)
        {
            _logger.LogWarning("Delete failed: {Count} case(s) reference department: {DepartmentID}", caseCount, request.DepartmentID);

            throw new ValidationException(new()
            {
                $"Cannot delete department. {caseCount} case(s) are currently linked to it. " +
                "Reassign or archive those cases first, or deactivate the department instead."
            });
        }

        // ================================================
        // 3. Delete department
        // ================================================
        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Department deleted successfully: {DepartmentID}", request.DepartmentID);

        return true;
    }
}

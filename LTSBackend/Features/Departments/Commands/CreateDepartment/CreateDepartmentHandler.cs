using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Models.Masters;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentHandler(AppDbContext _context, ILogger<CreateDepartmentHandler> _logger) : IRequestHandler<CreateDepartmentCommand, int>
{
    public async Task<int> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating department: {DepartmentName}", request.DepartmentName);

        request = request with
        {
            DepartmentName = request.DepartmentName.Trim(),
            DepartmentCode = request.DepartmentCode?.Trim(),
            Description = request.Description?.Trim()
        };

        // ================================================
        // 1. Ensure department name is unique
        // ================================================
        bool nameExists = await _context.Departments.AnyAsync(x => x.DepartmentName.ToLower() == request.DepartmentName.ToLower(), cancellationToken);

        if (nameExists)
        {
            _logger.LogWarning("Create failed: Department already exists: {DepartmentName}", request.DepartmentName);
            throw new ValidationException(new()
            {
                $"Department '{request.DepartmentName}' already exists."
            });
        }

        // ================================================
        // 2. Ensure department code is unique (if provided)
        // ================================================
        if (!string.IsNullOrWhiteSpace(request.DepartmentCode))
        {
            bool codeExists = await _context.Departments.AnyAsync(x => x.DepartmentCode != null &&
               x.DepartmentCode.ToLower() == request.DepartmentCode.ToLower(),cancellationToken);

            if (codeExists)
            {
                _logger.LogWarning("Create failed: Department code already exists: {DepartmentCode}", request.DepartmentCode);
                throw new ValidationException(new()
                {
                    $"Department code '{request.DepartmentCode}' already exists."
                });
            }
        }

        // ================================================
        // 3. Create department
        // ================================================
        var department = new Department
        {
            DepartmentName = request.DepartmentName,
            DepartmentCode = request.DepartmentCode,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Department created successfully: {DepartmentID}", department.DepartmentID);

        return department.DepartmentID;
    }
}

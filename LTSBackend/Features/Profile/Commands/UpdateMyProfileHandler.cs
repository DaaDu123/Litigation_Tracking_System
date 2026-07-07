using LTSBackend.Comman.Exceptions;
using LTSBackend.Common.Middleware;
using LTSBackend.Data;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Profile.Commands;

public class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IFileService _fileService;
    private readonly ILogger<UpdateMyProfileHandler> _logger;

    public UpdateMyProfileHandler(
        AppDbContext context,
        IFileService fileService,
        ILogger<UpdateMyProfileHandler> logger)
    {
        _context = context;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        UpdateMyProfileCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating profile for user: {UserId}", request.UserID);

        // ================================================
        // 1. Find user
        // ================================================
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.UserID == request.UserID, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Profile update failed: User not found: {UserId}", request.UserID);
            throw new NotFoundException("User not found.");
        }

        // ================================================
        // 2. Handle profile image update
        // ================================================
        if (request.ProfileImage != null)
        {
            // Delete old image if exists
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                _fileService.DeleteFile(user.ProfileImage);
                _logger.LogInformation("Old profile image deleted for user: {UserId}", request.UserID);
            }

            // Upload new image
            user.ProfileImage = await _fileService.SaveFileAsync(request.ProfileImage, "profile_pictures");
            _logger.LogInformation("New profile image saved for user: {UserId}", request.UserID);
        }

        // ================================================
        // 3. Update user properties
        // ================================================
        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.Department = request.Department;
        user.UpdatedAt = DateTime.UtcNow;

        // ================================================
        // 4. Save changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Profile updated successfully for user: {UserId}", request.UserID);

        return true;
    }
}
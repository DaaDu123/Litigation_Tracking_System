using FluentValidation;

namespace LTSBackend.Features.Documents.Commands.UploadDocument;

/// <summary>
/// Validator for UploadDocumentCommand
/// </summary>
public class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator()
    {
        RuleFor(x => x.CaseID)
            .GreaterThan(0)
            .WithMessage("Valid Case ID is required");

        RuleFor(x => x.DocumentTypeID)
            .GreaterThan(0)
            .WithMessage("Document type is required");

        RuleFor(x => x.DocumentName)
            .NotEmpty()
            .WithMessage("Document name is required")
            .MaximumLength(255)
            .WithMessage("Document name cannot exceed 255 characters");

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required")
            .Must(f => f?.Length > 0)
            .WithMessage("File cannot be empty")
            .Must(f => f?.Length <= 50 * 1024 * 1024) // 50MB
            .WithMessage("File size cannot exceed 50MB")
            .Must(IsValidFileType)
            .WithMessage("Invalid file type. Allowed types: PDF, DOC, DOCX, XLS, XLSX, JPG, JPEG, PNG");

        RuleFor(x => x.Remarks)
            .MaximumLength(500)
            .WithMessage("Remarks cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Remarks));
    }

    /// <summary>
    /// Check agar file type valid hai
    /// </summary>
    private static bool IsValidFileType(IFormFile? file)
    {
        if (file == null)
            return false;

        var allowedExtensions = new[]
        {
            ".pdf",
            ".doc", ".docx",
            ".xls", ".xlsx",
            ".jpg", ".jpeg", ".png",
            ".txt",
            ".zip"
        };

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }
}
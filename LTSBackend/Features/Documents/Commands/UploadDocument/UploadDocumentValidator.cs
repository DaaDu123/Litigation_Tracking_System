using FluentValidation;
using LTSBackend.Comman.Security;

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
            .WithMessage("Invalid file type. Allowed types: PDF, DOC, DOCX, XLS, XLSX, JPG, JPEG, PNG, TXT")
            // SECURITY FIX (SRS "File Upload Security" -> "MIME validation"):
            // the extension check above only looks at the client-supplied
            // file name, which the uploader fully controls - renaming
            // "payload.exe" to "invoice.pdf" would previously sail
            // straight through. This second check reads the file's actual
            // leading bytes (magic number) and confirms they match a real
            // signature for the claimed extension, so a mislabeled/renamed
            // file is rejected here instead of ever reaching disk. See
            // Comman/Security/FileSignatureValidator.cs.
            .Must(HasValidContentSignature)
            .WithMessage("File content does not match its file extension. The file may be corrupted, mislabeled, or of a disallowed type.");

        RuleFor(x => x.Remarks)
            .MaximumLength(500)
            .WithMessage("Remarks cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Remarks));
    }

    /// <summary>
    /// Checks whether the file type is valid
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
            ".txt"
            // NOTE: ".zip" was intentionally removed. A ZIP is an
            // arbitrary-content archive - accepting it as a case document
            // means anything (including executables) can ride inside with
            // no content check possible short of a real malware scanner,
            // and there's no legitimate "legal document" reason to accept
            // raw archives here. If bulk upload is genuinely needed later,
            // it should go through server-side extraction + per-file
            // validation of each member, not a pass-through archive type.
        };

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }

    /// <summary>
    /// Verifies the file's actual content (magic bytes) matches its
    /// claimed extension. Only meaningful once the extension itself is
    /// already known-valid (IsValidFileType), so this never needs to
    /// handle an extension outside the allow-list above.
    /// </summary>
    private static bool HasValidContentSignature(IFormFile? file)
    {
        if (file == null)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        using var stream = file.OpenReadStream();
        return FileSignatureValidator.HasValidSignature(stream, extension);
    }
}
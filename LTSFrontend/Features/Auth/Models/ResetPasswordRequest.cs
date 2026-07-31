using System.ComponentModel.DataAnnotations;

namespace LTSFrontend.Features.Auth.Models
{
    /// <summary>Mirrors LTSBackend.Features.Auth.ResetPassword.ResetPasswordCommand / ResetPasswordValidator</summary>
    public class ResetPasswordRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Reset link is invalid or missing. Please request a new one.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*(),.?"":{}|<>_\-+=\[\]\\/;'~`]).+$",
            ErrorMessage = "Password must contain an uppercase letter, a lowercase letter, a digit, and a symbol.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(NewPassword) && NewPassword != ConfirmPassword)
            {
                yield return new ValidationResult(
                    "Passwords do not match.",
                    new[] { nameof(ConfirmPassword) });
            }
        }
    }
}

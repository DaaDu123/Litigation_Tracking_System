namespace LTSBackend.Services.Email
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);

        /// <summary>
        /// Sends the "forgot password" email containing a clickable, single-use
        /// reset link (no code for the user to type). Used instead of
        /// SendOtpEmailAsync for the password-reset flow.
        /// </summary>
        Task SendPasswordResetLinkAsync(string toEmail, string fullName, string resetLink, int expiryMinutes);

        /// <summary>
        /// Sends an in-app Notification's content out as an email too, so a
        /// user gets the alert both on their dashboard AND in their inbox
        /// (e.g. deadline alerts, hearing reminders, case assignments).
        /// </summary>
        Task SendNotificationEmailAsync(string toEmail, string fullName, string subject, string message);
    }
}
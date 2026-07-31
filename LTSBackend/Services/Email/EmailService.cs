using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace LTSBackend.Services.Email;

public class EmailService(IConfiguration _configuration, ILogger<EmailService> _logger) : IEmailService
{
    public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        var textBody = $"Dear {fullName},\r\n\r\n" +
                        $"Your OTP code is: {otpCode}\r\n" +
                        $"This code will expire in 5 minutes.\r\n\r\n" +
                        $"If you didn't request this, please ignore this email.\r\n\r\n" +
                        $"Regards,\r\nLTS System";

        await SendAsync(toEmail, fullName, "LTS - Your OTP Code", textBody);
        _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
    }

    public async Task SendPasswordResetLinkAsync(string toEmail, string fullName, string resetLink, int expiryMinutes)
    {
        var textBody = $"Dear {fullName},\r\n\r\n" +
                        $"We received a request to reset the password for your LTS account.\r\n\r\n" +
                        $"Click the link below to set a new password:\r\n{resetLink}\r\n\r\n" +
                        $"This link is valid for {expiryMinutes} minutes and can only be used once.\r\n\r\n" +
                        $"If you didn't request a password reset, no action is needed - your password will remain unchanged.\r\n\r\n" +
                        $"Regards,\r\nLTS System";

        await SendAsync(toEmail, fullName, "LTS - Reset Your Password", textBody);
        _logger.LogInformation("Password reset link email sent successfully to {Email}", toEmail);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string textBody)
    {
        try
        {
            string smtpHost = _configuration["EmailSettings:SmtpHost"]!;
            int smtpPort = Convert.ToInt32(_configuration["EmailSettings:SmtpPort"]);
            string senderEmail = _configuration["EmailSettings:SenderEmail"]!;
            string senderName = _configuration["EmailSettings:SenderName"]!;
            string appPassword = _configuration["EmailSettings:AppPassword"]!;

            _logger.LogInformation("SMTP Configuration - Host: {Host}, Port: {Port}, Sender: {Sender}", smtpHost, smtpPort, senderEmail);

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(new MailboxAddress(toName, toEmail));
            email.Subject = subject;
            email.Body = new TextPart("plain") { Text = textBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, appPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email ({Subject}) to {Email}", subject, toEmail);
            throw; // Re-throw to let caller handle
        }
    }
}
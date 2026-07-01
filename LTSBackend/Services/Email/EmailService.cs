using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace LTSBackend.Services.Email;
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        try
        {
            string smtpHost = _configuration["EmailSettings:SmtpHost"]!;
            int smtpPort = Convert.ToInt32(_configuration["EmailSettings:SmtpPort"]);
            string senderEmail = _configuration["EmailSettings:SenderEmail"]!;
            string senderName = _configuration["EmailSettings:SenderName"]!;
            string appPassword = _configuration["EmailSettings:AppPassword"]!;

            _logger.LogInformation("SMTP Configuration - Host: {Host}, Port: {Port}, Sender: {Sender}",
                smtpHost, smtpPort, senderEmail);

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(new MailboxAddress(fullName, toEmail));
            email.Subject = "LTS - Your OTP Code";

            email.Body = new TextPart("plain")
            {
                Text = $@"Dear {fullName},Your OTP code is: {otpCode}This code will expire in 5 minutes.
                If you didn't request this, please ignore this email.Regards,LTS System"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, appPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            throw; // Re-throw to let caller handle
        }
    }
}
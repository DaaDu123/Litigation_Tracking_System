namespace LTSBackend.Services.Email
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
    }
}
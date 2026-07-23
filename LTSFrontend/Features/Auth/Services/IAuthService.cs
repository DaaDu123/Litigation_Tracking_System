using LTSFrontend.Features.Auth.Models;

namespace LTSFrontend.Features.Auth.Services
{
    /// <summary>
    /// Client-side gateway to LTSBackend's AuthController.
    /// Every call goes through ApiClient, which unwraps ApiResponse&lt;T&gt;
    /// and throws ApiException on failure (Success = false / non-2xx).
    /// </summary>
    public interface IAuthService
    {
        Task<RegisterResponseDTO> RegisterAsync(RegisterRequest request);
        Task<VerifyOtpResponseDTO> VerifyOtpAsync(VerifyOtpRequest request);
        Task<ResendOtpResponseDTO> ResendOtpAsync(ResendOtpRequest request);
        Task<LoginResponseDTO> LoginAsync(LoginRequest request);
        Task<ForgotPasswordResponseDTO> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ResetPasswordResponseDTO> ResetPasswordAsync(ResetPasswordRequest request);
        Task LogoutAsync();
    }
}

namespace LTSFrontend.Features.Auth.Models
{
    /// <summary>Mirrors LTSBackend.Features.Auth.RefreshToken.RefreshTokenResponseDTO.</summary>
    public class RefreshTokenResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiry { get; set; }
    }
}

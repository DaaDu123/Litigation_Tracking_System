namespace LTSBackend.Features.Auth.RefreshToken;
public class RefreshTokenResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
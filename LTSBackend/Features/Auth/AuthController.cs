using LTSBackend.Comman.Responses;
using LTSBackend.Features.Auth.ChangePassword;
using LTSBackend.Features.Auth.Login;
using LTSBackend.Features.Auth.Logout;
using LTSBackend.Features.Auth.RefreshToken;
using LTSBackend.Features.Auth.Register;
using LTSBackend.Features.Auth.ResendOtp;
using LTSBackend.Features.Auth.ResetPassword;
using LTSBackend.Features.Auth.VerifyOtp;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LTSBackend.Features.Auth.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IMediator mediator) : ControllerBase
{
    // =====================
    // REGISTER
    // =====================
    [HttpPost("register")]
  //  [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        // NOTE: No try/catch here on purpose — ValidationException, NotFoundException, etc.
        // are handled centrally by GlobalExceptionMiddleware, same as every other action
        // in this controller. Keeping that consistent avoids double-handling and drift.
        var result = await mediator.Send(command);
        return Ok(ApiResponse<RegisterResponseDTO>.SuccessResponse(result, result.Message));
    }

    // =====================
    // LOGIN
    // =====================
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<LoginResponseDTO>.SuccessResponse(result, "Login Successful"));
    }

    // =====================
    // REFRESH TOKEN
    // =====================
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<RefreshTokenResponseDTO>.SuccessResponse(result, "Token refreshed successfully."));
    }

    // =====================
    // VERIFY OTP
    // =====================
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<VerifyOtpResponseDTO>.SuccessResponse(result, result.Message));
    }

    // =====================
    // CHANGE PASSWORD
    // =====================
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid or missing user identity."));

        var commandWithUserId = command with { UserID = userId };
        var result = await mediator.Send(commandWithUserId);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Password changed successfully."));
    }

    // =====================
    // RESET PASSWORD
    // =====================
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<ResetPasswordResponseDTO>.SuccessResponse(result, result.Message));
    }

    // =====================
    // RESEND OTP
    // =====================
    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<ResendOtpResponseDTO>.SuccessResponse(result, result.Message));
    }

    // =====================
    // LOGOUT
    // =====================
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Logout successful."));
    }
}
using LTSBackend.Comman.Responses;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
// Intentionally NOT using LTSBackend.Comman.Exceptions here — every exception type
// below is fully-qualified. FluentValidation also defines a `ValidationException`,
// so if a `using LTSBackend.Comman.Exceptions;` is ever added next to a
// `using FluentValidation;` in this file, an unqualified `ValidationException` becomes
// ambiguous and fails to compile. Fully-qualifying avoids that trap permanently.

namespace LTSBackend.Comman.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (LTSBackend.Comman.Exceptions.ValidationException ex)
        {
            await HandleValidationException(context, ex);
        }
        catch (LTSBackend.Comman.Exceptions.NotFoundException ex)
        {
            await HandleNotFoundException(context, ex);
        }
        catch (LTSBackend.Comman.Exceptions.UnauthorizedException ex)
        {
            await HandleUnauthorizedException(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGeneralException(context, ex);
        }
    }

    private static async Task HandleValidationException(HttpContext context, LTSBackend.Comman.Exceptions.ValidationException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message,
            Errors = ex.Errors
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response), context.RequestAborted);
    }

    private static async Task HandleNotFoundException(HttpContext context, LTSBackend.Comman.Exceptions.NotFoundException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response), context.RequestAborted);
    }

    private static async Task HandleUnauthorizedException(HttpContext context, LTSBackend.Comman.Exceptions.UnauthorizedException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response), context.RequestAborted);
    }

    private async Task HandleGeneralException(HttpContext context, Exception ex)
    {
        logger.LogError(ex, "Unhandled exception occurred while processing {Method} {Path}",
            context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "An unexpected error occurred. Please try again later."
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response), context.RequestAborted);
    }
}
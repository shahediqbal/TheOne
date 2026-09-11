using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using TheOne.Application.Authentication;
using TheOne.Application.Common.Models;

namespace TheOne.API.Authentication;

/// <summary>Translates expected authentication failures without exposing credentials or internal errors.</summary>
public sealed class AuthenticationExceptionHandler : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        ApiResponse<object> response;
        switch (exception)
        {
            case TheOne.Application.Administration.AdministrationException administration:
                context.Response.StatusCode = administration.StatusCode;
                response = ApiResponse<object>.FailureResponse(administration.Message);
                break;
            case OtpRequestLimitException:
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                response = ApiResponse<object>.FailureResponse("Please wait before requesting another code.");
                break;
            case OtpDeliveryException:
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                response = ApiResponse<object>.FailureResponse("SMS delivery is unavailable. Please try again later.");
                break;
            case ValidationException validation:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response = ApiResponse<object>.FailureResponse("Validation failed.",
                    validation.Errors.Select(x => x.ErrorMessage));
                break;
            case AuthenticationException authentication:
                context.Response.StatusCode = authentication.InvalidCredentials
                    ? StatusCodes.Status401Unauthorized : StatusCodes.Status400BadRequest;
                response = ApiResponse<object>.FailureResponse(authentication.Message, authentication.Errors);
                break;
            default:
                return false;
        }
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ProteinTracker.Api.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            FoodNotFoundException => (
                StatusCodes.Status404NotFound,
                "Food not found",
                exception.Message),
            FoodEntryNotFoundException => (
                StatusCodes.Status404NotFound,
                "Food entry not found",
                exception.Message),
            ArchivedFoodException => (
                StatusCodes.Status400BadRequest,
                "Archived food cannot be used",
                exception.Message),
            FoodDeletionConflictException => (
                StatusCodes.Status409Conflict,
                "Food is referenced by historical entries",
                exception.Message),
            EmailAlreadyRegisteredException => (
                StatusCodes.Status409Conflict,
                "Email already registered",
                exception.Message),
            InvalidCredentialsException => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                exception.Message),
            BusinessValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred while processing the request.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred.");
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            },
            cancellationToken);

        return true;
    }
}

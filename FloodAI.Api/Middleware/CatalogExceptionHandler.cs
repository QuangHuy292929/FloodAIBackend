using FloodAI.Business.Catalog.Admin;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Middleware;

public sealed class CatalogExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not CatalogException catalog)
        {
            return false;
        }

        var status = catalog.Error switch
        {
            CatalogError.NotFound => StatusCodes.Status404NotFound,
            CatalogError.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Detail = catalog.Message };
        if (!await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details
        }))
        {
            await context.Response.WriteAsJsonAsync(details, options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
        }
        return true;
    }
}

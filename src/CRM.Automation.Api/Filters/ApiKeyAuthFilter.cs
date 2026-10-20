using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CRM.Automation.Core.Models;

namespace CRM.Automation.Api.Filters;

/// <summary>
/// Authenticates requests from CheckPoint Engine using the x-Api-Key header.
/// </summary>
public class ApiKeyAuthFilter : IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "x-Api-Key";
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthFilter> _logger;

    public ApiKeyAuthFilter(IConfiguration configuration, ILogger<ApiKeyAuthFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuredKey = _configuration["Authentication:ApiKey"];

        // If no API key configured in appsettings, allow (e.g. in dev mode if explicitly empty)
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("Request to {Path} rejected: Missing {HeaderName} header", context.HttpContext.Request.Path, ApiKeyHeaderName);
            context.Result = new UnauthorizedObjectResult(
                BOProcessResult.Fail($"Missing '{ApiKeyHeaderName}' authentication header.", "MISSING_API_KEY")
            );
            return;
        }

        if (!string.Equals(configuredKey, extractedApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Request to {Path} rejected: Invalid API Key provided", context.HttpContext.Request.Path);
            context.Result = new UnauthorizedObjectResult(
                BOProcessResult.Fail("Invalid API Key provided.", "INVALID_API_KEY")
            );
            return;
        }

        await next();
    }
}

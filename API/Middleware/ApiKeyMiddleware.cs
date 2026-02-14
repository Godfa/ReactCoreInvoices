using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace API.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private const string API_KEY_HEADER = "X-API-Key";

        public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            // Skip API key check for CORS preflight requests
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Skip API key check for account endpoints (login, register, etc.)
            if (context.Request.Path.StartsWithSegments("/api/account"))
            {
                await _next(context);
                return;
            }

            // Skip API key check for health endpoint
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            // Skip API key check for Swagger in development
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            // Get API key from configuration
            var apiKey = configuration["ApiKey"];

            // If no API key is configured, skip validation (for backwards compatibility)
            if (string.IsNullOrEmpty(apiKey))
            {
                await _next(context);
                return;
            }

            // Check for API key in request header
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var providedApiKey))
            {
                _logger.LogWarning("API request without API key from {RemoteIp}",
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
                return;
            }

            if (!apiKey.Equals(providedApiKey))
            {
                _logger.LogWarning("Invalid API key provided from {RemoteIp}",
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
                return;
            }

            await _next(context);
        }
    }
}

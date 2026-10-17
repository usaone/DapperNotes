using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace DapperNotes.Api.Middleware
{
    public sealed class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;
        private readonly RequestTimingOptions _options;

        public RequestTimingMiddleware(
            RequestDelegate next,
            ILogger<RequestTimingMiddleware> logger,
            IOptions<RequestTimingOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                // Runs on success and on exceptions, so every request gets a timing line.
                stopwatch.Stop();
                var level = GetLogLevel(context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
                _logger.Log(level, "{Method} {Path} -> {StatusCode} in {Elapsed} ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private LogLevel GetLogLevel(int statusCode, long elapsedMs)
        {
            // 5xx: our fault. 4xx: the client sent bad input. Slow: worth a look. Everything else is routine.
            // Order matters: a slow 500 is still an Error, and a slow 404 is still a Warning either way.
            return statusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ when elapsedMs >= _options.SlowRequestThresholdMs => LogLevel.Warning,
                _ => LogLevel.Information
            };
        }
    }
}

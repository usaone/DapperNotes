//namespace DapperNotes.Api.Middleware
//{
//    public class RequestTimingMiddleware
//    {
//        private readonly RequestDelegate _next;
//        private readonly ILogger<RequestTimingMiddleware> _logger;

//        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
//        {
//            _next = next;
//            _logger = logger;
//        }

//        public async Task InvokeAsync(HttpContext context)
//        {
//            var sw = System.Diagnostics.Stopwatch.StartNew();
//            await _next(context);
//            sw.Stop();

//            _logger.LogInformation("{Method} {Path} -> {StatusCode} in {Elapsed} ms", context.Request.Method,
//                context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds);
//        }
//    }
//}

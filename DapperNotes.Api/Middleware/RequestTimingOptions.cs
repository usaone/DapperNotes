using System.ComponentModel.DataAnnotations;

namespace DapperNotes.Api.Middleware
{
    public sealed class RequestTimingOptions
    {
        public const string SectionName = "RequestTiming";

        /// <summary>
        /// Requests that take at least this many milliseconds are logged as a Warning.
        /// </summary>
        [Range(1, 10000)]
        public int SlowRequestThresholdMs { get; set; } = 500;
    }
}

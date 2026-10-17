namespace DapperNotes.Api.Security
{
    public static class SwaggerSecurity
    {
        public const string Scheme = "Basic";
        public const string Policy = "SwaggerAccess";
        public const string Role = "SwaggerUser";
        public const string PathPrefix = "/swagger";
    }
}

namespace DapperNotes.Api.Security
{
    public sealed class SwaggerUser
    {
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Never set in appsettings.json. Provide it with user-secrets or an environment variable.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = [];
    }
}

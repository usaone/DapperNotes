namespace DapperNotes.Api.Security
{
    public sealed class SwaggerAuthOptions
    {
        public const string SectionName = "SwaggerAuth";

        public List<SwaggerUser> Users { get; set; } = [];
    }
}

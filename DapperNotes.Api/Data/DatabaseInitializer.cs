using Dapper;

namespace DapperNotes.Api.Data
{
    public static class DatabaseInitializer
    {
        private const string CreateNotesTableSql = """
            CREATE TABLE IF NOT EXISTS Notes (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Title     TEXT NOT NULL,
                Content   TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;

        public static async Task InitializeAsync(IDbConnectionFactory connectionFactory)
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();
            await connection.ExecuteAsync(CreateNotesTableSql);
        }
    }
}

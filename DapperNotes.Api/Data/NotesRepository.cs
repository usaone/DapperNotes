using Dapper;
using DapperNotes.Api.Models;

namespace DapperNotes.Api.Data
{
    public sealed class NotesRepository : INotesRepository
    {
        private const string GetByIdSql = """
            SELECT Id, Title, Content, CreatedAt
            FROM Notes
            WHERE Id = @Id;
            """;

        private const string CreateSql = """
            INSERT INTO Notes (Title, Content, CreatedAt)
            VALUES (@Title, @Content, @CreatedAt)
            RETURNING Id, Title, Content, CreatedAt;
            """;

        private readonly IDbConnectionFactory _connectionFactory;

        public NotesRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<NoteDto?> GetByIdAsync(int id)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            return await connection.QuerySingleOrDefaultAsync<NoteDto>(GetByIdSql, new { Id = id });
        }

        public async Task<NoteDto> CreateAsync(CreateNoteRequest request)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            return await connection.QuerySingleAsync<NoteDto>(
                CreateSql,
                new { request.Title, request.Content, CreatedAt = DateTime.UtcNow });
        }
    }
}

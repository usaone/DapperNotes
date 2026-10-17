using Dapper;
using DapperNotes.Api.Models;

namespace DapperNotes.Api.Data
{
    public sealed class NotesRepository : INotesRepository
    {
        private const string GetAllSql = """
            SELECT Id, Title, Content, CreatedAt
            FROM Notes
            ORDER BY Id;
            """;

        private const string UpdateSql = """
            UPDATE Notes
            SET Title = @Title, Content = @Content
            WHERE Id = @Id
            RETURNING Id, Title, Content, CreatedAt;
            """;

        private const string DeleteSql = """
            DELETE FROM Notes
            WHERE Id = @Id;
            """;

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

        public async Task<IReadOnlyList<NoteDto>> GetAllAsync()
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            var notes = await connection.QueryAsync<NoteDto>(GetAllSql);
            return notes.AsList();
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

        public async Task<NoteDto?> UpdateAsync(int id, UpdateNoteRequest request)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            return await connection.QuerySingleOrDefaultAsync<NoteDto>(
                UpdateSql,
                new { Id = id, request.Title, request.Content });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            var rowsAffected = await connection.ExecuteAsync(DeleteSql, new { Id = id });
            return rowsAffected > 0;
        }
    }
}

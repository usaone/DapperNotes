using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace DapperNotes.Api.Data
{
    public sealed class SqliteConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqliteConnectionFactory(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
            _connectionString = connectionString;
        }

        /// <summary>
        /// Returns a new, closed connection. The caller opens it (OpenAsync) and disposes it.
        /// </summary>
        public DbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}

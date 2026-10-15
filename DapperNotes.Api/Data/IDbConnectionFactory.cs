using System.Data.Common;

namespace DapperNotes.Api.Data
{
    public interface IDbConnectionFactory
    {
        DbConnection CreateConnection();
    }
}

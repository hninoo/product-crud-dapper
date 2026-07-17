using System.Data;
using Microsoft.Data.SqlClient;

namespace ProductCrud.Web.Data;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}

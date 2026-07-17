using System.Data;

namespace ProductCrud.Web.Data;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}

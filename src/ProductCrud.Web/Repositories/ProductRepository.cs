using System.Data;
using Dapper;
using ProductCrud.Web.Data;
using ProductCrud.Web.Models;

namespace ProductCrud.Web.Repositories;

public sealed class ProductRepository : IProductRepository
{
    
    private static bool? _fullTextAvailable;

    private readonly ISqlConnectionFactory _connectionFactory;

    public ProductRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm, int page, int pageSize)
    {
        var term = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var offset = (page - 1) * pageSize;

        using var connection = _connectionFactory.CreateConnection();

        if (term is not null && await IsFullTextAvailableAsync(connection))
        {
            var fullTextQuery = BuildFullTextQuery(term);
            if (fullTextQuery is not null)
            {
                const string ftsSql = """
                    SELECT COUNT(*)
                    FROM dbo.Products
                    WHERE CONTAINS((Name, Description), @FullTextQuery);

                    SELECT ProductId,
                           Name,
                           Description,
                           Price,
                           StockQuantity,
                           IsActive,
                           CreatedAt,
                           UpdatedAt
                    FROM dbo.Products
                    WHERE CONTAINS((Name, Description), @FullTextQuery)
                    ORDER BY Name, ProductId
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                    """;

                using var ftsGrid = await connection.QueryMultipleAsync(ftsSql, new
                {
                    FullTextQuery = fullTextQuery,
                    Offset = offset,
                    PageSize = pageSize
                });

                var ftsTotal = await ftsGrid.ReadSingleAsync<int>();
                var ftsItems = (await ftsGrid.ReadAsync<Product>()).AsList();
                return (ftsItems, ftsTotal);
            }
        }

        const string sql = """
            SELECT COUNT(*)
            FROM dbo.Products
            WHERE @SearchTerm IS NULL
               OR Name LIKE '%' + @SearchTerm + '%'
               OR Description LIKE '%' + @SearchTerm + '%';

            SELECT ProductId,
                   Name,
                   Description,
                   Price,
                   StockQuantity,
                   IsActive,
                   CreatedAt,
                   UpdatedAt
            FROM dbo.Products
            WHERE @SearchTerm IS NULL
               OR Name LIKE '%' + @SearchTerm + '%'
               OR Description LIKE '%' + @SearchTerm + '%'
            ORDER BY Name, ProductId
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var grid = await connection.QueryMultipleAsync(sql, new
        {
            SearchTerm = term,
            Offset = offset,
            PageSize = pageSize
        });

        var total = await grid.ReadSingleAsync<int>();
        var items = (await grid.ReadAsync<Product>()).AsList();
        return (items, total);
    }

    private static async Task<bool> IsFullTextAvailableAsync(IDbConnection connection)
    {
        if (_fullTextAvailable is bool cached)
        {
            return cached;
        }

        const string sql = """
            SELECT CASE WHEN ISNULL(CAST(SERVERPROPERTY('IsFullTextInstalled') AS int), 0) = 1
                         AND EXISTS
                         (
                             SELECT 1
                             FROM sys.fulltext_indexes
                             WHERE object_id = OBJECT_ID(N'dbo.Products')
                         )
                   THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        var available = await connection.ExecuteScalarAsync<bool>(sql);
        _fullTextAvailable = available;
        return available;
    }

    private static string? BuildFullTextQuery(string searchTerm)
    {
        var tokens = searchTerm
            .Replace("\"", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return tokens.Length == 0
            ? null
            : string.Join(" OR ", tokens.Select(t => $"\"{t}*\""));
    }

    public async Task<Product?> GetByIdAsync(int productId)
    {
        const string sql = """
            SELECT ProductId,
                   Name,
                   Description,
                   Price,
                   StockQuantity,
                   IsActive,
                   CreatedAt,
                   UpdatedAt
            FROM dbo.Products
            WHERE ProductId = @ProductId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>(sql, new
        {
            ProductId = productId
        });
    }

    public async Task<int> CreateAsync(Product product)
    {
        const string sql = """
            INSERT INTO dbo.Products
            (
                Name,
                Description,
                Price,
                StockQuantity,
                IsActive
            )
            OUTPUT INSERTED.ProductId
            VALUES
            (
                @Name,
                @Description,
                @Price,
                @StockQuantity,
                @IsActive
            );
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        const string sql = """
            UPDATE dbo.Products
            SET Name = @Name,
                Description = @Description,
                Price = @Price,
                StockQuantity = @StockQuantity,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE ProductId = @ProductId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, product) == 1;
    }

    public async Task<bool> SetActiveAsync(int productId, bool isActive)
    {
        const string sql = """
            UPDATE dbo.Products
            SET IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE ProductId = @ProductId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, new
        {
            ProductId = productId,
            IsActive = isActive
        }) == 1;
    }

    public async Task<bool> DeleteAsync(int productId)
    {
        const string sql = """
            DELETE FROM dbo.Products
            WHERE ProductId = @ProductId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, new
        {
            ProductId = productId
        }) == 1;
    }

    public async Task<bool> NameExistsAsync(
        string name,
        int? excludingProductId = null)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.Products
                WHERE Name = @Name
                  AND (@ExcludingProductId IS NULL
                       OR ProductId <> @ExcludingProductId)
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new
        {
            Name = name.Trim(),
            ExcludingProductId = excludingProductId
        });
    }
}

using Microsoft.Data.SqlClient;
using OrgImporter.Models;

namespace OrgImporter.Services;

/// <summary>
/// Handles all database operations for the staging table.
/// Single responsibility — knows nothing about Excel or scripts.
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;
    private readonly string _tableName;

    public DatabaseService(string connectionString, string tableName)
    {
        _connectionString = connectionString;
        _tableName = tableName;
    }

    /// <summary>
    /// Creates the staging table if it does not exist.
    /// Safe to call multiple times — uses IF NOT EXISTS.
    /// If it already exists, truncates it so re-running is clean.
    /// </summary>
    public async Task EnsureTableAsync()
    {
        var sql = $@"
            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = '{_tableName}'
            )
            BEGIN
                CREATE TABLE [{_tableName}] (
                    Id          INT IDENTITY(1,1) NOT NULL
                        CONSTRAINT PK_{_tableName} PRIMARY KEY,
                    OrgCode     NVARCHAR(50)  NOT NULL,
                    Description NVARCHAR(255) NOT NULL,
                    InsertedAt  DATETIME2     NOT NULL
                        CONSTRAINT DF_{_tableName}_InsertedAt
                        DEFAULT GETUTCDATE()
                )
            END
            ELSE
            BEGIN
                TRUNCATE TABLE [{_tableName}]
            END";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Inserts all records in a single transaction.
    /// If any insert fails the entire batch is rolled back — no partial data.
    /// Returns the number of rows inserted.
    /// </summary>
    public async Task<int> InsertAllAsync(IReadOnlyList<OrganizationRecord> records)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync()
            as SqlTransaction
            ?? throw new InvalidOperationException("Could not start transaction.");

        try
        {
            int count = 0;

            foreach (var record in records)
            {
                const string sql = @"
                    INSERT INTO [{0}] (OrgCode, Description)
                    VALUES (@OrgCode, @Description)";

                await using var cmd = new SqlCommand(
                    string.Format(sql, _tableName), connection, transaction);

                cmd.Parameters.AddWithValue("@OrgCode", record.OrgCode);
                cmd.Parameters.AddWithValue("@Description", record.Description);

                await cmd.ExecuteNonQueryAsync();
                count++;

                Console.WriteLine(
                    $"  [{count:D3}] {record.OrgCode,-15} {record.Description}");
            }

            await transaction.CommitAsync();
            return count;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw; // Let Program.cs handle and report
        }
    }

    /// <summary>
    /// Reads all records from the staging table ordered by Id.
    /// Used for verification and script generation.
    /// </summary>
    public async Task<List<OrganizationRecord>> ReadAllAsync()
    {
        var sql = $@"
            SELECT Id, OrgCode, Description, InsertedAt
            FROM [{_tableName}]
            ORDER BY Id ASC";

        var result = new List<OrganizationRecord>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new OrganizationRecord
            {
                Id = reader.GetInt32(0),
                OrgCode = reader.GetString(1),
                Description = reader.GetString(2),
                InsertedAt = reader.GetDateTime(3)
            });
        }

        return result;
    }
}
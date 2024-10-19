using Microsoft.Data.Sqlite;

namespace Docron;

public sealed class DbMigration(
    ILogger<DbMigration> logger,
    IConfiguration configuration)
{
    private const string FolderName = "DbScripts";
    private const string FileName = "tables_sqlite.sql";

    public void Migrate()
    {
        var connectionString = configuration.GetDbConnection();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        if (CheckIfTableExists(connection, "QRTZ_JOB_DETAILS"))
        {
            logger.LogInformation("The migration was applied before");
            return;
        }

        var path = Path.Combine(FolderName, FileName);
        var script = File.ReadAllText(path);
        
        using var command = new SqliteCommand(script, connection);
        command.ExecuteNonQuery();

        logger.LogInformation("Quartz DB is migrated");
    }
    
    private static bool CheckIfTableExists(SqliteConnection connection, string tableName)
    {
        var query = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        var count = (long)command.ExecuteScalar()!;
        return count > 0;
    }
}
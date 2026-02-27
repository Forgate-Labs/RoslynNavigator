using System.Reflection;
using Microsoft.Data.Sqlite;

namespace RoslynNavigator.Snapshot.Services;

public class SnapshotSchemaService
{
    private const string ResourceName = "RoslynNavigator.Snapshot.Resources.SnapshotSchema.sql";
    private const string SchemaVersionColumn = "schema_version";
    private const int CurrentSchemaVersion = 2;

    private readonly SnapshotPathService _pathService;

    public SnapshotSchemaService() : this(new SnapshotPathService())
    {
    }

    public SnapshotSchemaService(SnapshotPathService pathService)
    {
        _pathService = pathService;
    }

    /// <summary>
    /// Initializes a snapshot database: creates the file if needed, loads and executes the embedded schema.
    /// Updates or inserts snapshot_meta row with generation timestamp and solution path.
    /// </summary>
    public void InitializeDatabase(string dbPath, string solutionPath)
    {
        if (string.IsNullOrEmpty(dbPath))
        {
            throw new ArgumentException("Database path cannot be null or empty", nameof(dbPath));
        }

        if (string.IsNullOrEmpty(solutionPath))
        {
            throw new ArgumentException("Solution path cannot be null or empty", nameof(solutionPath));
        }

        // Ensure parent directory exists
        _pathService.EnsureParentDirectoryExists(dbPath);

        // Load embedded SQL schema
        var schemaSql = LoadEmbeddedSchema();

        // Open connection and execute schema within a transaction
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            // Execute schema creation (idempotent due to IF NOT EXISTS)
            using (var command = connection.CreateCommand())
            {
                command.CommandText = schemaSql;
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }

            // Upsert snapshot_meta row
            UpsertSnapshotMeta(connection, transaction, solutionPath);

            EnsureColumnExists(connection, transaction, "methods", "parameter_count", "INTEGER DEFAULT 0");
            EnsureColumnExists(connection, transaction, "methods", "uses_insecure_random", "INTEGER DEFAULT 0");
            EnsureColumnExists(connection, transaction, "methods", "uses_weak_crypto", "INTEGER DEFAULT 0");
            EnsureColumnExists(connection, transaction, "methods", "catches_general_exception", "INTEGER DEFAULT 0");
            EnsureColumnExists(connection, transaction, "methods", "throws_general_exception", "INTEGER DEFAULT 0");
            EnsureColumnExists(connection, transaction, "methods", "has_sql_string_concatenation", "INTEGER DEFAULT 0");
            EnsureColumnExists(connection, transaction, "methods", "has_hardcoded_secret", "INTEGER DEFAULT 0");

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Initializes the database using the default path derived from the solution path.
    /// </summary>
    public string InitializeDefaultDatabase(string solutionPath)
    {
        var dbPath = _pathService.ResolveDefaultPath(solutionPath);
        InitializeDatabase(dbPath, solutionPath);
        return dbPath;
    }

    /// <summary>
    /// Checks if all required tables exist in the database.
    /// </summary>
    public bool ValidateTablesExist(string dbPath)
    {
        var requiredTables = new[]
        {
            "snapshot_meta",
            "classes",
            "methods",
            "dependencies",
            "calls",
            "annotations",
            "flags"
        };

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        foreach (var table in requiredTables)
        {
            if (!TableExists(connection, table))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the snapshot metadata for a database.
    /// Returns null if the database doesn't exist or hasn't been initialized.
    /// </summary>
    public SnapshotMeta? GetSnapshotMeta(string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            return null;
        }

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        // Check if snapshot_meta table exists
        if (!TableExists(connection, "snapshot_meta"))
        {
            return null;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, generated_at, solution_path, schema_version FROM snapshot_meta WHERE id = 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new SnapshotMeta
            {
                Id = reader.GetInt32(0),
                GeneratedAt = reader.GetString(1),
                SolutionPath = reader.GetString(2),
                SchemaVersion = reader.GetInt32(3)
            };
        }

        return null;
    }

    /// <summary>
    /// Loads the embedded SQL schema from the assembly resources.
    /// </summary>
    private string LoadEmbeddedSchema()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' not found. " +
                "Ensure SnapshotSchema.sql is included as an EmbeddedResource in the project file.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    private bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
        command.Parameters.AddWithValue("@name", tableName);

        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }

    /// <summary>
    /// Upserts the snapshot_meta row with current timestamp and solution path.
    /// </summary>
    private void UpsertSnapshotMeta(SqliteConnection connection, SqliteTransaction transaction, string solutionPath)
    {
        var generatedAt = DateTime.UtcNow.ToString("O");

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO snapshot_meta (id, generated_at, solution_path, schema_version)
            VALUES (1, @generatedAt, @solutionPath, @schemaVersion)
            ON CONFLICT(id) DO UPDATE SET
                generated_at = @generatedAt,
                solution_path = @solutionPath,
                schema_version = @schemaVersion";
        
        command.Parameters.AddWithValue("@generatedAt", generatedAt);
        command.Parameters.AddWithValue("@solutionPath", solutionPath);
        command.Parameters.AddWithValue("@schemaVersion", CurrentSchemaVersion);
        command.Transaction = transaction;

        command.ExecuteNonQuery();
    }

    private void EnsureColumnExists(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        string columnName,
        string columnSqlType)
    {
        using var checkCommand = connection.CreateCommand();
        checkCommand.Transaction = transaction;
        checkCommand.CommandText = $"PRAGMA table_info({tableName})";

        var exists = false;
        using (var reader = checkCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists)
        {
            return;
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.Transaction = transaction;
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnSqlType}";
        alterCommand.ExecuteNonQuery();
    }
}

public class SnapshotMeta
{
    public int Id { get; set; }
    public string GeneratedAt { get; set; } = string.Empty;
    public string SolutionPath { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
}


using Microsoft.Data.Sqlite;

if (args.Length == 0)
{
    PrintUsage();
    return 2;
}

var command = args[0].ToLowerInvariant();

try
{
    switch (command)
    {
        case "backup" when args.Length == 3:
            Backup(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]));
            break;

        case "restore" when args.Length == 3:
            Restore(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]));
            break;

        case "verify" when args.Length == 2:
            Verify(Path.GetFullPath(args[1]));
            break;

        default:
            PrintUsage();
            return 2;
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void Backup(string sourcePath, string backupPath)
{
    if (!File.Exists(sourcePath))
        throw new FileNotFoundException("Source database was not found.", sourcePath);

    if (File.Exists(backupPath))
        throw new IOException($"Backup already exists: {backupPath}");

    EnsureDifferentPaths(sourcePath, backupPath);
    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

    try
    {
        using var source = OpenDatabase(sourcePath, SqliteOpenMode.ReadOnly);
        using var destination = OpenDatabase(backupPath, SqliteOpenMode.ReadWriteCreate);

        source.BackupDatabase(destination);

        Verify(backupPath);
        Console.WriteLine($"Backup created and verified: {backupPath}");
    }
    catch
    {
        TryDelete(backupPath);
        throw;
    }
}

static void Restore(string backupPath, string targetPath)
{
    if (!File.Exists(backupPath))
        throw new FileNotFoundException("Backup database was not found.", backupPath);

    EnsureDifferentPaths(backupPath, targetPath);

    var targetDirectory = Path.GetDirectoryName(targetPath)!;
    Directory.CreateDirectory(targetDirectory);

    var temporaryPath = Path.Combine(
        targetDirectory,
        $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.restore");

    try
    {
        // Both connections are disposed before verification and replacement.
        using (var source = OpenDatabase(backupPath, SqliteOpenMode.ReadOnly))
        using (var destination = OpenDatabase(temporaryPath, SqliteOpenMode.ReadWriteCreate))
        {
            source.BackupDatabase(destination);
        }

        Verify(temporaryPath);

        // Replace the target only after the restored database passes integrity_check.
        if (File.Exists(targetPath))
        {
            File.Replace(temporaryPath, targetPath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temporaryPath, targetPath);
        }

        // The service must be stopped. Remove sidecars belonging to the old database.
        File.Delete(targetPath + "-wal");
        File.Delete(targetPath + "-shm");

        Console.WriteLine($"Database restored and verified: {targetPath}");
    }
    finally
    {
        TryDelete(temporaryPath);
    }
}

static void Verify(string databasePath)
{
    if (!File.Exists(databasePath))
        throw new FileNotFoundException("Database was not found.", databasePath);

    using var connection = OpenDatabase(databasePath, SqliteOpenMode.ReadOnly);
    using var command = connection.CreateCommand();

    command.CommandText = "PRAGMA integrity_check;";

    var result = command.ExecuteScalar()?.ToString();

    if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException(
            $"SQLite integrity check failed for '{databasePath}': {result}");

    Console.WriteLine($"SQLite integrity check passed: {databasePath}");
}

static SqliteConnection OpenDatabase(string path, SqliteOpenMode mode)
{
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = path,
        Mode = mode,
        Pooling = false
    }.ToString();

    var connection = new SqliteConnection(connectionString);
    connection.Open();

    return connection;
}

static void EnsureDifferentPaths(string first, string second)
{
    if (string.Equals(
        Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar),
        Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar),
        StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Source and destination must be different files.");
    }
}

static void TryDelete(string path)
{
    try
    {
        if (File.Exists(path))
            File.Delete(path);
    }
    catch
    {
        // Preserve the original operation's exception.
    }
}

static void PrintUsage()
{
    Console.WriteLine("JobSearchAssistant.DatabaseTool");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  backup  <source.db> <backup.db>");
    Console.WriteLine("  restore <backup.db> <target.db>");
    Console.WriteLine("  verify  <database.db>");
}

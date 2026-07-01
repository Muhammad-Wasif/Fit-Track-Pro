using Microsoft.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace FitTrack.Database;

/// <summary>
/// Connection string is loaded from .env → DB_CONNECTION_STRING.
/// Call DatabaseHelper.Initialise() once at app startup (Program.cs).
/// </summary>
public static class DatabaseHelper
{
    private static string _connectionString = string.Empty;

    public static void Initialise()
    {
        _connectionString = PrepareConnectionString(EnvLoader.ConnectionString);
    }

    /// <summary>Override — used in tests or if caller already has the string.</summary>
    public static void Initialise(string connectionString)
    {
        _connectionString = PrepareConnectionString(connectionString);
    }

    private static string PrepareConnectionString(string connStr)
    {
        if (string.IsNullOrEmpty(connStr)) return connStr;
        try
        {
            var builder = new SqlConnectionStringBuilder(connStr);
            if (string.IsNullOrEmpty(builder.InitialCatalog))
            {
                builder.InitialCatalog = "FitTrackOOP";
            }
            return builder.ConnectionString;
        }
        catch
        {
            return connStr;
        }
    }

    public static SqlConnection GetConnection()
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException(
                "DatabaseHelper not initialised. Call DatabaseHelper.Initialise() first.");
        return new SqlConnection(_connectionString);
    }

    /// <summary>Quick connectivity test — returns true if DB responds.</summary>
    public static bool TestConnection(out string error)
    {
        error = string.Empty;
        try
        {
            using var conn = GetConnection();
            conn.Open();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Checks if target database and core tables exist.
    /// Creates database, runs schema.sql, and imports food CSV if missing.
    /// </summary>
    public static void EnsureDatabaseAndTablesExist()
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("DatabaseHelper not initialised.");

        // 1. Get connection string builder to extract server credentials and target DB name
        var builder = new SqlConnectionStringBuilder(_connectionString);
        string targetDb = string.IsNullOrEmpty(builder.InitialCatalog) ? "FitTrackOOP" : builder.InitialCatalog;

        // Create a connection string pointing to master to check/create the target database
        var masterBuilder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        };
        string masterConnectionString = masterBuilder.ConnectionString;

        // 2. Ensure database exists
        try
        {
            using var masterConn = new SqlConnection(masterConnectionString);
            masterConn.Open();
            
            // Check if database exists
            using var checkDbCmd = new SqlCommand("SELECT database_id FROM sys.databases WHERE name = @DbName", masterConn);
            checkDbCmd.Parameters.AddWithValue("@DbName", targetDb);
            var dbId = checkDbCmd.ExecuteScalar();

            if (dbId == null || dbId == DBNull.Value)
            {
                // Create database
                using var createDbCmd = new SqlCommand($"CREATE DATABASE [{targetDb}]", masterConn);
                createDbCmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to verify database existence via master: {ex.Message}");
        }

        // 3. Verify tables exist. We connect directly to target database.
        using var conn = GetConnection();
        conn.Open();

        bool personsExist = false;
        try
        {
            using var checkTableCmd = new SqlCommand("SELECT OBJECT_ID(@TableName)", conn);
            checkTableCmd.Parameters.AddWithValue("@TableName", $"[{targetDb}].[dbo].[Persons]");
            var tableObjId = checkTableCmd.ExecuteScalar();
            personsExist = tableObjId != null && tableObjId != DBNull.Value;
        }
        catch
        {
            personsExist = false;
        }

        if (!personsExist)
        {
            // Load and execute the schema SQL script
            string schemaPath = Path.Combine(AppContext.BaseDirectory, "Data", "FitTrackOOP_Schema.sql");
            if (!File.Exists(schemaPath))
            {
                // Fallback: search parent folders in case of different run/test context
                string currentDir = AppContext.BaseDirectory;
                while (!string.IsNullOrEmpty(currentDir))
                {
                    string checkPath = Path.Combine(currentDir, "Data", "FitTrackOOP_Schema.sql");
                    if (File.Exists(checkPath))
                    {
                        schemaPath = checkPath;
                        break;
                    }
                    currentDir = Path.GetDirectoryName(currentDir)!;
                }
            }

            if (File.Exists(schemaPath))
            {
                string script = File.ReadAllText(schemaPath);
                // Split script by "GO" statements on their own line (case-insensitive)
                string[] commands = Regex.Split(
                    script,
                    @"^\s*GO\s*$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);

                foreach (string cmdText in commands)
                {
                    string trimmed = cmdText.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    try
                    {
                        using var cmd = new SqlCommand(trimmed, conn);
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to execute schema batch: {ex.Message}");
                    }
                }
            }
        }

        // 4. Ensure FoodItems CSV is imported
        bool foodItemsEmpty = true;
        try
        {
            using var checkFoodCmd = new SqlCommand("SELECT COUNT(1) FROM FoodItems", conn);
            int count = (int)checkFoodCmd.ExecuteScalar()!;
            foodItemsEmpty = count == 0;
        }
        catch
        {
            foodItemsEmpty = true;
        }

        if (foodItemsEmpty)
        {
            string csvPath = Path.Combine(AppContext.BaseDirectory, "Data", "FoodItems.csv");
            if (!File.Exists(csvPath))
            {
                string currentDir = AppContext.BaseDirectory;
                while (!string.IsNullOrEmpty(currentDir))
                {
                    string checkPath = Path.Combine(currentDir, "Data", "FoodItems.csv");
                    if (File.Exists(checkPath))
                    {
                        csvPath = checkPath;
                        break;
                    }
                    currentDir = Path.GetDirectoryName(currentDir)!;
                }
            }

            if (File.Exists(csvPath))
            {
                var lines = File.ReadAllLines(csvPath);
                if (lines.Length > 1) // First line is header
                {
                    string insertSql = @"
                        IF NOT EXISTS (SELECT 1 FROM FoodItems WHERE FoodName = @FoodName)
                        BEGIN
                            INSERT INTO FoodItems (FoodCategoryId, GoalId, FoodName, CaloriesPer100g, ProteinPer100g, CarbsPer100g, FatPer100g, FiberPer100g)
                            VALUES (@FoodCategoryId, @GoalId, @FoodName, @CaloriesPer100g, @ProteinPer100g, @CarbsPer100g, @FatPer100g, @FiberPer100g)
                        END";

                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        string[] parts = line.Split(',');
                        if (parts.Length < 8) continue;

                        try
                        {
                            int categoryId = int.Parse(parts[0]);
                            int? goalId = string.IsNullOrEmpty(parts[1]) ? null : (int?)int.Parse(parts[1]);
                            string name = parts[2].Trim();
                            double calories = double.Parse(parts[3]);
                            double protein = double.Parse(parts[4]);
                            double carbs = double.Parse(parts[5]);
                            double fat = double.Parse(parts[6]);
                            double? fiber = string.IsNullOrEmpty(parts[7]) ? null : (double?)double.Parse(parts[7]);

                            using var insertCmd = new SqlCommand(insertSql, conn);
                            insertCmd.Parameters.AddWithValue("@FoodCategoryId", categoryId);
                            insertCmd.Parameters.AddWithValue("@GoalId", (object?)goalId ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@FoodName", name);
                            insertCmd.Parameters.AddWithValue("@CaloriesPer100g", calories);
                            insertCmd.Parameters.AddWithValue("@ProteinPer100g", protein);
                            insertCmd.Parameters.AddWithValue("@CarbsPer100g", carbs);
                            insertCmd.Parameters.AddWithValue("@FatPer100g", fat);
                            insertCmd.Parameters.AddWithValue("@FiberPer100g", (object?)fiber ?? DBNull.Value);

                            insertCmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to import CSV row at line {i + 1}: {ex.Message}");
                        }
                    }
                }
            }
        }

        // 5. Apply incremental schema updates (e.g., TrainerLocked column)
        ApplyIncrementalUpdates(conn);
    }

    private static void ApplyIncrementalUpdates(SqlConnection conn)
    {
        // 1. Persons Table
        if (!ColumnExists(conn, "Persons", "TrainerLocked"))
            ExecuteSql(conn, "ALTER TABLE Persons ADD TrainerLocked BIT NOT NULL DEFAULT 0");

        if (!ColumnExists(conn, "Persons", "TrainerId"))
            ExecuteSql(conn, "ALTER TABLE Persons ADD TrainerId INT NULL REFERENCES Persons(PersonId)");

        if (!ColumnExists(conn, "Persons", "BodyFatPct"))
            ExecuteSql(conn, "ALTER TABLE Persons ADD BodyFatPct FLOAT NULL");

        // 2. Goals Table
        if (!ColumnExists(conn, "Goals", "CalorieDelta"))
            ExecuteSql(conn, "ALTER TABLE Goals ADD CalorieDelta INT NOT NULL DEFAULT 0");

        // 3. Exercises Table
        if (!ColumnExists(conn, "Exercises", "METValue"))
            ExecuteSql(conn, "ALTER TABLE Exercises ADD METValue FLOAT NOT NULL DEFAULT 3.0");

        // 4. New Tables
        if (!TableExists(conn, "LoginStreaks"))
        {
            ExecuteSql(conn, @"
                CREATE TABLE LoginStreaks (
                    StreakId       INT      PRIMARY KEY IDENTITY(1,1),
                    PersonId       INT      NOT NULL UNIQUE REFERENCES Persons(PersonId) ON DELETE CASCADE,
                    CurrentStreak  INT      NOT NULL DEFAULT 0,
                    LongestStreak  INT      NOT NULL DEFAULT 0,
                    LastLoginDate  DATE     NULL,
                    UpdatedAt      DATETIME NOT NULL DEFAULT GETDATE()
                )");
        }

        if (!TableExists(conn, "TrainerAttendance"))
        {
            ExecuteSql(conn, @"
                CREATE TABLE TrainerAttendance (
                    AttendanceId   INT      PRIMARY KEY IDENTITY(1,1),
                    TrainerId      INT      NOT NULL REFERENCES Persons(PersonId) ON DELETE CASCADE,
                    AttendDate     DATE     NOT NULL DEFAULT CAST(GETDATE() AS DATE),
                    Status         NVARCHAR(20) NOT NULL CHECK (Status IN ('Present','On Leave','Absent')),
                    MarkedBy       NVARCHAR(50) NOT NULL DEFAULT 'Admin',
                    Notes          NVARCHAR(300) NULL,
                    UNIQUE (TrainerId, AttendDate)
                )");
        }
    }

    private static bool ColumnExists(SqlConnection conn, string tableName, string columnName)
    {
        using var cmd = new SqlCommand(
            "SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(@Table) AND name = @Column", conn);
        cmd.Parameters.AddWithValue("@Table", tableName);
        cmd.Parameters.AddWithValue("@Column", columnName);
        return cmd.ExecuteScalar() != null;
    }

    private static bool TableExists(SqlConnection conn, string tableName)
    {
        using var cmd = new SqlCommand("SELECT OBJECT_ID(@Table)", conn);
        cmd.Parameters.AddWithValue("@Table", tableName);
        var result = cmd.ExecuteScalar();
        return result != null && result != DBNull.Value;
    }

    private static void ExecuteSql(SqlConnection conn, string sql)
    {
        try
        {
            using var cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Incremental update failed: {ex.Message}");
        }
    }
}

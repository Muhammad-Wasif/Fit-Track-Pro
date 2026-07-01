using FitTrack;
using FitTrack.Database;
using FitTrack.GUI.Forms;

namespace FitTrackPro;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // ── 1. Load .env (connection string) ──────────────────────
        try
        {
            EnvLoader.Load();
        }
        catch (FileNotFoundException ex)
        {
            MessageBox.Show(
                ex.Message + "\n\nCreate a .env file next to FitTrackPro.exe:\n" +
                "DB_CONNECTION_STRING=Server=localhost;Database=FitTrackOOP;" +
                "Integrated Security=True;TrustServerCertificate=True;",
                "Configuration Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // ── 2. Initialise DatabaseHelper (reads from EnvLoader) ────
        DatabaseHelper.Initialise();

        // ── 2b. Ensure database, tables and seed data exist ────────
        try
        {
            DatabaseHelper.EnsureDatabaseAndTablesExist();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Database auto-configuration failed:\n\n{ex.Message}\n\n" +
                "The application will try to proceed, but you may experience database errors.",
                "Initialization Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // ── 3. Quick connectivity check ────────────────────────────
        if (!DatabaseHelper.TestConnection(out string dbError))
        {
            MessageBox.Show(
                $"Cannot connect to database.\n\n{dbError}\n\n" +
                "Verify DB_CONNECTION_STRING in your .env file and ensure SQL Server is running.",
                "Database Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // ── 4. Launch app ──────────────────────────────────────────
        try
        {
            Application.Run(new LandingForm());
        }
        catch (Exception ex)
        {
            File.WriteAllText("crash.log", ex.ToString());
            MessageBox.Show(ex.ToString(), "Fatal Error");
        }
    }
}

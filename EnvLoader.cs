namespace FitTrack;

/// <summary>
/// Reads key=value pairs from a .env file located next to the executable.
/// Usage: EnvLoader.Load() → then DatabaseHelper.ConnectionString
/// </summary>
public static class EnvLoader
{
    private static readonly Dictionary<string, string> _vars = new();

    public static void Load(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, ".env");

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $".env file not found at: {path}\n" +
                "Create it from .env.example and set DB_CONNECTION_STRING.");

        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

            int eq = line.IndexOf('=');
            if (eq < 1) continue;

            string key = line[..eq].Trim();
            string val = line[(eq + 1)..].Trim();
            _vars[key] = val;
        }
    }

    public static string Get(string key)
    {
        if (_vars.TryGetValue(key, out string? val)) return val;
        throw new KeyNotFoundException(
            $"Key '{key}' not found in .env. " +
            "Make sure DB_CONNECTION_STRING is set.");
    }

    public static string ConnectionString => Get("DB_CONNECTION_STRING");
}

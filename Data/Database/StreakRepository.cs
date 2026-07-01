using Microsoft.Data.SqlClient;

namespace FitTrack.Database;

public class LoginStreak
{
    public int StreakId       { get; set; }
    public int PersonId       { get; set; }
    public int CurrentStreak  { get; set; }
    public int LongestStreak  { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StreakRepository
{
    public LoginStreak? GetByPersonId(int personId)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        using SqlCommand cmd = new SqlCommand(
            "SELECT StreakId, PersonId, CurrentStreak, LongestStreak, LastLoginDate, UpdatedAt " +
            "FROM LoginStreaks WHERE PersonId = @PersonId", conn);
        cmd.Parameters.AddWithValue("@PersonId", personId);

        using SqlDataReader r = cmd.ExecuteReader();
        if (r.Read()) return Map(r);
        return null;
    }

    public void Upsert(LoginStreak streak)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        string sql = @"
            MERGE LoginStreaks AS target
            USING (SELECT @PersonId AS PersonId) AS source ON target.PersonId = source.PersonId
            WHEN MATCHED THEN
                UPDATE SET CurrentStreak = @Current, LongestStreak = @Longest,
                           LastLoginDate = @LastLogin, UpdatedAt = @Now
            WHEN NOT MATCHED THEN
                INSERT (PersonId, CurrentStreak, LongestStreak, LastLoginDate, UpdatedAt)
                VALUES (@PersonId, @Current, @Longest, @LastLogin, @Now);";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PersonId",  streak.PersonId);
        cmd.Parameters.AddWithValue("@Current",   streak.CurrentStreak);
        cmd.Parameters.AddWithValue("@Longest",   streak.LongestStreak);
        cmd.Parameters.AddWithValue("@LastLogin",
            (object?)streak.LastLoginDate?.Date ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Now", DateTime.Now);
        cmd.ExecuteNonQuery();
    }

    private LoginStreak Map(SqlDataReader r) => new LoginStreak
    {
        StreakId      = Convert.ToInt32(r["StreakId"]),
        PersonId      = Convert.ToInt32(r["PersonId"]),
        CurrentStreak = Convert.ToInt32(r["CurrentStreak"]),
        LongestStreak = Convert.ToInt32(r["LongestStreak"]),
        LastLoginDate = r["LastLoginDate"] == DBNull.Value ? null : Convert.ToDateTime(r["LastLoginDate"]),
        UpdatedAt     = Convert.ToDateTime(r["UpdatedAt"])
    };
}

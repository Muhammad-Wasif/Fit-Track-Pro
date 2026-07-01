using Microsoft.Data.SqlClient;

namespace FitTrack.Database;

public class TrainerAttendanceRecord
{
    public int      AttendanceId { get; set; }
    public int      TrainerId    { get; set; }
    public string   TrainerName  { get; set; } = string.Empty;
    public DateTime AttendDate   { get; set; }
    public string   Status       { get; set; } = string.Empty;
    public string   MarkedBy     { get; set; } = string.Empty;
    public string?  Notes        { get; set; }
}

public class AttendanceRepository
{
    public void Upsert(int trainerId, DateTime date, string status, string markedBy, string? notes)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        string sql = @"
            MERGE TrainerAttendance AS target
            USING (SELECT @TrainerId AS TrainerId, @Date AS AttendDate) AS src
                  ON target.TrainerId = src.TrainerId AND target.AttendDate = src.AttendDate
            WHEN MATCHED THEN
                UPDATE SET Status = @Status, MarkedBy = @MarkedBy, Notes = @Notes
            WHEN NOT MATCHED THEN
                INSERT (TrainerId, AttendDate, Status, MarkedBy, Notes)
                VALUES (@TrainerId, @Date, @Status, @MarkedBy, @Notes);";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TrainerId", trainerId);
        cmd.Parameters.AddWithValue("@Date",      date.Date);
        cmd.Parameters.AddWithValue("@Status",    status);
        cmd.Parameters.AddWithValue("@MarkedBy",  markedBy);
        cmd.Parameters.AddWithValue("@Notes",     (object?)notes ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<TrainerAttendanceRecord> GetByTrainer(int trainerId, int days = 30)
    {
        List<TrainerAttendanceRecord> list = new();

        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        string sql = @"
            SELECT a.AttendanceId, a.TrainerId, p.FullName AS TrainerName,
                   a.AttendDate, a.Status, a.MarkedBy, a.Notes
            FROM TrainerAttendance a
            JOIN Persons p ON a.TrainerId = p.PersonId
            WHERE a.TrainerId = @TrainerId
              AND a.AttendDate >= CAST(DATEADD(DAY, @Days, GETDATE()) AS DATE)
            ORDER BY a.AttendDate DESC";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TrainerId", trainerId);
        cmd.Parameters.AddWithValue("@Days",      -days);

        using SqlDataReader r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<TrainerAttendanceRecord> GetAllForDate(DateTime date)
    {
        List<TrainerAttendanceRecord> list = new();

        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        string sql = @"
            SELECT a.AttendanceId, a.TrainerId, p.FullName AS TrainerName,
                   a.AttendDate, a.Status, a.MarkedBy, a.Notes
            FROM TrainerAttendance a
            JOIN Persons p ON a.TrainerId = p.PersonId
            WHERE a.AttendDate = @Date
            ORDER BY p.FullName";

        using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Date", date.Date);

        using SqlDataReader r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public string? GetStatusForToday(int trainerId)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        using SqlCommand cmd = new SqlCommand(
            "SELECT Status FROM TrainerAttendance WHERE TrainerId=@T AND AttendDate=CAST(GETDATE() AS DATE)",
            conn);
        cmd.Parameters.AddWithValue("@T", trainerId);
        object? res = cmd.ExecuteScalar();
        return res == null || res == DBNull.Value ? null : res.ToString();
    }

    private TrainerAttendanceRecord Map(SqlDataReader r) => new TrainerAttendanceRecord
    {
        AttendanceId = Convert.ToInt32(r["AttendanceId"]),
        TrainerId    = Convert.ToInt32(r["TrainerId"]),
        TrainerName  = r["TrainerName"].ToString()!,
        AttendDate   = Convert.ToDateTime(r["AttendDate"]),
        Status       = r["Status"].ToString()!,
        MarkedBy     = r["MarkedBy"].ToString()!,
        Notes        = r["Notes"] == DBNull.Value ? null : r["Notes"].ToString()
    };
}

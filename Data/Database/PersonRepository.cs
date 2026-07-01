using Microsoft.Data.SqlClient;
using FitTrack.Models;

namespace FitTrack.Database;

public class PersonRepository
{
    // ─── READ ────────────────────────────────────────────────────
    public Person? GetById(int personId)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(SelectBase + " WHERE p.PersonId = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", personId);
        using SqlDataReader r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public Person? GetByUsername(string username)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(SelectBase + " WHERE p.Username = @U", conn);
        cmd.Parameters.AddWithValue("@U", username);
        using SqlDataReader r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public List<Person> GetAll()
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(SelectBase + " ORDER BY p.Role, p.FullName", conn);
        using SqlDataReader r = cmd.ExecuteReader();
        List<Person> list = new();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<Person> GetAllByRole(string role)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(SelectBase + " WHERE p.Role = @Role ORDER BY p.FullName", conn);
        cmd.Parameters.AddWithValue("@Role", role);
        using SqlDataReader r = cmd.ExecuteReader();
        List<Person> list = new();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<Person> GetTraineesByTrainer(int trainerId)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(SelectBase + " WHERE p.TrainerId = @T ORDER BY p.FullName", conn);
        cmd.Parameters.AddWithValue("@T", trainerId);
        using SqlDataReader r = cmd.ExecuteReader();
        List<Person> list = new();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public bool UsernameExists(string username)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(
            "SELECT COUNT(1) FROM Persons WHERE Username=@U", conn);
        cmd.Parameters.AddWithValue("@U", username);
        return (int)cmd.ExecuteScalar()! > 0;
    }

    public bool EmailExists(string email)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(
            "SELECT COUNT(1) FROM Persons WHERE Email=@E", conn);
        cmd.Parameters.AddWithValue("@E", email.ToLower());
        return (int)cmd.ExecuteScalar()! > 0;
    }

    // ─── WRITE ───────────────────────────────────────────────────
    public int Insert(Person p)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        string sql = @"
            INSERT INTO Persons
                (FullName,Username,PasswordHash,Email,Role,Gender,Age,
                 HeightCm,WeightKg,BodyFatPct,GoalId,TrainerId,TrainerLocked,CreatedAt)
            VALUES
                (@FN,@UN,@PH,@EM,@RL,@GN,@AG,
                 @HC,@WK,@BF,@GI,@TI,0,@CA);
            SELECT SCOPE_IDENTITY();";

        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@FN", p.FullName);
        cmd.Parameters.AddWithValue("@UN", p.Username);
        cmd.Parameters.AddWithValue("@PH", p.PasswordHash);
        cmd.Parameters.AddWithValue("@EM", p.Email);
        cmd.Parameters.AddWithValue("@RL", p.Role);
        cmd.Parameters.AddWithValue("@GN", p.Gender);
        cmd.Parameters.AddWithValue("@AG", p.Age);
        cmd.Parameters.AddWithValue("@HC", p.HeightCm);
        cmd.Parameters.AddWithValue("@WK", p.WeightKg);
        cmd.Parameters.AddWithValue("@BF", (object?)p.BodyFatPct ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GI", (object?)p.GoalId    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TI", (object?)p.TrainerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CA", p.CreatedAt);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Update(Person p)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        string sql = @"
            UPDATE Persons SET
                FullName=@FN, Email=@EM, Gender=@GN, Age=@AG,
                HeightCm=@HC, WeightKg=@WK, BodyFatPct=@BF, GoalId=@GI
            WHERE PersonId=@ID";

        using SqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddWithValue("@FN", p.FullName);
        cmd.Parameters.AddWithValue("@EM", p.Email);
        cmd.Parameters.AddWithValue("@GN", p.Gender);
        cmd.Parameters.AddWithValue("@AG", p.Age);
        cmd.Parameters.AddWithValue("@HC", p.HeightCm);
        cmd.Parameters.AddWithValue("@WK", p.WeightKg);
        cmd.Parameters.AddWithValue("@BF", (object?)p.BodyFatPct ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GI", (object?)p.GoalId    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ID", p.PersonId);
        cmd.ExecuteNonQuery();
    }

    public void UpdatePassword(int personId, string hash)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(
            "UPDATE Persons SET PasswordHash=@H WHERE PersonId=@ID", conn);
        cmd.Parameters.AddWithValue("@H",  hash);
        cmd.Parameters.AddWithValue("@ID", personId);
        cmd.ExecuteNonQuery();
    }

    public void UpdateRole(int personId, string role)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(
            "UPDATE Persons SET Role=@R WHERE PersonId=@ID", conn);
        cmd.Parameters.AddWithValue("@R",  role);
        cmd.Parameters.AddWithValue("@ID", personId);
        cmd.ExecuteNonQuery();
    }

    public void UpdateTrainer(int traineeId, int trainerId)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(
            "UPDATE Persons SET TrainerId=@TI WHERE PersonId=@ID", conn);
        cmd.Parameters.AddWithValue("@TI", trainerId);
        cmd.Parameters.AddWithValue("@ID", traineeId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Lock the trainer assignment so trainee cannot change it (spec §9).</summary>
    public void LockTrainer(int traineeId)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(
            "UPDATE Persons SET TrainerLocked=1 WHERE PersonId=@ID", conn);
        cmd.Parameters.AddWithValue("@ID", traineeId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Remove trainer and unlock (called by trainer to remove a trainee — spec §9).</summary>
    public void RemoveTrainer(int traineeId)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(
            "UPDATE Persons SET TrainerId=NULL, TrainerLocked=0 WHERE PersonId=@ID", conn);
        cmd.Parameters.AddWithValue("@ID", traineeId);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int personId)
    {
        using SqlConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        using SqlCommand cmd = new(
            "DELETE FROM Persons WHERE PersonId=@ID", conn);
        cmd.Parameters.AddWithValue("@ID", personId);
        cmd.ExecuteNonQuery();
    }

    // ─── HELPERS ─────────────────────────────────────────────────
    private const string SelectBase = @"
        SELECT p.PersonId, p.FullName, p.Username, p.PasswordHash, p.Email,
               p.Role, p.Gender, p.Age, p.HeightCm, p.WeightKg,
               p.BodyFatPct, p.GoalId, p.TrainerId, p.TrainerLocked, p.CreatedAt,
               g.GoalName, t.FullName AS TrainerName
        FROM Persons p
        LEFT JOIN Goals   g ON p.GoalId    = g.GoalId
        LEFT JOIN Persons t ON p.TrainerId = t.PersonId";

    private Person Map(SqlDataReader r)
    {
        string role = r["Role"].ToString()!;
        Person p = role switch
        {
            "Trainer" => new Trainer(),
            "Admin"   => new Admin(),
            _         => new Trainee()
        };

        p.PersonId      = Convert.ToInt32(r["PersonId"]);
        p.FullName      = r["FullName"].ToString()!;
        p.Username      = r["Username"].ToString()!;
        p.PasswordHash  = r["PasswordHash"].ToString()!;
        p.Email         = r["Email"].ToString()!;
        p.Role          = role;
        p.Gender        = r["Gender"].ToString()!;
        p.Age           = Convert.ToInt32(r["Age"]);
        p.HeightCm      = Convert.ToDouble(r["HeightCm"]);
        p.WeightKg      = Convert.ToDouble(r["WeightKg"]);
        p.BodyFatPct    = r["BodyFatPct"] == DBNull.Value ? null : Convert.ToDouble(r["BodyFatPct"]);
        p.GoalId        = r["GoalId"]     == DBNull.Value ? null : Convert.ToInt32(r["GoalId"]);
        p.TrainerId     = r["TrainerId"]  == DBNull.Value ? null : Convert.ToInt32(r["TrainerId"]);
        p.TrainerLocked = Convert.ToBoolean(r["TrainerLocked"]);
        p.CreatedAt     = Convert.ToDateTime(r["CreatedAt"]);

        if (p is Trainee t)
        {
            t.GoalName    = r["GoalName"]    == DBNull.Value ? null : r["GoalName"].ToString();
            t.TrainerName = r["TrainerName"] == DBNull.Value ? null : r["TrainerName"].ToString();
        }

        return p;
    }
}

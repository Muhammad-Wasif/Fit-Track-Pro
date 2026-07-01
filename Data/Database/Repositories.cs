using Microsoft.Data.SqlClient;
using FitTrack.Models;

namespace FitTrack.Database;

// ================================================================
//  GOAL REPOSITORY
// ================================================================
public class GoalRepository
{
    public List<Goal> GetAll()
    {
        var list = new List<Goal>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand("SELECT GoalId,GoalName,Description,CalorieDelta FROM Goals ORDER BY GoalId", conn);
        using var r    = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public Goal? GetById(int id)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand("SELECT GoalId,GoalName,Description,CalorieDelta FROM Goals WHERE GoalId=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public bool Exists(int id)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand("SELECT COUNT(1) FROM Goals WHERE GoalId=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return (int)cmd.ExecuteScalar()! > 0;
    }

    private Goal Map(SqlDataReader r) => new()
    {
        GoalId       = Convert.ToInt32(r["GoalId"]),
        GoalName     = r["GoalName"].ToString()!,
        Description  = r["Description"].ToString()!,
        CalorieDelta = Convert.ToInt32(r["CalorieDelta"])
    };
}

// ================================================================
//  NUTRITION REPOSITORY
// ================================================================
public class NutritionRepository
{
    private const string FoodSelect = @"
        SELECT f.FoodItemId,f.FoodCategoryId,f.GoalId,f.FoodName,
               f.CaloriesPer100g,f.ProteinPer100g,f.CarbsPer100g,
               f.FatPer100g,f.FiberPer100g,c.CategoryName
        FROM FoodItems f JOIN FoodCategories c ON f.FoodCategoryId=c.FoodCategoryId";

    public List<FoodItem> GetAllFoodItems()
    {
        var list = new List<FoodItem>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(FoodSelect + " ORDER BY c.CategoryName,f.FoodName", conn);
        using var r    = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapFood(r));
        return list;
    }

    public List<FoodItem> SearchFoodItems(string q)
    {
        var list = new List<FoodItem>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(FoodSelect + " WHERE f.FoodName LIKE @Q ORDER BY f.FoodName", conn);
        cmd.Parameters.AddWithValue("@Q", $"%{q}%");
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapFood(r));
        return list;
    }

    public FoodItem? GetFoodItemById(int id)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(FoodSelect + " WHERE f.FoodItemId=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapFood(r) : null;
    }

    public int InsertNutritionLog(NutritionLog log)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"INSERT INTO NutritionLogs
            (PersonId,FoodItemId,MealType,ServingGrams,Calories,ProteinG,CarbsG,FatG,LoggedAt,LoggedByPersonId)
            VALUES(@PI,@FI,@MT,@SG,@CA,@PR,@CB,@FA,@LA,@LB); SELECT SCOPE_IDENTITY();";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", log.PersonId);
        cmd.Parameters.AddWithValue("@FI", log.FoodItemId);
        cmd.Parameters.AddWithValue("@MT", log.MealType);
        cmd.Parameters.AddWithValue("@SG", log.ServingGrams);
        cmd.Parameters.AddWithValue("@CA", log.Calories);
        cmd.Parameters.AddWithValue("@PR", log.ProteinG);
        cmd.Parameters.AddWithValue("@CB", log.CarbsG);
        cmd.Parameters.AddWithValue("@FA", log.FatG);
        cmd.Parameters.AddWithValue("@LA", log.LoggedAt);
        cmd.Parameters.AddWithValue("@LB", (object?)log.LoggedByPersonId ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<NutritionLog> GetLogsForDay(int personId, DateTime date)
    {
        var list = new List<NutritionLog>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"SELECT nl.*,f.FoodName FROM NutritionLogs nl
            JOIN FoodItems f ON nl.FoodItemId=f.FoodItemId
            WHERE nl.PersonId=@PI AND CAST(nl.LoggedAt AS DATE)=@D ORDER BY nl.LoggedAt";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        cmd.Parameters.AddWithValue("@D",  date.Date);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapLog(r));
        return list;
    }

    public List<NutritionLog> GetRecentLogs(int personId, int days)
    {
        var list = new List<NutritionLog>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"SELECT nl.*,f.FoodName FROM NutritionLogs nl
            JOIN FoodItems f ON nl.FoodItemId=f.FoodItemId
            WHERE nl.PersonId=@PI AND nl.LoggedAt>=DATEADD(DAY,@D,GETDATE())
            ORDER BY nl.LoggedAt DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        cmd.Parameters.AddWithValue("@D",  -days);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapLog(r));
        return list;
    }

    public void DeleteLog(int id)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand("DELETE FROM NutritionLogs WHERE NutritionLogId=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    private FoodItem MapFood(SqlDataReader r) => new()
    {
        FoodItemId      = Convert.ToInt32(r["FoodItemId"]),
        FoodCategoryId  = Convert.ToInt32(r["FoodCategoryId"]),
        GoalId          = r["GoalId"] == DBNull.Value ? null : Convert.ToInt32(r["GoalId"]),
        CategoryName    = r["CategoryName"].ToString()!,
        FoodName        = r["FoodName"].ToString()!,
        CaloriesPer100g = Convert.ToDouble(r["CaloriesPer100g"]),
        ProteinPer100g  = Convert.ToDouble(r["ProteinPer100g"]),
        CarbsPer100g    = Convert.ToDouble(r["CarbsPer100g"]),
        FatPer100g      = Convert.ToDouble(r["FatPer100g"]),
        FiberPer100g    = r["FiberPer100g"] == DBNull.Value ? null : Convert.ToDouble(r["FiberPer100g"])
    };

    private NutritionLog MapLog(SqlDataReader r) => new()
    {
        NutritionLogId   = Convert.ToInt32(r["NutritionLogId"]),
        PersonId         = Convert.ToInt32(r["PersonId"]),
        FoodItemId       = Convert.ToInt32(r["FoodItemId"]),
        FoodName         = r["FoodName"].ToString()!,
        MealType         = r["MealType"].ToString()!,
        ServingGrams     = Convert.ToDouble(r["ServingGrams"]),
        Calories         = Convert.ToDouble(r["Calories"]),
        ProteinG         = Convert.ToDouble(r["ProteinG"]),
        CarbsG           = Convert.ToDouble(r["CarbsG"]),
        FatG             = Convert.ToDouble(r["FatG"]),
        LoggedAt         = Convert.ToDateTime(r["LoggedAt"]),
        LoggedByPersonId = r["LoggedByPersonId"] == DBNull.Value ? null : Convert.ToInt32(r["LoggedByPersonId"])
    };

    // Archive today's logs to NutritionHistory, then delete them.
    // Only archives if today hasn't been archived yet.
    public void ArchiveTodayAndReset(int personId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();

        // Skip if already archived today
        using var chk = new SqlCommand(
            "SELECT COUNT(1) FROM NutritionHistory WHERE PersonId=@PI AND ArchiveDate=CAST(GETDATE()-1 AS DATE)", conn);
        chk.Parameters.AddWithValue("@PI", personId);
        if (Convert.ToInt32(chk.ExecuteScalar()) > 0) return;

        // Aggregate yesterday's logs
        using var agg = new SqlCommand(@"
            INSERT INTO NutritionHistory (PersonId, ArchiveDate, TotalCalories, TotalProteinG, TotalCarbsG, TotalFatG)
            SELECT PersonId,
                   CAST(CAST(GETDATE()-1 AS DATE) AS DATETIME),
                   SUM(Calories), SUM(ProteinG), SUM(CarbsG), SUM(FatG)
            FROM NutritionLogs
            WHERE PersonId=@PI AND CAST(LoggedAt AS DATE)=CAST(GETDATE()-1 AS DATE)
            GROUP BY PersonId
            HAVING COUNT(*)>0;

            DELETE FROM NutritionLogs
            WHERE PersonId=@PI2 AND CAST(LoggedAt AS DATE)=CAST(GETDATE()-1 AS DATE);",
            conn);
        agg.Parameters.AddWithValue("@PI",  personId);
        agg.Parameters.AddWithValue("@PI2", personId);
        agg.ExecuteNonQuery();
    }

    public List<NutritionHistory> GetHistory(int personId, int days = 30)
    {
        var list = new List<NutritionHistory>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(@"
            SELECT * FROM NutritionHistory
            WHERE PersonId=@PI AND ArchiveDate >= CAST(DATEADD(DAY,-@D,GETDATE()) AS DATE)
            ORDER BY ArchiveDate DESC", conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        cmd.Parameters.AddWithValue("@D",  days);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new NutritionHistory
            {
                HistoryId     = Convert.ToInt32(r["HistoryId"]),
                PersonId      = Convert.ToInt32(r["PersonId"]),
                ArchiveDate   = Convert.ToDateTime(r["ArchiveDate"]),
                TotalCalories = Convert.ToDouble(r["TotalCalories"]),
                TotalProteinG = Convert.ToDouble(r["TotalProteinG"]),
                TotalCarbsG   = Convert.ToDouble(r["TotalCarbsG"]),
                TotalFatG     = Convert.ToDouble(r["TotalFatG"])
            });
        return list;
    }
}

// ================================================================
//  PROGRESS REPOSITORY
// ================================================================
public class ProgressRepository
{
    public int InsertSnapshot(ProgressSnapshot s)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"INSERT INTO ProgressSnapshots(PersonId,SnapshotDate,WeightKg,BodyFatPct,BMI,Notes)
            VALUES(@PI,@SD,@WK,@BF,@BM,@NO); SELECT SCOPE_IDENTITY();";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", s.PersonId);
        cmd.Parameters.AddWithValue("@SD", s.SnapshotDate);
        cmd.Parameters.AddWithValue("@WK", s.WeightKg);
        cmd.Parameters.AddWithValue("@BF", (object?)s.BodyFatPct ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BM", s.BMI);
        cmd.Parameters.AddWithValue("@NO", (object?)s.Notes     ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<ProgressSnapshot> GetSnapshots(int personId, int take)
    {
        var list = new List<ProgressSnapshot>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = $"SELECT TOP {take} SnapshotId,PersonId,SnapshotDate,WeightKg,BodyFatPct,BMI,Notes " +
                     "FROM ProgressSnapshots WHERE PersonId=@PI ORDER BY SnapshotDate DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new ProgressSnapshot
        {
            SnapshotId   = Convert.ToInt32(r["SnapshotId"]),
            PersonId     = Convert.ToInt32(r["PersonId"]),
            SnapshotDate = Convert.ToDateTime(r["SnapshotDate"]),
            WeightKg     = Convert.ToDouble(r["WeightKg"]),
            BodyFatPct   = r["BodyFatPct"] == DBNull.Value ? null : Convert.ToDouble(r["BodyFatPct"]),
            BMI          = Convert.ToDouble(r["BMI"]),
            Notes        = r["Notes"] == DBNull.Value ? null : r["Notes"].ToString()
        });
        return list;
    }

    public void DeleteSnapshot(int id)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand("DELETE FROM ProgressSnapshots WHERE SnapshotId=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }
}

// ================================================================
//  EXERCISE REPOSITORY
// ================================================================
public class ExerciseRepository
{
    private const string ExSel = @"
        SELECT e.ExerciseId,e.CategoryId,e.GoalId,e.Name,e.MuscleGroup,
               e.Equipment,e.DefaultSets,e.DefaultReps,e.DefaultSecs,
               e.METValue,e.Description,c.CategoryName
        FROM Exercises e JOIN ExerciseCategories c ON e.CategoryId=c.CategoryId";

    public List<Exercise> GetAll()
    {
        var list = new List<Exercise>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(ExSel + " ORDER BY c.CategoryName,e.Name", conn);
        using var r    = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public List<Exercise> GetByCategory(int catId)
    {
        var list = new List<Exercise>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(ExSel + " WHERE e.CategoryId=@C ORDER BY e.Name", conn);
        cmd.Parameters.AddWithValue("@C", catId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public Exercise? GetById(int id)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(ExSel + " WHERE e.ExerciseId=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public List<ExerciseCategory> GetAllCategories()
    {
        var list = new List<ExerciseCategory>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand("SELECT CategoryId,CategoryName FROM ExerciseCategories ORDER BY CategoryName", conn);
        using var r    = cmd.ExecuteReader();
        while (r.Read()) list.Add(new ExerciseCategory
        {
            CategoryId   = Convert.ToInt32(r["CategoryId"]),
            CategoryName = r["CategoryName"].ToString()!
        });
        return list;
    }

    private Exercise Map(SqlDataReader r) => new()
    {
        ExerciseId   = Convert.ToInt32(r["ExerciseId"]),
        CategoryId   = Convert.ToInt32(r["CategoryId"]),
        GoalId       = r["GoalId"] == DBNull.Value ? null : Convert.ToInt32(r["GoalId"]),
        CategoryName = r["CategoryName"].ToString()!,
        Name         = r["Name"].ToString()!,
        MuscleGroup  = r["MuscleGroup"].ToString()!,
        Equipment    = r["Equipment"].ToString()!,
        DefaultSets  = Convert.ToInt32(r["DefaultSets"]),
        DefaultReps  = r["DefaultReps"] == DBNull.Value ? null : Convert.ToInt32(r["DefaultReps"]),
        DefaultSecs  = r["DefaultSecs"] == DBNull.Value ? null : Convert.ToInt32(r["DefaultSecs"]),
        METValue     = Convert.ToDouble(r["METValue"]),
        Description  = r["Description"] == DBNull.Value ? null : r["Description"].ToString()
    };
}

// ================================================================
//  WORKOUT REPOSITORY
// ================================================================
public class WorkoutRepository
{
    public int InsertPlan(WorkoutPlan p)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"INSERT INTO WorkoutPlans
            (CreatedByPersonId,AssignedToPersonId,GoalId,PlanName,DurationWeeks,IsActive,CreatedAt)
            VALUES(@CB,@AT,@GI,@PN,@DW,1,@CA); SELECT SCOPE_IDENTITY();";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@CB", p.CreatedByPersonId);
        cmd.Parameters.AddWithValue("@AT", (object?)p.AssignedToPersonId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GI", p.GoalId);
        cmd.Parameters.AddWithValue("@PN", p.PlanName);
        cmd.Parameters.AddWithValue("@DW", p.DurationWeeks);
        cmd.Parameters.AddWithValue("@CA", DateTime.Now);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void InsertPlanExercise(WorkoutPlanExercise pe)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"INSERT INTO WorkoutPlanExercises
            (PlanId,ExerciseId,DayOfWeek,OrderInDay,Sets,Reps,Seconds,RestSeconds)
            VALUES(@PI,@EI,@DW,@OI,@SE,@RE,@SC,@RS)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", pe.PlanId);
        cmd.Parameters.AddWithValue("@EI", pe.ExerciseId);
        cmd.Parameters.AddWithValue("@DW", pe.DayOfWeek);
        cmd.Parameters.AddWithValue("@OI", pe.OrderInDay);
        cmd.Parameters.AddWithValue("@SE", pe.Sets);
        cmd.Parameters.AddWithValue("@RE", (object?)pe.Reps    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SC", (object?)pe.Seconds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RS", pe.RestSeconds);
        cmd.ExecuteNonQuery();
    }

    public void DeactivateAllPlansForPerson(int personId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(
            "UPDATE WorkoutPlans SET IsActive=0 WHERE AssignedToPersonId=@PI OR (AssignedToPersonId IS NULL AND CreatedByPersonId=@PI)", conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        cmd.ExecuteNonQuery();
    }

    public void DeletePlan(int planId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand("DELETE FROM WorkoutPlans WHERE PlanId=@Id", conn);
        cmd.Parameters.AddWithValue("@Id", planId);
        cmd.ExecuteNonQuery();
    }

    public WorkoutPlan? GetActivePlan(int personId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"SELECT TOP 1 wp.*,g.GoalName FROM WorkoutPlans wp
            JOIN Goals g ON wp.GoalId=g.GoalId
            WHERE (wp.AssignedToPersonId=@PI OR (wp.AssignedToPersonId IS NULL AND wp.CreatedByPersonId=@PI))
              AND wp.IsActive=1 ORDER BY wp.CreatedAt DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var plan = MapPlan(r); r.Close();
        plan.Exercises = GetPlanExercises(plan.PlanId);
        return plan;
    }

    public List<WorkoutPlan> GetAllPlansForPerson(int personId)
    {
        var list = new List<WorkoutPlan>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"SELECT wp.*,g.GoalName FROM WorkoutPlans wp
            JOIN Goals g ON wp.GoalId=g.GoalId
            WHERE wp.AssignedToPersonId=@PI OR (wp.AssignedToPersonId IS NULL AND wp.CreatedByPersonId=@PI)
            ORDER BY wp.CreatedAt DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapPlan(r));
        return list;
    }

    public List<WorkoutSession> GetSessionHistory(int personId, int take)
    {
        var list = new List<WorkoutSession>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = $"SELECT TOP {take} SessionId,PersonId,PlanId,SessionDate,DurationMinutes,TotalCalories,Notes " +
                     "FROM WorkoutSessions WHERE PersonId=@PI ORDER BY SessionDate DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new WorkoutSession
        {
            SessionId       = Convert.ToInt32(r["SessionId"]),
            PersonId        = Convert.ToInt32(r["PersonId"]),
            PlanId          = r["PlanId"] == DBNull.Value ? null : Convert.ToInt32(r["PlanId"]),
            SessionDate     = Convert.ToDateTime(r["SessionDate"]),
            DurationMinutes = Convert.ToInt32(r["DurationMinutes"]),
            TotalCalories   = Convert.ToDouble(r["TotalCalories"]),
            Notes           = r["Notes"] == DBNull.Value ? null : r["Notes"].ToString()
        });
        return list;
    }

    public int InsertSession(WorkoutSession session)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"INSERT INTO WorkoutSessions (PersonId, PlanId, SessionDate, DurationMinutes, TotalCalories, Notes)
            VALUES (@PersonId, @PlanId, @SessionDate, @DurationMinutes, @TotalCalories, @Notes);
            SELECT SCOPE_IDENTITY();";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PersonId", session.PersonId);
        cmd.Parameters.AddWithValue("@PlanId", (object?)session.PlanId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SessionDate", session.SessionDate);
        cmd.Parameters.AddWithValue("@DurationMinutes", session.DurationMinutes);
        cmd.Parameters.AddWithValue("@TotalCalories", session.TotalCalories);
        cmd.Parameters.AddWithValue("@Notes", (object?)session.Notes ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void InsertSessionLog(SessionLog log)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"INSERT INTO SessionLogs (SessionId, ExerciseId, SetNumber, ActualReps, ActualSeconds, WeightKg, CaloriesBurned)
            VALUES (@SessionId, @ExerciseId, @SetNumber, @ActualReps, @ActualSeconds, @WeightKg, @CaloriesBurned)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", log.SessionId);
        cmd.Parameters.AddWithValue("@ExerciseId", log.ExerciseId);
        cmd.Parameters.AddWithValue("@SetNumber", log.SetNumber);
        cmd.Parameters.AddWithValue("@ActualReps", (object?)log.ActualReps ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ActualSeconds", (object?)log.ActualSeconds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@WeightKg", (object?)log.WeightKg ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CaloriesBurned", log.CaloriesBurned);
        cmd.ExecuteNonQuery();
    }

    public void UpdateSessionCalories(int sessionId, double totalCalories)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd = new SqlCommand("UPDATE WorkoutSessions SET TotalCalories = @Cal WHERE SessionId = @SessionId", conn);
        cmd.Parameters.AddWithValue("@Cal", totalCalories);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        cmd.ExecuteNonQuery();
    }

    public WorkoutSession? GetSessionById(int sessionId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = "SELECT SessionId, PersonId, PlanId, SessionDate, DurationMinutes, TotalCalories, Notes FROM WorkoutSessions WHERE SessionId = @SessionId";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var session = new WorkoutSession
        {
            SessionId       = Convert.ToInt32(r["SessionId"]),
            PersonId        = Convert.ToInt32(r["PersonId"]),
            PlanId          = r["PlanId"] == DBNull.Value ? null : Convert.ToInt32(r["PlanId"]),
            SessionDate     = Convert.ToDateTime(r["SessionDate"]),
            DurationMinutes = Convert.ToInt32(r["DurationMinutes"]),
            TotalCalories   = Convert.ToDouble(r["TotalCalories"]),
            Notes           = r["Notes"] == DBNull.Value ? null : r["Notes"].ToString()
        };
        r.Close();
        session.Logs = GetSessionLogs(sessionId);
        return session;
    }

    public List<SessionLog> GetSessionLogs(int sessionId)
    {
        var list = new List<SessionLog>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"SELECT sl.LogId, sl.SessionId, sl.ExerciseId, sl.SetNumber,
                   sl.ActualReps, sl.ActualSeconds, sl.WeightKg, sl.CaloriesBurned,
                   e.Name AS ExerciseName
            FROM SessionLogs sl
            JOIN Exercises e ON sl.ExerciseId = e.ExerciseId
            WHERE sl.SessionId = @SessionId
            ORDER BY sl.SetNumber";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new SessionLog
            {
                LogId          = Convert.ToInt32(r["LogId"]),
                SessionId      = Convert.ToInt32(r["SessionId"]),
                ExerciseId     = Convert.ToInt32(r["ExerciseId"]),
                ExerciseName   = r["ExerciseName"].ToString()!,
                SetNumber      = Convert.ToInt32(r["SetNumber"]),
                ActualReps     = r["ActualReps"] == DBNull.Value ? null : Convert.ToInt32(r["ActualReps"]),
                ActualSeconds  = r["ActualSeconds"] == DBNull.Value ? null : Convert.ToInt32(r["ActualSeconds"]),
                WeightKg       = r["WeightKg"] == DBNull.Value ? null : Convert.ToDouble(r["WeightKg"]),
                CaloriesBurned = Convert.ToDouble(r["CaloriesBurned"])
            });
        }
        return list;
    }

    private List<WorkoutPlanExercise> GetPlanExercises(int planId)
    {
        var list = new List<WorkoutPlanExercise>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"SELECT pe.*,e.Name,e.MuscleGroup FROM WorkoutPlanExercises pe
            JOIN Exercises e ON pe.ExerciseId=e.ExerciseId
            WHERE pe.PlanId=@PI ORDER BY pe.DayOfWeek,pe.OrderInDay";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", planId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new WorkoutPlanExercise
        {
            PlanExerciseId = Convert.ToInt32(r["PlanExerciseId"]),
            PlanId         = Convert.ToInt32(r["PlanId"]),
            ExerciseId     = Convert.ToInt32(r["ExerciseId"]),
            ExerciseName   = r["Name"].ToString()!,
            MuscleGroup    = r["MuscleGroup"].ToString()!,
            DayOfWeek      = Convert.ToInt32(r["DayOfWeek"]),
            OrderInDay     = Convert.ToInt32(r["OrderInDay"]),
            Sets           = Convert.ToInt32(r["Sets"]),
            Reps           = r["Reps"]       == DBNull.Value ? null : Convert.ToInt32(r["Reps"]),
            Seconds        = r["Seconds"]    == DBNull.Value ? null : Convert.ToInt32(r["Seconds"]),
            RestSeconds    = Convert.ToInt32(r["RestSeconds"]),
            IsUserAdded    = r["IsUserAdded"] != DBNull.Value && Convert.ToBoolean(r["IsUserAdded"])
        });
        return list;
    }

    /// <summary>Append a new exercise to an existing plan, marked as user-added (not locked).</summary>
    public int AddExerciseToPlan(WorkoutPlanExercise pe)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        string sql = @"INSERT INTO WorkoutPlanExercises
            (PlanId,ExerciseId,DayOfWeek,OrderInDay,Sets,Reps,Seconds,RestSeconds,IsUserAdded)
            VALUES(@PI,@EI,@DW,@OI,@SE,@RE,@SC,@RS,1);
            SELECT SCOPE_IDENTITY();";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", pe.PlanId);
        cmd.Parameters.AddWithValue("@EI", pe.ExerciseId);
        cmd.Parameters.AddWithValue("@DW", pe.DayOfWeek);
        cmd.Parameters.AddWithValue("@OI", pe.OrderInDay);
        cmd.Parameters.AddWithValue("@SE", pe.Sets);
        cmd.Parameters.AddWithValue("@RE", (object?)pe.Reps    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SC", (object?)pe.Seconds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RS", pe.RestSeconds);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Deactivate plans whose computed EndsAt date has passed.</summary>
    public void DeactivateExpiredPlans(int personId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(
            @"UPDATE WorkoutPlans SET IsActive=0
              WHERE IsActive=1
                AND EndsAt < GETDATE()
                AND (AssignedToPersonId=@PI OR (AssignedToPersonId IS NULL AND CreatedByPersonId=@PI))", conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Returns the EndsAt date of the active plan, or null.</summary>
    public DateTime? GetPlanEndsAt(int personId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(
            @"SELECT TOP 1 EndsAt FROM WorkoutPlans
              WHERE (AssignedToPersonId=@PI OR (AssignedToPersonId IS NULL AND CreatedByPersonId=@PI))
                AND IsActive=1 ORDER BY CreatedAt DESC", conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(result);
    }

    /// <summary>Returns the next available OrderInDay for a given plan+day.</summary>
    public int GetNextOrderInDay(int planId, int dayOfWeek)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(
            "SELECT ISNULL(MAX(OrderInDay),0)+1 FROM WorkoutPlanExercises WHERE PlanId=@PI AND DayOfWeek=@DW", conn);
        cmd.Parameters.AddWithValue("@PI", planId);
        cmd.Parameters.AddWithValue("@DW", dayOfWeek);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }


    private WorkoutPlan MapPlan(SqlDataReader r) => new()
    {
        PlanId             = Convert.ToInt32(r["PlanId"]),
        CreatedByPersonId  = Convert.ToInt32(r["CreatedByPersonId"]),
        AssignedToPersonId = r["AssignedToPersonId"] == DBNull.Value ? null : Convert.ToInt32(r["AssignedToPersonId"]),
        GoalId             = Convert.ToInt32(r["GoalId"]),
        GoalName           = r["GoalName"].ToString()!,
        PlanName           = r["PlanName"].ToString()!,
        DurationWeeks      = Convert.ToInt32(r["DurationWeeks"]),
        IsActive           = Convert.ToBoolean(r["IsActive"]),
        CreatedAt          = Convert.ToDateTime(r["CreatedAt"])
    };
}

// ================================================================
//  DAILY CONFIRMATION REPOSITORY
// ================================================================
public class DailyConfirmationRepository
{
    /// <summary>Insert a daily confirmation; ignores duplicates (same person+exercise+date).</summary>
    public void Confirm(int personId, int planExerciseId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        // Use MERGE to handle the UNIQUE constraint gracefully
        string sql = @"
            IF NOT EXISTS (
                SELECT 1 FROM DailySessionConfirmations
                WHERE PersonId=@PI AND PlanExerciseId=@PE AND ConfirmedDate=CAST(GETDATE() AS DATE)
            )
            INSERT INTO DailySessionConfirmations (PersonId, PlanExerciseId, ConfirmedDate)
            VALUES (@PI, @PE, CAST(GETDATE() AS DATE))";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        cmd.Parameters.AddWithValue("@PE", planExerciseId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Returns all PlanExerciseIds confirmed by this person today.</summary>
    public HashSet<int> GetConfirmedToday(int personId)
    {
        var set = new HashSet<int>();
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(
            "SELECT PlanExerciseId FROM DailySessionConfirmations WHERE PersonId=@PI AND ConfirmedDate=CAST(GETDATE() AS DATE)", conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) set.Add(Convert.ToInt32(r["PlanExerciseId"]));
        return set;
    }

    /// <summary>Check if a single plan exercise is confirmed today.</summary>
    public bool IsConfirmedToday(int personId, int planExerciseId)
    {
        using var conn = DatabaseHelper.GetConnection(); conn.Open();
        using var cmd  = new SqlCommand(
            "SELECT COUNT(1) FROM DailySessionConfirmations WHERE PersonId=@PI AND PlanExerciseId=@PE AND ConfirmedDate=CAST(GETDATE() AS DATE)", conn);
        cmd.Parameters.AddWithValue("@PI", personId);
        cmd.Parameters.AddWithValue("@PE", planExerciseId);
        return (int)cmd.ExecuteScalar()! > 0;
    }
}

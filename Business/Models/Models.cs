// ── Admin.cs ──────────────────────────────────────────────────
namespace FitTrack.Models;

public class Admin : Person
{
    public Admin() { Role = "Admin"; }
    public override string GetRoleLabel() => "Admin";
    public override string ToString() => base.ToString() + " | Full System Access";
}

// ── Trainer.cs ────────────────────────────────────────────────
public class Trainer : Person
{
    public List<Trainee> Trainees { get; set; } = new();
    public Trainer() { Role = "Trainer"; }
    public override string GetRoleLabel() => "Trainer";
    public override string ToString() => base.ToString() + $" | Clients: {Trainees.Count}";
}

// ── Trainee.cs ────────────────────────────────────────────────
public class Trainee : Person
{
    public string? GoalName    { get; set; }
    public string? TrainerName { get; set; }
    public Trainee() { Role = "Trainee"; }
    public override string GetRoleLabel() => "Trainee";
    public override string ToString()
        => base.ToString() + $" | Goal: {GoalName ?? "None"} | Trainer: {TrainerName ?? "None"}";
}

// ── Goal.cs ───────────────────────────────────────────────────
public class Goal
{
    public int    GoalId       { get; set; }
    public string GoalName     { get; set; } = string.Empty;
    public string Description  { get; set; } = string.Empty;
    public int    CalorieDelta { get; set; }
    public override string ToString()
    {
        string dir = CalorieDelta >= 0 ? $"+{CalorieDelta}" : $"{CalorieDelta}";
        return $"{GoalName} ({dir} kcal)";
    }
}

// ── FoodItem.cs ───────────────────────────────────────────────
public class FoodCategory
{
    public int    FoodCategoryId { get; set; }
    public string CategoryName   { get; set; } = string.Empty;
    public override string ToString() => CategoryName;
}

public class FoodItem
{
    public int     FoodItemId      { get; set; }
    public int     FoodCategoryId  { get; set; }
    public int?    GoalId          { get; set; }
    public string  CategoryName    { get; set; } = string.Empty;
    public string  FoodName        { get; set; } = string.Empty;
    public double  CaloriesPer100g { get; set; }
    public double  ProteinPer100g  { get; set; }
    public double  CarbsPer100g    { get; set; }
    public double  FatPer100g      { get; set; }
    public double? FiberPer100g    { get; set; }

    public double CalculateCalories(double g) => Math.Round(CaloriesPer100g * g / 100.0, 2);
    public double CalculateProtein(double g)  => Math.Round(ProteinPer100g  * g / 100.0, 2);
    public double CalculateCarbs(double g)    => Math.Round(CarbsPer100g    * g / 100.0, 2);
    public double CalculateFat(double g)      => Math.Round(FatPer100g      * g / 100.0, 2);
    public override string ToString() => $"{FoodName} | {CaloriesPer100g} kcal/100g";
}

// ── NutritionLog.cs ───────────────────────────────────────────
public class NutritionLog
{
    public int      NutritionLogId { get; set; }
    public int      PersonId       { get; set; }
    public int      FoodItemId     { get; set; }
    public string   FoodName       { get; set; } = string.Empty;
    public string   MealType       { get; set; } = string.Empty;
    public double   ServingGrams   { get; set; }
    public double   Calories       { get; set; }
    public double   ProteinG       { get; set; }
    public double   CarbsG         { get; set; }
    public double   FatG           { get; set; }
    public DateTime LoggedAt       { get; set; }
    /// <summary>Who added this log entry (trainee or trainer PersonId).</summary>
    public int?     LoggedByPersonId { get; set; }
}

// ── NutritionHistory.cs ─────────────────────────────────────────────
public class NutritionHistory
{
    public int      HistoryId     { get; set; }
    public int      PersonId      { get; set; }
    public DateTime ArchiveDate   { get; set; }
    public double   TotalCalories { get; set; }
    public double   TotalProteinG { get; set; }
    public double   TotalCarbsG   { get; set; }
    public double   TotalFatG     { get; set; }
    public string?  MealsJson     { get; set; }
}

// ── Exercise.cs ───────────────────────────────────────────────
public class ExerciseCategory
{
    public int    CategoryId   { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public override string ToString() => CategoryName;
}

public class Exercise
{
    public int     ExerciseId   { get; set; }
    public int     CategoryId   { get; set; }
    public int?    GoalId       { get; set; }
    public string  CategoryName { get; set; } = string.Empty;
    public string  Name         { get; set; } = string.Empty;
    public string  MuscleGroup  { get; set; } = string.Empty;
    public string  Equipment    { get; set; } = string.Empty;
    public int     DefaultSets  { get; set; }
    public int?    DefaultReps  { get; set; }
    public int?    DefaultSecs  { get; set; }
    public double  METValue     { get; set; }
    public string? Description  { get; set; }

    public double CalculateCaloriesBurned(double weightKg, int durationSecs)
        => Math.Round(METValue * weightKg * (durationSecs / 3600.0), 2);

    public override string ToString() => $"{Name} | {MuscleGroup} | {Equipment}";
}

// ── WorkoutPlan.cs ────────────────────────────────────────────
public class WorkoutPlanExercise
{
    public int    PlanExerciseId { get; set; }
    public int    PlanId         { get; set; }
    public int    ExerciseId     { get; set; }
    public string ExerciseName   { get; set; } = string.Empty;
    public string MuscleGroup    { get; set; } = string.Empty;
    public int    DayOfWeek      { get; set; }
    public int    OrderInDay     { get; set; }
    public int    Sets           { get; set; }
    public int?   Reps           { get; set; }
    public int?   Seconds        { get; set; }
    public int    RestSeconds    { get; set; }
    /// <summary>True if this exercise was added after the initial plan creation (not locked).</summary>
    public bool   IsUserAdded    { get; set; }
    public override string ToString()
    {
        string vol = Reps.HasValue ? $"{Sets}×{Reps}" : $"{Sets}×{Seconds}s";
        return $"Day {DayOfWeek} #{OrderInDay}: {ExerciseName} {vol} | Rest {RestSeconds}s";
    }
}


public class WorkoutPlan
{
    public int      PlanId             { get; set; }
    public int      CreatedByPersonId  { get; set; }
    public int?     AssignedToPersonId { get; set; }
    public int      GoalId             { get; set; }
    public string   GoalName           { get; set; } = string.Empty;
    public string   PlanName           { get; set; } = string.Empty;
    public int      DurationWeeks      { get; set; }
    public bool     IsActive           { get; set; }
    public DateTime CreatedAt          { get; set; }
    public List<WorkoutPlanExercise> Exercises { get; set; } = new();
    public override string ToString() => $"{PlanName} | {GoalName} | {DurationWeeks}w";
}

// ── WorkoutSession.cs ─────────────────────────────────────────
public class SessionLog
{
    public int    LogId          { get; set; }
    public int    SessionId      { get; set; }
    public int    ExerciseId     { get; set; }
    public string ExerciseName   { get; set; } = string.Empty;
    public int    SetNumber      { get; set; }
    public int?   ActualReps     { get; set; }
    public int?   ActualSeconds  { get; set; }
    public double? WeightKg      { get; set; }
    public double CaloriesBurned { get; set; }
}

public class WorkoutSession
{
    public int      SessionId       { get; set; }
    public int      PersonId        { get; set; }
    public int?     PlanId          { get; set; }
    public DateTime SessionDate     { get; set; }
    public int      DurationMinutes { get; set; }
    public double   TotalCalories   { get; set; }
    public string?  Notes           { get; set; }
    public List<SessionLog> Logs    { get; set; } = new();
}

// ── ProgressSnapshot.cs ───────────────────────────────────────
public class ProgressSnapshot
{
    public int      SnapshotId   { get; set; }
    public int      PersonId     { get; set; }
    public DateTime SnapshotDate { get; set; }
    public double   WeightKg     { get; set; }
    public double?  BodyFatPct   { get; set; }
    public double   BMI          { get; set; }
    public string?  Notes        { get; set; }
}

// ── DailySessionConfirmation.cs ──────────────────────────────
/// <summary>
/// Lightweight record that a trainee confirmed completing a specific
/// plan exercise on a given day (separate from the full WorkoutSession log).
/// </summary>
public class DailySessionConfirmation
{
    public int      ConfirmationId  { get; set; }
    public int      PersonId        { get; set; }
    public int      PlanExerciseId  { get; set; }
    public DateTime ConfirmedDate   { get; set; }
}


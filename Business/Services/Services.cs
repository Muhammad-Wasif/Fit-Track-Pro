using FitTrack.Database;
using FitTrack.Models;

namespace FitTrack.Services;

// ================================================================
//  GOAL SERVICE
// ================================================================
public class GoalService
{
    private readonly GoalRepository   _goalRepo   = new();
    private readonly PersonRepository _personRepo = new();

    public List<Goal> GetAll() => _goalRepo.GetAll();
    public Goal? GetById(int id) => _goalRepo.GetById(id);

    public (double bmi, string cat, double bmr, double tdee, double target)
        GetMetrics(int personId, double activityMultiplier = 1.55)
    {
        Person? p = _personRepo.GetById(personId);
        if (p == null) return (0, "Unknown", 0, 0, 0);

        double bmi  = p.CalculateBMI();
        string cat  = p.GetBMICategory();
        double bmr  = p.CalculateBMR();
        double tdee = p.CalculateTDEE(activityMultiplier);
        double target = tdee;

        if (p.GoalId.HasValue)
        {
            Goal? g = _goalRepo.GetById(p.GoalId.Value);
            if (g != null) target = tdee + g.CalorieDelta;
        }
        return (bmi, cat, bmr, tdee, target);
    }
}

// ================================================================
//  NUTRITION SERVICE
// ================================================================
public class NutritionService
{
    private readonly NutritionRepository _repo       = new();
    private readonly PersonRepository    _personRepo = new();
    private readonly GoalRepository      _goalRepo   = new();

    public List<FoodItem> GetAll()                    => _repo.GetAllFoodItems();
    public List<FoodItem> Search(string q)             => _repo.SearchFoodItems(q);
    public FoodItem? GetById(int id)                   => _repo.GetFoodItemById(id);

    public (bool ok, string msg, NutritionLog? log) LogMeal(
        int personId, int foodItemId, string mealType, double servingGrams,
        int? loggedByPersonId = null)
    {
        if (servingGrams <= 0)       return (false, "Serving grams must be > 0.", null);
        Person?   p = _personRepo.GetById(personId);
        if (p == null)               return (false, "User not found.", null);
        FoodItem? f = _repo.GetFoodItemById(foodItemId);
        if (f == null)               return (false, "Food item not found.", null);

        NutritionLog log = new()
        {
            PersonId         = personId,
            FoodItemId       = foodItemId,
            FoodName         = f.FoodName,
            MealType         = mealType,
            ServingGrams     = servingGrams,
            Calories         = f.CalculateCalories(servingGrams),
            ProteinG         = f.CalculateProtein(servingGrams),
            CarbsG           = f.CalculateCarbs(servingGrams),
            FatG             = f.CalculateFat(servingGrams),
            LoggedAt         = DateTime.Now,
            LoggedByPersonId = loggedByPersonId ?? personId
        };

        int id = _repo.InsertNutritionLog(log);
        log.NutritionLogId = id;
        return (true, "Meal logged.", log);
    }

    public (double cal, double prot, double carbs, double fat, List<NutritionLog> meals)
        GetDailySummary(int personId, DateTime date)
    {
        var meals = _repo.GetLogsForDay(personId, date);
        double cal = 0, p = 0, c = 0, f = 0;
        foreach (var m in meals) { cal += m.Calories; p += m.ProteinG; c += m.CarbsG; f += m.FatG; }
        return (Math.Round(cal,2), Math.Round(p,2), Math.Round(c,2), Math.Round(f,2), meals);
    }

    /// <summary>Returns (overLimit, targetCalories) for calorie warning system (Spec §12).</summary>
    public (bool overLimit, double target) CheckCalorieLimit(int personId, double totalCaloriesLogged)
    {
        Person? p = _personRepo.GetById(personId);
        if (p == null) return (false, 0);
        double tdee = p.CalculateTDEE();
        double target = tdee;
        if (p.GoalId.HasValue)
        {
            Goal? g = _goalRepo.GetById(p.GoalId.Value);
            if (g != null) target = tdee + g.CalorieDelta;
        }
        return (totalCaloriesLogged > target, target);
    }

    public List<NutritionLog> GetRecent(int personId, int days = 7)
        => _repo.GetRecentLogs(personId, days);

    public (bool ok, string msg) DeleteLog(int id)
    {
        _repo.DeleteLog(id);
        return (true, "Log deleted.");
    }

    /// <summary>Archive yesterday's meals to history and delete them from live logs.</summary>
    public void ArchiveAndReset(int personId) => _repo.ArchiveTodayAndReset(personId);

    public List<NutritionHistory> GetHistory(int personId, int days = 30)
        => _repo.GetHistory(personId, days);
}

// ================================================================
//  PROGRESS SERVICE
// ================================================================
public class ProgressService
{
    private readonly ProgressRepository _repo       = new();
    private readonly PersonRepository   _personRepo = new();

    public (bool ok, string msg, ProgressSnapshot? snap) LogSnapshot(
        int personId, double weightKg, double? bodyFatPct, string? notes)
    {
        Person? p = _personRepo.GetById(personId);
        if (p == null) return (false, "User not found.", null);

        ProgressSnapshot snap = new()
        {
            PersonId     = personId,
            SnapshotDate = DateTime.Now,
            WeightKg     = weightKg,
            BodyFatPct   = bodyFatPct,
            BMI          = p.CalculateBMI(),
            Notes        = notes
        };

        int id = _repo.InsertSnapshot(snap);
        snap.SnapshotId = id;

        // Update person weight
        p.WeightKg = weightKg;
        if (bodyFatPct.HasValue) p.BodyFatPct = bodyFatPct;
        _personRepo.Update(p);

        return (true, "Progress saved.", snap);
    }

    public List<ProgressSnapshot> GetSnapshots(int personId, int take = 30)
        => _repo.GetSnapshots(personId, take);

    public (bool ok, string msg) Delete(int snapshotId)
    {
        _repo.DeleteSnapshot(snapshotId);
        return (true, "Snapshot deleted.");
    }
}

// ================================================================
//  WORKOUT SERVICE
// ================================================================
public class WorkoutService
{
    private readonly WorkoutRepository  _workRepo   = new();
    private readonly ExerciseRepository _exRepo     = new();
    private readonly PersonRepository   _personRepo = new();
    private readonly GoalRepository     _goalRepo   = new();

    public List<Exercise>         GetExercises()                      => _exRepo.GetAll();
    public List<ExerciseCategory> GetCategories()                     => _exRepo.GetAllCategories();
    public List<Exercise>         GetByCategory(int catId)            => _exRepo.GetByCategory(catId);
    public WorkoutPlan?           GetActivePlan(int personId)         => _workRepo.GetActivePlan(personId);
    public List<WorkoutPlan>      GetPlans(int personId)              => _workRepo.GetAllPlansForPerson(personId);
    public List<WorkoutSession>   GetHistory(int personId, int n=10)  => _workRepo.GetSessionHistory(personId, n);

    public (bool ok, string msg, WorkoutPlan? plan) CreatePlan(
        int createdBy, int? assignedTo, int goalId, string name, int weeks,
        List<(int exId, int day, int order, int sets, int? reps, int? secs, int rest)> exercises)
    {
        if (string.IsNullOrWhiteSpace(name)) return (false, "Plan name required.", null);
        if (!_goalRepo.Exists(goalId))       return (false, "Goal not found.", null);

        if (assignedTo.HasValue)
        {
            if (_personRepo.GetById(assignedTo.Value) == null)
                return (false, "Assigned user not found.", null);
            if (_workRepo.GetActivePlan(assignedTo.Value) != null)
                return (false, "This user already has an active plan (locked and unchangeable).", null);
        }

        WorkoutPlan plan = new()
        {
            CreatedByPersonId  = createdBy,
            AssignedToPersonId = assignedTo,
            GoalId             = goalId,
            PlanName           = name.Trim(),
            DurationWeeks      = weeks,
            IsActive           = true,
            CreatedAt          = DateTime.Now
        };

        int planId = _workRepo.InsertPlan(plan);
        plan.PlanId = planId;
        foreach (var ex in exercises)
            _workRepo.InsertPlanExercise(new WorkoutPlanExercise
            {
                PlanId      = planId,
                ExerciseId  = ex.exId,
                DayOfWeek   = ex.day,
                OrderInDay  = ex.order,
                Sets        = ex.sets,
                Reps        = ex.reps,
                Seconds     = ex.secs,
                RestSeconds = ex.rest
            });

        return (true, "Plan created.", _workRepo.GetActivePlan(assignedTo ?? createdBy));
    }

    public (bool success, string message, WorkoutSession? session) LogSession(
        int personId, int? planId, int durationMinutes, string? notes,
        List<(int exerciseId, int setNumber, int? actualReps, int? actualSeconds, double? weightKg)> sets)
    {
        Person? person = _personRepo.GetById(personId);
        if (person == null)
            return (false, "User not found.", null);

        if (sets.Count == 0)
            return (false, "Session must have at least one set.", null);

        WorkoutSession session = new WorkoutSession
        {
            PersonId        = personId,
            PlanId          = planId,
            SessionDate     = DateTime.Now,
            DurationMinutes = durationMinutes,
            TotalCalories   = 0,
            Notes           = notes
        };

        int sessionId = _workRepo.InsertSession(session);
        session.SessionId = sessionId;

        double totalCalories = 0;

        foreach (var set in sets)
        {
            Exercise? exercise = _exRepo.GetById(set.exerciseId);
            if (exercise == null) continue;

            int durationSecs = set.actualSeconds ?? (set.actualReps.HasValue ? set.actualReps.Value * 3 : 0);
            double calories  = exercise.CalculateCaloriesBurned(person.WeightKg, durationSecs);
            totalCalories   += calories;

            SessionLog log = new SessionLog
            {
                SessionId     = sessionId,
                ExerciseId    = set.exerciseId,
                ExerciseName  = exercise.Name,
                SetNumber     = set.setNumber,
                ActualReps    = set.actualReps,
                ActualSeconds = set.actualSeconds,
                WeightKg      = set.weightKg,
                CaloriesBurned = Math.Round(calories, 2)
            };

            _workRepo.InsertSessionLog(log);
        }

        _workRepo.UpdateSessionCalories(sessionId, Math.Round(totalCalories, 2));

        WorkoutSession? saved = _workRepo.GetSessionById(sessionId);
        return (true, "Session logged.", saved);
    }

    public (bool ok, string msg) DeletePlan(int planId)
    {
        _workRepo.DeletePlan(planId);
        return (true, "Plan deleted.");
    }

    /// <summary>
    /// Append an exercise to an existing active plan.
    /// Both trainee (unassigned) and trainer can call this.
    /// </summary>
    public (bool ok, string msg, WorkoutPlanExercise? exercise) AddExerciseToPlan(
        int planId, int exerciseId, int dayOfWeek, int sets, int? reps, int? secs, int restSeconds)
    {
        var ex = _exRepo.GetById(exerciseId);
        if (ex == null) return (false, "Exercise not found.", null);

        int order = _workRepo.GetNextOrderInDay(planId, dayOfWeek);
        var pe = new WorkoutPlanExercise
        {
            PlanId      = planId,
            ExerciseId  = exerciseId,
            ExerciseName = ex.Name,
            MuscleGroup  = ex.MuscleGroup,
            DayOfWeek   = dayOfWeek,
            OrderInDay  = order,
            Sets        = sets,
            Reps        = reps,
            Seconds     = secs,
            RestSeconds = restSeconds,
            IsUserAdded = true
        };
        _workRepo.AddExerciseToPlan(pe);
        return (true, "Exercise added.", pe);
    }

    /// <summary>Deactivate expired plans and return whether the plan just expired.</summary>
    public bool CheckAndExpirePlans(int personId)
    {
        _workRepo.DeactivateExpiredPlans(personId);
        return false; // After deactivation, GetActivePlan will return null
    }

    /// <summary>Get the expiry date of the active plan (for display purposes).</summary>
    public DateTime? GetPlanEndsAt(int personId) => _workRepo.GetPlanEndsAt(personId);
}

// ================================================================
//  DAILY CONFIRMATION SERVICE
// ================================================================
public class DailyConfirmationService
{
    private readonly DailyConfirmationRepository _repo = new();

    public void Confirm(int personId, int planExerciseId)
        => _repo.Confirm(personId, planExerciseId);

    public HashSet<int> GetConfirmedToday(int personId)
        => _repo.GetConfirmedToday(personId);

    public bool IsConfirmedToday(int personId, int planExerciseId)
        => _repo.IsConfirmedToday(personId, planExerciseId);
}


// ================================================================
//  ATTENDANCE SERVICE  (NEW — spec §7)
// ================================================================
public class AttendanceService
{
    private readonly AttendanceRepository _repo = new();

    public void MarkAttendance(int trainerId, string status, string? notes = null)
        => _repo.Upsert(trainerId, DateTime.Today, status, "Admin", notes);

    public List<TrainerAttendanceRecord> GetHistory(int trainerId, int days = 30)
        => _repo.GetByTrainer(trainerId, days);

    public List<TrainerAttendanceRecord> GetAllToday()
        => _repo.GetAllForDate(DateTime.Today);

    public string GetTodayStatus(int trainerId)
        => _repo.GetStatusForToday(trainerId) ?? "Not Marked";
}

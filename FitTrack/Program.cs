using FitTrack.Database;
using FitTrack.Models;
using FitTrack.Services;
using FitTrack.SeedData;

string connectionString = "Server=localhost\\SQLEXPRESS;Database=FitTrackOOP;Trusted_Connection=True;TrustServerCertificate=True;";

DatabaseHelper.Initialise(connectionString);
DatabaseSeeder.Seed();

AuthService     authService     = new AuthService();
UserService     userService     = new UserService();
GoalService     goalService     = new GoalService();
WorkoutService  workoutService  = new WorkoutService();
NutritionService nutritionService = new NutritionService();
ProgressService progressService = new ProgressService();

Person? currentUser = null;

Console.WriteLine("=== FIT-TRACK PRO ===");

while (true)
{
    Console.WriteLine();

    if (currentUser == null)
    {
        Console.WriteLine("[1] Register   [2] Login   [0] Exit");
        string choice = Ask("Choice");

        if (choice == "0") break;

        if (choice == "1")
        {
            List<Goal> goals = goalService.GetAllGoals();
            Console.WriteLine("\nAvailable Goals:");
            foreach (Goal g in goals) Console.WriteLine(g);

            string fullName  = Ask("Full Name");
            string username  = Ask("Username");
            string password  = Ask("Password");
            string email     = Ask("Email");
            string role      = Ask("Role (Trainee / Trainer / Admin)");
            string gender    = Ask("Gender (Male / Female / Other)");
            int    age       = int.Parse(Ask("Age"));
            double height    = double.Parse(Ask("Height cm"));
            double weight    = double.Parse(Ask("Weight kg"));
            double? bf       = TryDouble(Ask("Body Fat % (blank to skip)"));
            int?   goalId    = TryInt(Ask("Goal ID (blank to skip)"));

            var (ok, msg, person) = authService.Register(
                fullName, username, password, email, role, gender,
                age, height, weight, bf, goalId);

            Console.WriteLine(ok ? $"  OK — {msg}" : $"  ERROR: {msg}");
            if (ok) currentUser = person;
        }
        else if (choice == "2")
        {
            string username = Ask("Username");
            string password = Ask("Password");

            var (ok, msg, person) = authService.Login(username, password);
            Console.WriteLine(ok ? $"  OK — Welcome {person!.FullName}" : $"  ERROR: {msg}");
            if (ok) currentUser = person;
        }
    }
    else
    {
        Console.WriteLine($"Logged in as: {currentUser.FullName} [{currentUser.GetRoleLabel()}]");
        Console.WriteLine("[1]  View Profile        [2]  Update Profile      [3]  Change Password");
        Console.WriteLine("[4]  Health Metrics      [5]  All Goals           [6]  View Active Plan");
        Console.WriteLine("[7]  List Exercises      [8]  Log Workout         [9]  Session History");
        Console.WriteLine("[10] Search Food         [11] Log Meal            [12] Daily Nutrition");
        Console.WriteLine("[13] Log Progress        [14] Progress History");

        if (currentUser.Role == "Trainer" || currentUser.Role == "Admin")
        {
            Console.WriteLine("[15] My Trainees        [16] Create Plan         [17] Assign Trainer");
        }
        if (currentUser.Role == "Admin")
        {
            Console.WriteLine("[18] All Users          [19] Set Role            [20] All Plans");
        }
        Console.WriteLine("[0]  Logout");

        string choice = Ask("Choice");

        switch (choice)
        {
            case "0":
                currentUser = null;
                break;

            case "1":
            {
                Person? p = userService.GetById(currentUser.PersonId);
                if (p != null) PrintPerson(p);
                break;
            }

            case "2":
            {
                string fullName = Ask("Full Name");
                string email    = Ask("Email");
                string gender   = Ask("Gender");
                int    age      = int.Parse(Ask("Age"));
                double height   = double.Parse(Ask("Height cm"));
                double weight   = double.Parse(Ask("Weight kg"));
                double? bf      = TryDouble(Ask("Body Fat % (blank to skip)"));
                int? goalId     = TryInt(Ask("Goal ID (blank to skip)"));

                var (ok, msg) = userService.UpdateProfile(
                    currentUser.PersonId, fullName, email, gender, age, height, weight, bf, goalId);

                Console.WriteLine(ok ? $"  OK — {msg}" : $"  ERROR: {msg}");
                if (ok) currentUser = userService.GetById(currentUser.PersonId) ?? currentUser;
                break;
            }

            case "3":
            {
                string current = Ask("Current Password");
                string newPass = Ask("New Password");
                var (ok, msg) = authService.ChangePassword(currentUser.PersonId, current, newPass);
                Console.WriteLine(ok ? $"  OK — {msg}" : $"  ERROR: {msg}");
                break;
            }

            case "4":
            {
                Console.WriteLine("  Activity: 1=Sedentary 2=Light 3=Moderate 4=VeryActive 5=Athlete");
                double mult = Ask("Level (1-5)") switch
                {
                    "1" => 1.2, "2" => 1.375, "3" => 1.55, "4" => 1.725, "5" => 1.9, _ => 1.375
                };

                var (bmi, cat, bmr, tdee, target) =
                    goalService.GetHealthMetrics(currentUser.PersonId, mult);

                Console.WriteLine($"  BMI: {bmi} ({cat})");
                Console.WriteLine($"  BMR: {bmr} kcal   TDEE: {tdee} kcal   Target: {target} kcal");
                break;
            }

            case "5":
            {
                foreach (Goal g in goalService.GetAllGoals())
                    Console.WriteLine("  " + g);
                break;
            }

            case "6":
            {
                WorkoutPlan? plan = workoutService.GetActivePlan(currentUser.PersonId);
                if (plan == null) { Console.WriteLine("  No active plan found."); break; }
                PrintPlan(plan);
                break;
            }

            case "7":
            {
                foreach (Exercise e in workoutService.GetAllExercises())
                    Console.WriteLine("  " + e);
                break;
            }

            case "8":
            {
                int durationMins = int.Parse(Ask("Duration (minutes)"));
                string? notes   = NullIfEmpty(Ask("Notes (blank to skip)"));

                var sets = new List<(int, int, int?, int?, double?)>();
                int setNum = 1;

                Console.WriteLine("  Add sets. Press Enter with no Exercise ID to finish.");
                while (true)
                {
                    string exStr = Ask($"Set {setNum} Exercise ID (blank to stop)");
                    if (string.IsNullOrWhiteSpace(exStr)) break;

                    int exId     = int.Parse(exStr);
                    int? reps    = TryInt(Ask("  Reps (blank if timed)"));
                    int? secs    = TryInt(Ask("  Seconds (blank if reps)"));
                    double? wkg  = TryDouble(Ask("  Weight kg (blank = bodyweight)"));

                    sets.Add((exId, setNum, reps, secs, wkg));
                    setNum++;
                }

                var (ok, msg, session) = workoutService.LogSession(
                    currentUser.PersonId, null, durationMins, notes, sets);

                Console.WriteLine(ok ? "  OK — " + msg : "  ERROR: " + msg);
                if (ok && session != null) PrintSession(session);
                break;
            }

            case "9":
            {
                List<WorkoutSession> sessions = workoutService.GetSessionHistory(currentUser.PersonId, 10);
                if (sessions.Count == 0) { Console.WriteLine("  No sessions found."); break; }
                foreach (WorkoutSession s in sessions) Console.WriteLine("  " + s);
                break;
            }

            case "10":
            {
                string query = Ask("Search food name");
                List<FoodItem> foods = nutritionService.SearchFood(query);
                if (foods.Count == 0) { Console.WriteLine("  No results found."); break; }
                foreach (FoodItem f in foods) Console.WriteLine("  " + f);
                break;
            }

            case "11":
            {
                int foodId        = int.Parse(Ask("Food Item ID"));
                string mealType   = Ask("Meal Type (Breakfast / Lunch / Dinner / Snack)");
                double servingGrams = double.Parse(Ask("Serving (grams)"));

                var (ok, msg, log) = nutritionService.LogMeal(
                    currentUser.PersonId, foodId, mealType, servingGrams);

                Console.WriteLine(ok ? "  OK — " + msg : "  ERROR: " + msg);
                if (ok && log != null) Console.WriteLine("  " + log);
                break;
            }

            case "12":
            {
                string dateStr  = Ask("Date yyyy-MM-dd (blank = today)");
                DateTime date   = string.IsNullOrWhiteSpace(dateStr) ? DateTime.Today : DateTime.Parse(dateStr);

                var (cal, pro, carb, fat, meals) =
                    nutritionService.GetDailySummary(currentUser.PersonId, date);

                Console.WriteLine($"  Date: {date:yyyy-MM-dd}");
                Console.WriteLine($"  Total: {cal} kcal  P:{pro}g  C:{carb}g  F:{fat}g");
                Console.WriteLine($"  Meals ({meals.Count}):");
                foreach (NutritionLog m in meals) Console.WriteLine("    " + m);
                break;
            }

            case "13":
            {
                double weight  = double.Parse(Ask("Weight kg"));
                double? bf     = TryDouble(Ask("Body Fat % (blank to skip)"));
                string? notes  = NullIfEmpty(Ask("Notes (blank to skip)"));

                var (ok, msg, snap) = progressService.LogSnapshot(
                    currentUser.PersonId, weight, bf, notes);

                Console.WriteLine(ok ? "  OK — " + msg : "  ERROR: " + msg);
                if (ok && snap != null) Console.WriteLine("  " + snap);
                break;
            }

            case "14":
            {
                List<ProgressSnapshot> snaps = progressService.GetSnapshots(currentUser.PersonId, 10);
                if (snaps.Count == 0) { Console.WriteLine("  No snapshots found."); break; }
                foreach (ProgressSnapshot s in snaps) Console.WriteLine("  " + s);
                break;
            }

            case "15":
            {
                List<Person> trainees = userService.GetTraineesByTrainer(currentUser.PersonId);
                if (trainees.Count == 0) { Console.WriteLine("  No trainees assigned."); break; }
                foreach (Person p in trainees) PrintPerson(p);
                break;
            }

            case "16":
            {
                Console.WriteLine("  Goals:"); foreach (Goal g in goalService.GetAllGoals()) Console.WriteLine("  " + g);
                Console.WriteLine("  Exercises:"); foreach (Exercise e in workoutService.GetAllExercises()) Console.WriteLine("  " + e);

                string planName     = Ask("Plan Name");
                int goalId          = int.Parse(Ask("Goal ID"));
                int durationWeeks   = int.Parse(Ask("Duration (weeks)"));
                int? assignedTo     = TryInt(Ask("Assign to Person ID (blank = template)"));

                var exercises = new List<(int, int, int, int, int?, int?, int)>();
                Console.WriteLine("  Add exercises. Blank Exercise ID to stop.");
                while (true)
                {
                    string exStr = Ask("Exercise ID (blank to stop)");
                    if (string.IsNullOrWhiteSpace(exStr)) break;

                    int exId    = int.Parse(exStr);
                    int day     = int.Parse(Ask("Day of Week (1=Mon 7=Sun)"));
                    int order   = int.Parse(Ask("Order in Day"));
                    int sets    = int.Parse(Ask("Sets"));
                    int? reps   = TryInt(Ask("Reps (blank if timed)"));
                    int? secs   = TryInt(Ask("Seconds (blank if reps)"));
                    int rest    = int.Parse(Ask("Rest seconds"));

                    exercises.Add((exId, day, order, sets, reps, secs, rest));
                }

                var (ok, msg, plan) = workoutService.CreatePlan(
                    currentUser.PersonId, assignedTo, goalId, planName, durationWeeks, exercises);

                Console.WriteLine(ok ? "  OK — " + msg : "  ERROR: " + msg);
                if (ok && plan != null) PrintPlan(plan);
                break;
            }

            case "17":
            {
                int traineeId = int.Parse(Ask("Trainee Person ID"));
                int trainerId = int.Parse(Ask("Trainer Person ID"));
                var (ok, msg) = userService.AssignTrainer(traineeId, trainerId);
                Console.WriteLine(ok ? "  OK — " + msg : "  ERROR: " + msg);
                break;
            }

            case "18":
            {
                List<Person> all = userService.GetAll();
                foreach (Person p in all) PrintPerson(p);
                break;
            }

            case "19":
            {
                int personId = int.Parse(Ask("Person ID"));
                string role  = Ask("New Role (Trainee / Trainer / Admin)");
                var (ok, msg) = userService.SetRole(personId, role);
                Console.WriteLine(ok ? "  OK — " + msg : "  ERROR: " + msg);
                break;
            }

            case "20":
            {
                List<WorkoutPlan> plans = workoutService.GetAllPlans();
                foreach (WorkoutPlan p in plans) Console.WriteLine("  " + p);
                break;
            }

            default:
                Console.WriteLine("  Unknown option.");
                break;
        }
    }
}

Console.WriteLine("Goodbye.");

static string Ask(string label)
{
    Console.Write($"  {label}: ");
    return Console.ReadLine()?.Trim() ?? string.Empty;
}

static int? TryInt(string s) => int.TryParse(s, out int v) ? v : null;
static double? TryDouble(string s) => double.TryParse(s, out double v) ? v : null;
static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

static void PrintPerson(Person p) => Console.WriteLine("  " + p);

static void PrintPlan(WorkoutPlan plan)
{
    Console.WriteLine("  " + plan);
    foreach (WorkoutPlanExercise e in plan.Exercises)
        Console.WriteLine(e);
}

static void PrintSession(WorkoutSession session)
{
    Console.WriteLine("  " + session);
    foreach (SessionLog log in session.Logs)
        Console.WriteLine(log);
}

# Fit Track Pro

Premium WinForms gym management app — C# · .NET 8 · Microsoft SQL Server.

---

## Folder structure

```
FitTrackPro/
├── Business/
│   ├── Models/          Person.cs · Models.cs (all entity classes)
│   └── Services/        AuthService · UserService · GoalService
│                        NutritionService · WorkoutService
│                        ProgressService · AttendanceService · StreakService
├── Data/
│   ├── Database/        DatabaseHelper · PersonRepository · GoalRepository
│   │                    NutritionRepository · WorkoutRepository
│   │                    ProgressRepository · ExerciseRepository
│   │                    AttendanceRepository · StreakRepository
│   ├── FitTrackOOP_Schema.sql   ← run first
│   ├── 02_ImportCSV.sql         ← run second (food data)
│   └── FoodItems.csv            ← source CSV
├── GUI/
│   ├── UITheme.cs               Design system (colors, fonts, helpers)
│   ├── Controls/CustomControls  RoundedPanel · GoldButton · MetricCard
│   │                            SidebarNavButton · StyledTextBox · LineChartControl
│   └── Forms/
│       ├── LandingForm.cs       Animated splash — Sign In / Sign Up only
│       ├── LoginForm.cs         Auto-routes Trainer / Trainee / Admin
│       ├── RegisterForm.cs      Role selection + full validation
│       ├── MainShell.cs         Base shell — sidebar · clock · nav
│       ├── Admin/               AdminShell · Dashboard · TrainerList
│       │                        AttendancePanel · AddTrainerPanel
│       ├── Trainer/             TrainerShell · Dashboard · Profile
│       │                        TraineeManagement · Exercises · Nutrition
│       └── Trainee/             TraineeShell · Dashboard · Profile
│                                Workout · Nutrition · Progress · Settings
├── EnvLoader.cs                 Reads .env file
├── Program.cs                   Entry point
├── FitTrackPro.csproj
├── app.manifest
├── .env                         ← you create this (see below)
└── .gitignore
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| Visual Studio 2022+ | with .NET Desktop workload |
| .NET SDK | 8.0 |
| SQL Server | 2019+ (Express is fine) |
| SSMS | any recent version |

---

## Setup — step by step

### 1. Clone & open

```bash
git clone https://github.com/Muhammad-Wasif/Gym-Tracker.git
cd Gym-Tracker
```

Open `FitTrackPro.csproj` in Visual Studio 2022.

---

### 2. Create `.env` file

Create a file called `.env` in the project root (same folder as `FitTrackPro.csproj`):

```
DB_CONNECTION_STRING=Server=localhost;Database=FitTrackOOP;Integrated Security=True;TrustServerCertificate=True;
```

> Change `localhost` to your SQL Server instance name if needed (e.g. `localhost\SQLEXPRESS`).

**Important:** `.env` is in `.gitignore` — it will never be committed.

---

### 3. Create database

Open SSMS → New Query → run `Data/FitTrackOOP_Schema.sql`.

This creates:
- `FitTrackOOP` database
- All tables (Users, Goals, Exercises, WorkoutPlans, NutritionLogs, ProgressSnapshots, LoginStreaks, TrainerAttendance, …)
- Seed data (Goals, ExerciseCategories, 40 Exercises)

---

### 4. Import food data (CSV)

Open `Data/02_ImportCSV.sql` in SSMS.

**Edit line 25** — change the path to match your local CSV location:

```sql
FROM 'C:\FitTrackPro\Data\FoodItems.csv'   -- ← change this
```

Run the script. It bulk-inserts 90+ food items from `FoodItems.csv`.

---

### 5. Restore NuGet packages

Visual Studio does this automatically on build. Or run:

```bash
dotnet restore
```

Packages:
- `Microsoft.Data.SqlClient 5.2.1`
- `BCrypt.Net-Next 4.0.3`

---

### 6. Build & run

Press **F5** in Visual Studio.

---

## Accounts

### Admin (hidden — spec §6)
| Username | Password |
|----------|----------|
| `admin`  | `Wasif-92743` |

> No admin button is shown in the UI. Type these into the normal Sign In form.

### Username rules
- Letters + numbers only
- Must contain **both** letters and numbers
- No symbols

### Password rules
- Minimum 8 characters
- Must contain: uppercase · lowercase · number · symbol

---

## Features implemented

| # | Spec | Feature |
|---|------|---------|
| 3 | §3  | Landing form — Sign In / Sign Up only |
| 4 | §4  | Sign Up with role choice (Trainer / Trainee) + full validation |
| 5 | §5  | Login auto-routes by role — no role selector shown |
| 6 | §6  | Hidden admin login (`admin` / `Wasif-92743`) |
| 7 | §7  | Admin: add trainers, view list, mark attendance (Present/On Leave/Absent), history |
| 8 | §8  | Trainer: profile, trainee list, assign daily/weekly workout & nutrition plans |
| 9 | §9  | Trainer lock — trainee cannot change trainer once assigned; trainer can remove |
| 10 | §10 | Trainee health data (BMI/BMR/TDEE) viewable by trainer, editable only by trainee |
| 11 | §11 | Goal-based system (Weight Loss / Fat Loss / Muscle Gain / Maintenance / Weight Gain) |
| 12 | §12 | Calorie warning — shows warning when limit exceeded but does not block |
| 13 | §13 | Trainee panel: view plans if has trainer; manage own plans if individual |
| 14 | §14 | Progress chart (weight over time, line chart with gradient fill) |
| 15 | §15 | Login streak — current streak + longest streak tracked per user |
| 16 | §16 | White · Gold · Red · Gray premium color palette |
| 17 | §17 | Animated "FIT TRACK PRO" gold glow loop on landing screen |
| 18 | §18 | Collapsible sidebar with smooth animation · live clock / date / day (top-right) |
| 19 | §19 | Profile cards (BMI, BMR, TDEE, Height, Weight) · Change password with validation |
| 20 | §20 | Pure WinForms — no XAML, no WPF, no web tech |
| 21 | §21 | Clean architecture: Business / Data / GUI layers · SQL parameterisation · BCrypt |
| 22 | §22 | SQL schema · CSV import · NuGet list · setup instructions (this file) |

---

## Files to delete from original repo

These files in the original commit should be removed (spec §1):

```
dm.zip
jb/        (Rider index cache)
obj/       (build output)
.vs/       (Visual Studio cache)
```

Keep: `Business/` · `Data/` · `GUI/` · `.gitignore` · `README.md` · `app.manifest`

---

## Architecture notes

```
Program.cs
  └── EnvLoader.Load()             reads .env → DB_CONNECTION_STRING
  └── DatabaseHelper.Initialise()  passes string to SqlConnection factory
  └── LandingForm                  entry UI

GUI layer  →  calls  →  Service layer  →  calls  →  Repository layer  →  SQL Server
```

- **No hardcoded connection strings** anywhere in code.
- **BCrypt** used for all password storage.
- **SQL parameterisation** used on every query — no string concatenation.
- **Trainer lock** enforced at service + DB level (`TrainerLocked` bit column).
- **Streak** updated on every successful login; resets if a day is missed.
- **Calorie warning** shown inline but never blocks food logging (spec §12).


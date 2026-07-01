using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainee;

public class TraineeDashboardForm : AppShell
{
    // ── Services ─────────────────────────────────────────────────
    private readonly GoalService      _goalSvc   = new();
    private readonly StreakService    _streakSvc = new();
    private readonly NutritionService _nutSvc    = new();

    // ── Content-specific controls ─────────────────────────────────
    private Label              lblWelcome        = null!;
    private Label              lblDateText       = null!;
    private ProgressRingControl ringCalories     = null!;
    private RoundedPanel       panelStreak       = null!;
    private Label              lblStreakTitle     = null!;
    private Label              lblStreakDetails   = null!;
    private Label              lblStreakMotivation= null!;
    private MetricCard         cardBMI           = null!;
    private MetricCard         cardStatus        = null!;
    private MetricCard         cardBMR           = null!;
    private MetricCard         cardTDEE          = null!;
    private Label              lblNutritionTitle = null!;
    private RoundedPanel       panelNutrition    = null!;
    private Label              lblTrainerInfo     = null!;

    // ── Designer support ─────────────────────────────────────────
    public TraineeDashboardForm() : this(new Models.Trainee { FullName = "Trainee Designer", Role = "Trainee" }) { }

    public TraineeDashboardForm(Person user) : base(user, "Dashboard")
    {
        // ── Wire nav buttons ─────────────────────────────────────
        var btnDash  = AddNavButton("🏠", "Dashboard", "dashboard");
        var btnProf  = AddNavButton("👤", "Profile",   "profile");
        var btnWork  = AddNavButton("💪", "Workout",   "workout");
        var btnNut   = AddNavButton("🥗", "Nutrition", "nutrition");
        var btnProg  = AddNavButton("📈", "Progress",  "progress");
        var btnSets  = AddNavButton("⚙️", "Settings",  "settings");

        btnDash.Click += (_, _) => NavigateToForm<TraineeDashboardForm>();
        btnProf.Click += (_, _) => NavigateToForm<TraineeProfileForm>();
        btnWork.Click += (_, _) => NavigateToForm<TraineeWorkoutForm>();
        btnNut.Click  += (_, _) => NavigateToForm<TraineeNutritionForm>();
        btnProg.Click += (_, _) => NavigateToForm<TraineeProgressForm>();
        btnSets.Click += (_, _) => NavigateToForm<TraineeSettingsForm>();

        SetActiveNav("dashboard");

        // ── Build content ─────────────────────────────────────────
        BuildContent();
        LoadDashboardData(user);

        Resize += (_, _) => ReflowMetricCards();
        ReflowMetricCards();
        PlayEntryAnimation();
    }

    // ─────────────────────────────────────────────────────────────
    //  CONTENT BUILD
    // ─────────────────────────────────────────────────────────────
    private void BuildContent()
    {
        // Welcome heading
        lblWelcome = new Label
        {
            Text      = $"Welcome, {CurrentUser.FullName}!",
            Location  = new Point(0, 0),
            Size      = new Size(700, 40),
            BackColor = Color.Transparent,
            Font      = UITheme.FontHeading(22f),
            ForeColor = UITheme.TextPrimary
        };

        // Date sub-label
        lblDateText = new Label
        {
            Text      = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
            Location  = new Point(0, 44),
            Size      = new Size(700, 20),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(9.5f),
            ForeColor = UITheme.TextSecondary
        };

        // Calorie ring
        ringCalories = new ProgressRingControl
        {
            Location  = new Point(0, 76),
            Size      = new Size(140, 140),
            Progress  = 0f,
            SubText   = "Calories",
            RingColor = UITheme.Gold
        };

        // Streak panel
        panelStreak = new RoundedPanel { Location = new Point(156, 76), Size = new Size(420, 140) };

        lblStreakTitle = new Label
        {
            Text      = "🔥  0-Day Streak",
            Location  = new Point(UITheme.S16, UITheme.S16),
            Size      = new Size(380, 30),
            BackColor = Color.Transparent,
            Font      = UITheme.FontHeading(16f),
            ForeColor = UITheme.Gold
        };
        lblStreakDetails = new Label
        {
            Text      = "Best: 0 days  •  Today: 0 / 0 kcal",
            Location  = new Point(UITheme.S16, 52),
            Size      = new Size(388, 22),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(10f),
            ForeColor = UITheme.TextSecondary
        };
        lblStreakMotivation = new Label
        {
            Text      = "Keep pushing! Consistency is the key to results.",
            Location  = new Point(UITheme.S16, 80),
            Size      = new Size(388, 22),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(9f),
            ForeColor = UITheme.TextMuted
        };
        panelStreak.Controls.AddRange(new Control[] { lblStreakTitle, lblStreakDetails, lblStreakMotivation });

        // Metric cards — standard 220x96
        cardBMI    = new MetricCard { Label = "BMI",    Metric = "0.0",    AccentColor = UITheme.Gold,          Location = new Point(0,   232), Size = new Size(220, 96) };
        cardStatus = new MetricCard { Label = "Status", Metric = "Normal", AccentColor = UITheme.Warning,       Location = new Point(236, 232), Size = new Size(220, 96) };
        cardBMR    = new MetricCard { Label = "BMR",    Metric = "0 kcal", AccentColor = UITheme.Red,           Location = new Point(472, 232), Size = new Size(220, 96) };
        cardTDEE   = new MetricCard { Label = "TDEE",   Metric = "0 kcal", AccentColor = UITheme.TextSecondary, Location = new Point(708, 232), Size = new Size(220, 96) };

        // Nutrition summary section
        lblNutritionTitle = new Label
        {
            Text      = "Today's Nutrition",
            Location  = new Point(0, 344),
            Size      = new Size(400, 22),
            BackColor = Color.Transparent,
            Font      = UITheme.FontSemiBold(11f),
            ForeColor = UITheme.TextPrimary
        };
        panelNutrition = new RoundedPanel { Location = new Point(0, 372), Size = new Size(752, 80) };

        // Trainer info
        lblTrainerInfo = new Label
        {
            Text      = "🏋️ Loading trainer info...",
            Location  = new Point(0, 468),
            Size      = new Size(752, 22),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(10f),
            ForeColor = UITheme.TextSecondary
        };

        _contentArea.Controls.AddRange(new Control[]
        {
            lblWelcome, lblDateText, ringCalories, panelStreak,
            cardBMI, cardStatus, cardBMR, cardTDEE,
            lblNutritionTitle, panelNutrition, lblTrainerInfo
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  DATA LOAD
    // ─────────────────────────────────────────────────────────────
    private void LoadDashboardData(Person user)
    {
        lblWelcome.Text  = $"Welcome, {user.FullName}!";
        lblDateText.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");

        var streak = _streakSvc.GetOrCreate(user.PersonId);
        var (calToday, prot, carbs, fat, _) = _nutSvc.GetDailySummary(user.PersonId, DateTime.Today);
        var (bmi, cat, bmr, tdee, target)    = _goalSvc.GetMetrics(user.PersonId);

        ringCalories.Progress = target > 0 ? (float)(calToday / target) : 0f;

        lblStreakTitle.Text    = $"🔥  {streak.CurrentStreak}-Day Streak";
        lblStreakDetails.Text  = $"Best: {streak.LongestStreak} days  •  Today: {calToday:F0} / {target:F0} kcal";

        cardBMI.Metric    = $"{bmi:F1}";
        cardStatus.Metric = cat;
        cardBMR.Metric    = $"{bmr:F0} kcal";
        cardTDEE.Metric   = $"{tdee:F0} kcal";

        // Nutrition summary panels
        panelNutrition.Controls.Clear();
        panelNutrition.Controls.Add(StatLabel($"{calToday:F0} kcal", "Calories", 0));
        panelNutrition.Controls.Add(StatLabel($"{prot:F1}g",          "Protein",  188));
        panelNutrition.Controls.Add(StatLabel($"{carbs:F1}g",         "Carbs",    376));
        panelNutrition.Controls.Add(StatLabel($"{fat:F1}g",           "Fat",      564));

        lblTrainerInfo.Text = user is Models.Trainee t && t.TrainerName != null
            ? $"🏋️ Your Trainer: {t.TrainerName}"
            : "🏋️ No trainer assigned — you're training independently";
    }

    private Label StatLabel(string val, string lbl, int x) => new Label
    {
        Text      = $"{val}\n{lbl}",
        Location  = new Point(x + UITheme.S16, 10),
        Size      = new Size(168, 56),
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleCenter,
        Font      = UITheme.FontHeading(12f),
        ForeColor = UITheme.TextPrimary
    };

    // ─────────────────────────────────────────────────────────────
    //  REFLOW
    // ─────────────────────────────────────────────────────────────
    private void ReflowMetricCards()
    {
        cardBMI.Location    = new Point(0,   232); cardBMI.Size    = new Size(220, 96);
        cardStatus.Location = new Point(236, 232); cardStatus.Size = new Size(220, 96);
        cardBMR.Location    = new Point(472, 232); cardBMR.Size    = new Size(220, 96);
        cardTDEE.Location   = new Point(708, 232); cardTDEE.Size   = new Size(220, 96);
    }

    // ─────────────────────────────────────────────────────────────
    //  ENTRY ANIMATION (stagger-fade in)
    // ─────────────────────────────────────────────────────────────
    private void PlayEntryAnimation()
    {
        var items = _contentArea.Controls.Cast<Control>().ToList();
        foreach (var c in items) c.Visible = false;
        var tmr = new System.Windows.Forms.Timer { Interval = 50 };
        int idx = 0;
        tmr.Tick += (_, _) =>
        {
            if (idx >= items.Count) { tmr.Stop(); return; }
            items[idx].Visible = true;
            idx++;
        };
        tmr.Start();
    }
}

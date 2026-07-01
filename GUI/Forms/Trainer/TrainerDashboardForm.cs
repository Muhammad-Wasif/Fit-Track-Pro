using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainer;

public class TrainerDashboardForm : AppShell
{
    private readonly UserService _userSvc = new();
    private readonly StreakService _streakSvc = new();

    // Content controls
    private Label lblWelcome = null!;
    private Label lblDashboardSub = null!;
    private MetricCard cardTrainees = null!;
    private MetricCard cardStreak = null!;
    private MetricCard cardBestStreak = null!;

    public TrainerDashboardForm() : this(new FitTrack.Models.Trainer { FullName = "Trainer Designer", Role = "Trainer" }) { }

    public TrainerDashboardForm(Person user) : base(user, "Dashboard")
    {
        // Nav buttons
        AddNavButton("🏠", "Dashboard",   "dashboard").Click += (_, _) => NavigateToForm<TrainerDashboardForm>();
        AddNavButton("👤", "Profile",     "profile"  ).Click += (_, _) => NavigateToForm<TrainerProfileForm>();
        AddNavButton("👥", "My Trainees", "trainees" ).Click += (_, _) => NavigateToForm<TraineeManagementForm>();
        AddNavButton("💪", "Exercises",   "exercises").Click += (_, _) => NavigateToForm<TrainerExerciseForm>();
        AddNavButton("🥗", "Nutrition",   "nutrition").Click += (_, _) => NavigateToForm<TrainerNutritionForm>();
        AddNavButton("⚙️", "Settings",   "settings" ).Click += (_, _) => NavigateToForm<TrainerSettingsForm>();
        SetActiveNav("dashboard");

        BuildContent();
        LoadDashboardData(user);

        Resize += (_, _) => UITheme.ResponsiveReflow(_contentArea);
    }

    private void BuildContent()
    {
        lblWelcome = new Label
        {
            Text      = "Welcome back, Trainer!",
            Location  = new Point(UITheme.S24, UITheme.S24),
            Size      = new Size(700, 36),
            Font      = UITheme.FontHeading(22f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        };

        lblDashboardSub = new Label
        {
            Location  = new Point(UITheme.S24, 68),
            Size      = new Size(700, 20),
            Font      = UITheme.FontBody(9.5f),
            ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };

        cardTrainees = new MetricCard
        {
            Location    = new Point(0, 112),
            Size        = new Size(220, 96),
            Label       = "Trainees",
            Metric      = "0",
            AccentColor = UITheme.Gold
        };

        cardStreak = new MetricCard
        {
            Location    = new Point(236, 112),
            Size        = new Size(220, 96),
            Label       = "Login Streak",
            Metric      = "0 days",
            AccentColor = UITheme.Red
        };

        cardBestStreak = new MetricCard
        {
            Location    = new Point(472, 112),
            Size        = new Size(220, 96),
            Label       = "Best Streak",
            Metric      = "0 days",
            AccentColor = UITheme.TextSecondary
        };

        _contentArea.Controls.AddRange(new Control[]
        {
            lblWelcome, lblDashboardSub,
            cardTrainees, cardStreak, cardBestStreak
        });
    }

    private void LoadDashboardData(Person user)
    {
        lblWelcome.Text      = $"Welcome back, {user.FullName}!";
        lblDashboardSub.Text = $"Trainer Dashboard  •  {DateTime.Now:dddd, dd MMMM yyyy}";

        var streak       = _streakSvc.GetOrCreate(user.PersonId);
        int traineeCount = _userSvc.GetTraineesByTrainer(user.PersonId).Count;

        cardTrainees.Metric    = traineeCount.ToString();
        cardStreak.Metric      = $"{streak.CurrentStreak} days";
        cardBestStreak.Metric  = $"{streak.LongestStreak} days";
    }
}

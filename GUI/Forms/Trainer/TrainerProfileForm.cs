using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainer;

public class TrainerProfileForm : AppShell
{
    private readonly UserService _userSvc = new();
    private readonly GoalService _goalSvc = new();

    // Content controls
    private Label lblHeading = null!;
    private RoundedPanel card = null!;

    public TrainerProfileForm() : this(new FitTrack.Models.Trainer { FullName = "Trainer Designer", Role = "Trainer" }) { }

    public TrainerProfileForm(Person user) : base(user, "My Profile")
    {
        // Nav buttons
        AddNavButton("🏠", "Dashboard",   "dashboard").Click += (_, _) => NavigateToForm<TrainerDashboardForm>();
        AddNavButton("👤", "Profile",     "profile"  ).Click += (_, _) => NavigateToForm<TrainerProfileForm>();
        AddNavButton("👥", "My Trainees", "trainees" ).Click += (_, _) => NavigateToForm<TraineeManagementForm>();
        AddNavButton("💪", "Exercises",   "exercises").Click += (_, _) => NavigateToForm<TrainerExerciseForm>();
        AddNavButton("🥗", "Nutrition",   "nutrition").Click += (_, _) => NavigateToForm<TrainerNutritionForm>();
        AddNavButton("⚙️", "Settings",   "settings" ).Click += (_, _) => NavigateToForm<TrainerSettingsForm>();
        SetActiveNav("profile");

        BuildContent();
        LoadProfileData(user);

        Resize += (_, _) => UITheme.ResponsiveReflow(_contentArea);
    }

    private void BuildContent()
    {
        lblHeading = new Label
        {
            Text      = "Trainer Profile",
            Location  = new Point(UITheme.S24, UITheme.S24),
            Size      = new Size(500, 36),
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        };

        card = new RoundedPanel
        {
            Location = new Point(UITheme.S24, 70),
            Size     = new Size(700, 360)
        };

        _contentArea.Controls.Add(lblHeading);
        _contentArea.Controls.Add(card);
    }

    private void LoadProfileData(Person user)
    {
        var (bmi, cat, bmr, tdee, _) = _goalSvc.GetMetrics(user.PersonId);

        AddMetric(card, "BMI",    bmi.ToString("F1"),            UITheme.Gold,          0,   0);
        AddMetric(card, "BMR",    bmr.ToString("F0") + " kcal",  UITheme.Red,           170, 0);
        AddMetric(card, "TDEE",   tdee.ToString("F0") + " kcal", UITheme.TextSecondary, 340, 0);
        AddMetric(card, "Height", user.HeightCm + " cm",         UITheme.Gold,          0,   110);
        AddMetric(card, "Weight", user.WeightKg + " kg",         UITheme.Red,           170, 110);
        AddMetric(card, "Age",    user.Age + " yrs",             UITheme.TextSecondary, 340, 110);

        int iy = 240;
        AddInfo(card, "Full Name",  user.FullName,  0,   iy);
        AddInfo(card, "Username",   user.Username,  340, iy);
        AddInfo(card, "Email",      user.Email,     0,   iy + 55);
        AddInfo(card, "BMI Status", cat,            340, iy + 55);
    }

    private void AddMetric(Panel p, string label, string val, Color accent, int x, int y) =>
        p.Controls.Add(new MetricCard
        {
            Left        = x + 16,
            Top         = y + 16,
            Width       = 150,
            Height      = 88,
            Label       = label,
            Metric      = val,
            AccentColor = accent
        });

    private void AddInfo(Panel p, string label, string val, int x, int y)
    {
        p.Controls.Add(new Label
        {
            Text      = label,
            Left      = x + 16,
            Top       = y,
            Width     = 300,
            Height    = 16,
            Font      = UITheme.FontSemiBold(8.5f),
            ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        });
        p.Controls.Add(new Label
        {
            Text      = val,
            Left      = x + 16,
            Top       = y + 18,
            Width     = 300,
            Height    = 22,
            Font      = UITheme.FontSemiBold(11f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        });
    }
}

using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainee;

public class TraineeWorkoutForm : AppShell
{
    // ── Services ─────────────────────────────────────────────────
    private readonly WorkoutService           _workSvc    = new();
    private readonly GoalService              _goalSvc    = new();
    private readonly DailyConfirmationService _confirmSvc = new();
    private readonly bool                     _hasTrainer;

    // ── Content-specific controls ─────────────────────────────────
    private Label      lblHeading    = null!;
    private Label      lblPlanMode   = null!;
    private GoldButton btnCreatePlan = null!;
    private Panel      _scrollPanel  = null!;

    // ── Designer support ─────────────────────────────────────────
    public TraineeWorkoutForm() : this(new Models.Trainee { FullName = "Trainee Designer", Role = "Trainee" }) { }

    public TraineeWorkoutForm(Person user) : base(user, "Workout Plan")
    {
        _hasTrainer = user.TrainerId.HasValue;

        // ── Nav ──
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

        SetActiveNav("workout");

        // ── Build content ─────────────────────────────────────────
        lblHeading = new Label
        {
            Text      = "Workout Plan",
            Location  = new Point(0, 0),
            Size      = new Size(500, 36),
            BackColor = Color.Transparent,
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary
        };

        lblPlanMode = new Label
        {
            Location  = new Point(0, 44),
            Size      = new Size(760, 24),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(10f),
            ForeColor = UITheme.TextSecondary
        };

        btnCreatePlan = new GoldButton
        {
            Text     = "+ Create Plan",
            Location = new Point(0, 76),
            Size     = new Size(180, UITheme.ButtonHeight)
        };
        btnCreatePlan.Click += (_, _) => CreatePlanClick();

        _scrollPanel = new Panel
        {
            Location    = new Point(0, 76),
            Size        = new Size(760, 550),
            AutoScroll  = true,
            BackColor   = Color.Transparent
        };

        _contentArea.Controls.AddRange(new Control[]
        {
            lblHeading, lblPlanMode, btnCreatePlan, _scrollPanel
        });

        LoadWorkoutData();

        Resize += (_, _) =>
        {
            _scrollPanel.Width  = _contentArea.Width  - 56;
            _scrollPanel.Height = _contentArea.Height - 100;
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  LOAD WORKOUT DATA
    // ─────────────────────────────────────────────────────────────
    private void LoadWorkoutData()
    {
        _scrollPanel.Controls.Clear();
        _workSvc.CheckAndExpirePlans(CurrentUser.PersonId);

        if (_hasTrainer)
        {
            lblPlanMode.Text       = "🔒  Your workout plan is managed by your trainer.";
            btnCreatePlan.Visible  = false;
            _scrollPanel.Top       = 76;
        }
        else
        {
            lblPlanMode.Text       = "Individual mode — you manage your own plan.";
            btnCreatePlan.Visible  = true;
            _scrollPanel.Top       = 128;
        }

        var plan = _workSvc.GetActivePlan(CurrentUser.PersonId);
        int y    = 0;

        if (plan == null)
        {
            _scrollPanel.Controls.Add(new Label
            {
                Text = "No active workout plan.", Left = 0, Top = y, Width = 720, Height = 28,
                Font = UITheme.FontBody(10f), ForeColor = UITheme.TextMuted, BackColor = Color.Transparent
            });
            y += 36;
        }
        else
        {
            btnCreatePlan.Visible = false;
            if (!_hasTrainer)
                lblPlanMode.Text = "🔒  Plan created — original exercises are locked.";

            // Plan header card
            var endsAt = _workSvc.GetPlanEndsAt(CurrentUser.PersonId);
            var header  = new RoundedPanel { Left = 0, Top = y, Width = 740, Height = endsAt.HasValue ? 86 : 72 };
            header.Controls.Add(new Label
            {
                Text = plan.PlanName, Left = UITheme.S16, Top = 10, Width = 500, Height = 26,
                Font = UITheme.FontHeading(15f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
            });
            header.Controls.Add(new Label
            {
                Text = $"Goal: {plan.GoalName}  •  {plan.DurationWeeks} weeks  •  Created {plan.CreatedAt:dd MMM yyyy}",
                Left = UITheme.S16, Top = 38, Width = 700, Height = 18,
                Font = UITheme.FontBody(9f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
            });
            if (endsAt.HasValue)
            {
                header.Controls.Add(new Label
                {
                    Text = $"⏳  Plan expires: {endsAt.Value:dd MMM yyyy}",
                    Left = UITheme.S16, Top = 58, Width = 500, Height = 18,
                    Font = UITheme.FontBody(8.5f), ForeColor = UITheme.Gold, BackColor = Color.Transparent
                });
            }
            _scrollPanel.Controls.Add(header);
            y += header.Height + 12;

            // Per-day cards
            var confirmedToday = _confirmSvc.GetConfirmedToday(CurrentUser.PersonId);
            string[] dayNames  = { "", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            var byDay = plan.Exercises.GroupBy(e => e.DayOfWeek).OrderBy(g => g.Key);

            foreach (var group in byDay)
            {
                string dayName  = group.Key >= 1 && group.Key <= 7 ? dayNames[group.Key] : $"Day {group.Key}";
                var exercises   = group.OrderBy(e => e.OrderInDay).ToList();
                int dayKey      = group.Key;
                int exCount     = exercises.Count;
                int cardHeight  = 28 + exCount * 44 + 36 + exCount * 36 + 50;
                var dayCard     = new RoundedPanel { Left = 0, Top = y, Width = 740, Height = cardHeight, ShowBorder = true };

                dayCard.Controls.Add(new Label
                {
                    Text = dayName, Left = UITheme.S16, Top = 6, Width = 300, Height = 20,
                    Font = UITheme.FontSemiBold(10f), ForeColor = UITheme.Gold, BackColor = Color.Transparent
                });
                dayCard.Controls.Add(new Label
                {
                    Text = $"{exCount} exercise(s)", Left = 320, Top = 8, Width = 200, Height = 16,
                    Font = UITheme.FontSmall(8f), ForeColor = UITheme.TextMuted, BackColor = Color.Transparent
                });

                int ey = 28;
                foreach (var ex in exercises)
                {
                    bool locked  = !ex.IsUserAdded;
                    string lockIcon = locked ? "🔒 " : "✏️ ";
                    string vol   = ex.Reps.HasValue ? $"{ex.Sets} × {ex.Reps} reps" : $"{ex.Sets} × {ex.Seconds}s";
                    string line  = $"{lockIcon}{ex.OrderInDay}. {ex.ExerciseName}  —  {vol}  |  Rest {ex.RestSeconds}s  |  {ex.MuscleGroup}";
                    dayCard.Controls.Add(new Label
                    {
                        Text = line, Left = 8, Top = ey, Width = 710, Height = 38,
                        Font = UITheme.FontBody(9.5f),
                        ForeColor = locked ? UITheme.TextSecondary : UITheme.TextPrimary,
                        BackColor = locked ? Color.FromArgb(12, 180, 150, 50) : Color.FromArgb(12, 100, 200, 100)
                    });
                    ey += 44;
                }

                dayCard.Controls.Add(new Label
                {
                    Text = "  Today's Session", Left = 8, Top = ey, Width = 720, Height = 28,
                    Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextPrimary,
                    BackColor = Color.FromArgb(20, UITheme.Gold)
                });
                ey += 30;

                foreach (var ex in exercises)
                {
                    bool doneToday = confirmedToday.Contains(ex.PlanExerciseId);
                    string vol     = ex.Reps.HasValue ? $"{ex.Sets}×{ex.Reps} reps" : $"{ex.Sets}×{ex.Seconds}s";
                    var rowPanel   = new Panel
                    {
                        Left = 8, Top = ey, Width = 718, Height = 34,
                        BackColor = doneToday ? Color.FromArgb(20, 50, 200, 80) : Color.FromArgb(8, 180, 180, 180)
                    };
                    rowPanel.Controls.Add(new Label
                    {
                        Text = $"{ex.ExerciseName}  ({vol})", Left = 8, Top = 7, Width = 440, Height = 20,
                        Font = UITheme.FontBody(9f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
                    });
                    var btnDone = new GoldButton
                    {
                        Text    = doneToday ? "✅  Done" : "✔ Completed",
                        Left    = 455, Top = 4, Width = 130, Height = 26,
                        Style   = doneToday ? GoldButton.ButtonStyle.Ghost : GoldButton.ButtonStyle.Gold,
                        Enabled = !doneToday,
                        Tag     = ex.PlanExerciseId
                    };
                    if (!doneToday)
                    {
                        int capturedId = ex.PlanExerciseId;
                        btnDone.Click += (_, _) => { _confirmSvc.Confirm(CurrentUser.PersonId, capturedId); LoadWorkoutData(); };
                    }
                    rowPanel.Controls.Add(btnDone);
                    dayCard.Controls.Add(rowPanel);
                    ey += 36;
                }

                if (!_hasTrainer)
                {
                    int capturedDay    = dayKey;
                    int capturedPlanId = plan.PlanId;
                    var btnAdd = new GoldButton
                    {
                        Text  = $"+ Add Exercise to {dayName}",
                        Left  = 8, Top = ey + 4, Width = 260, Height = 34,
                        Style = GoldButton.ButtonStyle.Ghost
                    };
                    btnAdd.Click += (_, _) =>
                    {
                        using var dlg = new AddExerciseToPlanDialog(capturedPlanId, capturedDay);
                        if (dlg.ShowDialog() == DialogResult.OK) LoadWorkoutData();
                    };
                    dayCard.Controls.Add(btnAdd);
                }

                _scrollPanel.Controls.Add(dayCard);
                y += cardHeight + 12;
            }

            if (!byDay.Any())
            {
                _scrollPanel.Controls.Add(new Label
                {
                    Text = "No exercises in this plan yet.", Left = 0, Top = y, Width = 600, Height = 24,
                    Font = UITheme.FontBody(9.5f), ForeColor = UITheme.TextMuted, BackColor = Color.Transparent
                });
                y += 34;
            }
        }

        // Recent session history
        y += 10;
        _scrollPanel.Controls.Add(new Label
        {
            Text = "Recent Session History", Left = 0, Top = y, Width = 300, Height = 22,
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(11f), ForeColor = UITheme.TextPrimary
        });
        y += 28;

        var history = _workSvc.GetHistory(CurrentUser.PersonId, 5);
        if (history.Count == 0)
        {
            _scrollPanel.Controls.Add(new Label
            {
                Text = "No workout sessions logged yet.", Left = 0, Top = y, Width = 500, Height = 20,
                Font = UITheme.FontBody(9.5f), ForeColor = UITheme.TextMuted, BackColor = Color.Transparent
            });
        }
        else
        {
            foreach (var sess in history)
            {
                var sessCard = new RoundedPanel { Left = 0, Top = y, Width = 740, Height = 56, ShowBorder = true };
                sessCard.Controls.Add(new Label
                {
                    Text = $"💪  Workout Session — {sess.SessionDate:dd MMM yyyy HH:mm}",
                    Left = 12, Top = 8, Width = 500, Height = 20,
                    Font = UITheme.FontSemiBold(9.5f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
                });
                sessCard.Controls.Add(new Label
                {
                    Text = $"{sess.DurationMinutes} mins  •  Burned {sess.TotalCalories:F0} kcal  •  Notes: {sess.Notes ?? "None"}",
                    Left = 16, Top = 28, Width = 680, Height = 18,
                    Font = UITheme.FontBody(9f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
                });
                _scrollPanel.Controls.Add(sessCard);
                y += 66;
            }
        }

        _scrollPanel.AutoScrollMinSize = new Size(740, y + 40);
    }

    private void CreatePlanClick()
    {
        new AssignPlanDialog(CurrentUser, CurrentUser).ShowDialog();
        LoadWorkoutData();
    }
}

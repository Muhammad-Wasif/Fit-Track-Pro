using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainer;

/// <summary>
/// Opened by TraineeManagementForm when trainer double-clicks a trainee.
/// Shows the trainee's active workout plan. Original exercises are read-only (🔒).
/// Trainer can append new exercises to any day. Trainee cannot edit.
/// Also shows the trainee's daily confirmation status for today.
/// </summary>
public class TrainerTraineePlanForm : Form
{
    private readonly Person _trainer;
    private readonly Person _trainee;
    private readonly WorkoutService           _workSvc    = new();
    private readonly DailyConfirmationService  _confirmSvc = new();

    private Panel      _scrollPanel = null!;
    private Label      _lblStatus   = null!;

    public TrainerTraineePlanForm() : this(
        new FitTrack.Models.Trainer  { FullName = "Trainer Designer", Role = "Trainer" },
        new FitTrack.Models.Trainee  { FullName = "Trainee Designer", Role = "Trainee" })
    { }

    public TrainerTraineePlanForm(Person trainer, Person trainee)
    {
        _trainer = trainer;
        _trainee = trainee;
        Build();
    }

    private void Build()
    {
        Text            = $"Workout Plan — {_trainee.FullName}";
        Size            = new Size(860, 750);
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = UITheme.BgPage;
        FormBorderStyle = FormBorderStyle.Sizable;
        DoubleBuffered  = true;

        // ── Top strip ─────────────────────────────────────────
        var topStrip = new Panel
        {
            Dock = DockStyle.Top, Height = 64, BackColor = UITheme.BgSidebar
        };
        topStrip.Controls.Add(new Label
        {
            Text      = $"💪  {_trainee.FullName}'s Workout Plan",
            Left = 20, Top = 0, Width = 600, Height = 64,
            Font      = UITheme.FontHeading(14f),
            ForeColor = UITheme.Gold,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var btnRefresh = new GoldButton
        {
            Text = "🔄 Refresh", Left = 640, Top = 14, Width = 110, Height = 36,
            Style = GoldButton.ButtonStyle.Ghost
        };
        btnRefresh.Click += (_, _) => LoadPlan();
        topStrip.Controls.Add(btnRefresh);

        // Status label
        _lblStatus = new Label
        {
            Left = 20, Top = 64, Width = 800, Height = 22,
            Font = UITheme.FontBody(9f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
        };

        // Scrollable content
        _scrollPanel = new Panel
        {
            Left = 20, Top = 92, Width = 800, Height = 580,
            AutoScroll = true, BackColor = Color.Transparent
        };

        this.Controls.Add(topStrip);
        this.Controls.Add(_lblStatus);
        this.Controls.Add(_scrollPanel);

        Resize += (_, _) =>
        {
            _scrollPanel.Width  = this.ClientSize.Width - 40;
            _scrollPanel.Height = this.ClientSize.Height - 100;
        };

        LoadPlan();
    }

    private void LoadPlan()
    {
        _scrollPanel.Controls.Clear();

        // Expire stale plans
        _workSvc.CheckAndExpirePlans(_trainee.PersonId);

        var plan = _workSvc.GetActivePlan(_trainee.PersonId);
        int y = 0;

        if (plan == null)
        {
            _lblStatus.Text     = "No active plan. Assign one from the Trainees page.";
            _lblStatus.ForeColor = UITheme.Red;

            _scrollPanel.Controls.Add(new Label
            {
                Text = "This trainee has no active workout plan.", Left = 0, Top = y, Width = 700, Height = 30,
                Font = UITheme.FontBody(10f), ForeColor = UITheme.TextMuted, BackColor = Color.Transparent
            });
            return;
        }

        var endsAt = _workSvc.GetPlanEndsAt(_trainee.PersonId);
        _lblStatus.Text      = $"Plan: {plan.PlanName}  •  {plan.GoalName}  •  {plan.DurationWeeks} weeks" +
                               (endsAt.HasValue ? $"  •  Expires {endsAt.Value:dd MMM yyyy}" : "");
        _lblStatus.ForeColor = UITheme.TextSecondary;

        // Confirmation status for trainee today
        var confirmedToday = _confirmSvc.GetConfirmedToday(_trainee.PersonId);
        string[] dayNames  = { "", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        // Group exercises by day for quick lookup
        var byDay = plan.Exercises
            .GroupBy(e => e.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.OrderInDay).ToList());

        // ── Show ALL 7 days so trainer can add to any day ──────
        for (int dayKey = 1; dayKey <= 7; dayKey++)
        {
            string dayName  = dayNames[dayKey];
            bool   hasExercises = byDay.TryGetValue(dayKey, out var exercises);
            exercises ??= new List<WorkoutPlanExercise>();

            int exCount    = exercises.Count;
            bool isRestDay = !hasExercises;

            // Card height: day header(32) + exercises(44 each) + confirmation header(30) + rows(36 each) + add btn(50) + padding
            int cardHeight = isRestDay
                ? 56   // compact rest-day card: just header + add button
                : 32 + exCount * 44 + 36 + exCount * 36 + 54;

            var dayCard = new RoundedPanel
            {
                Left      = 0, Top = y,
                Width     = 780,
                Height    = cardHeight,
                ShowBorder = true
            };

            // Day name label
            dayCard.Controls.Add(new Label
            {
                Text = dayName,
                Left = 16, Top = 6, Width = 200, Height = 22,
                Font      = UITheme.FontSemiBold(10f),
                ForeColor = isRestDay ? UITheme.TextMuted : UITheme.Gold,
                BackColor = Color.Transparent
            });

            // Exercise count / rest-day badge
            dayCard.Controls.Add(new Label
            {
                Text = isRestDay ? "— rest day / no exercises yet" : $"{exCount} exercise(s)",
                Left = 230, Top = 8, Width = 300, Height = 18,
                Font      = UITheme.FontSmall(8f),
                ForeColor = UITheme.TextMuted,
                BackColor = Color.Transparent
            });

            int ey = 32;

            if (!isRestDay)
            {
                // Exercise list (read-only for original, editable icon for user-added)
                foreach (var ex in exercises)
                {
                    bool locked   = !ex.IsUserAdded;
                    string lockIcon = locked ? "🔒 " : "✏️ ";
                    string vol    = ex.Reps.HasValue ? $"{ex.Sets}×{ex.Reps} reps" : $"{ex.Sets}×{ex.Seconds}s";
                    string line   = $"{lockIcon}{ex.OrderInDay}. {ex.ExerciseName}  —  {vol}  |  Rest {ex.RestSeconds}s  |  {ex.MuscleGroup}";

                    dayCard.Controls.Add(new Label
                    {
                        Text      = line,
                        Left = 8, Top = ey, Width = 760, Height = 38,
                        Font      = UITheme.FontBody(9.5f),
                        ForeColor = locked ? UITheme.TextSecondary : UITheme.TextPrimary,
                        BackColor = locked
                            ? Color.FromArgb(12, 180, 150, 50)
                            : Color.FromArgb(12, 100, 200, 100)
                    });
                    ey += 44;
                }

                // Trainee daily confirmation status (read-only view for trainer)
                dayCard.Controls.Add(new Label
                {
                    Text      = "  Trainee's Today Confirmation",
                    Left = 8, Top = ey, Width = 760, Height = 28,
                    Font      = UITheme.FontSemiBold(8.5f),
                    ForeColor = UITheme.TextPrimary,
                    BackColor = Color.FromArgb(20, UITheme.Gold)
                });
                ey += 30;

                foreach (var ex in exercises)
                {
                    bool doneToday = confirmedToday.Contains(ex.PlanExerciseId);
                    string vol     = ex.Reps.HasValue ? $"{ex.Sets}×{ex.Reps} reps" : $"{ex.Sets}×{ex.Seconds}s";

                    var rowPanel = new Panel
                    {
                        Left = 8, Top = ey, Width = 758, Height = 34,
                        BackColor = doneToday
                            ? Color.FromArgb(20, 50, 200, 80)
                            : Color.FromArgb(8, 180, 180, 180)
                    };
                    rowPanel.Controls.Add(new Label
                    {
                        Text = $"{ex.ExerciseName}  ({vol})",
                        Left = 8, Top = 7, Width = 500, Height = 20,
                        Font = UITheme.FontBody(9f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
                    });
                    rowPanel.Controls.Add(new Label
                    {
                        Text      = doneToday ? "✅  Confirmed" : "⬜  Not Done",
                        Left = 520, Top = 7, Width = 130, Height = 20,
                        Font      = UITheme.FontSemiBold(8.5f),
                        ForeColor = doneToday ? UITheme.Success : UITheme.TextMuted,
                        BackColor = Color.Transparent
                    });
                    dayCard.Controls.Add(rowPanel);
                    ey += 36;
                }
            }

            // ── Add Exercise button — TRAINER CAN ALWAYS ADD TO ANY DAY ──
            int capturedDay    = dayKey;
            int capturedPlanId = plan.PlanId;
            var btnAdd = new GoldButton
            {
                Text  = isRestDay
                    ? $"+ Add First Exercise to {dayName}"
                    : $"+ Add Exercise to {dayName}",
                Left  = 8,
                Top   = isRestDay ? 24 : ey + 4,
                Width = isRestDay ? 310 : 280,
                Height = 34,
                Style = GoldButton.ButtonStyle.Ghost
            };
            btnAdd.Click += (_, _) =>
            {
                using var dlg = new AddExerciseToPlanDialog(capturedPlanId, capturedDay);
                if (dlg.ShowDialog() == DialogResult.OK)
                    LoadPlan();
            };
            dayCard.Controls.Add(btnAdd);

            _scrollPanel.Controls.Add(dayCard);
            y += cardHeight + 12;
        }

        _scrollPanel.AutoScrollMinSize = new Size(780, y + 40);
    }
}

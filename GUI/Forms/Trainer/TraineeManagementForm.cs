using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainer;

public class TraineeManagementForm : AppShell
{
    private readonly UserService _userSvc   = new();
    private readonly WorkoutService _workSvc = new();
    private readonly GoalService _goalSvc   = new();

    // Content controls
    private Label lblHeading       = null!;
    private DataGridView _dgvTrainees = null!;
    private Panel _detailPanel     = null!;
    private Label _lblDetail       = null!;
    private GoldButton btnRemove   = null!;
    private GoldButton btnAssignW  = null!;
    private GoldButton btnEditPlan = null!;

    private Person? _selected;
    private List<Person> _traineeList = new();

    public TraineeManagementForm() : this(new FitTrack.Models.Trainer { FullName = "Trainer Designer", Role = "Trainer" }) { }

    public TraineeManagementForm(Person user) : base(user, "My Trainees")
    {
        // Nav buttons
        AddNavButton("🏠", "Dashboard",   "dashboard").Click += (_, _) => NavigateToForm<TrainerDashboardForm>();
        AddNavButton("👤", "Profile",     "profile"  ).Click += (_, _) => NavigateToForm<TrainerProfileForm>();
        AddNavButton("👥", "My Trainees", "trainees" ).Click += (_, _) => NavigateToForm<TraineeManagementForm>();
        AddNavButton("💪", "Exercises",   "exercises").Click += (_, _) => NavigateToForm<TrainerExerciseForm>();
        AddNavButton("🥗", "Nutrition",   "nutrition").Click += (_, _) => NavigateToForm<TrainerNutritionForm>();
        AddNavButton("⚙️", "Settings",   "settings" ).Click += (_, _) => NavigateToForm<TrainerSettingsForm>();
        SetActiveNav("trainees");

        BuildContent();

        _dgvTrainees.SelectionChanged += (_, _) => ShowDetail();
        _dgvTrainees.CellDoubleClick  += (_, e) => { if (e.RowIndex >= 0) OpenTraineePlan(); };
        btnRemove.Click   += (_, _) => RemoveTrainee();
        btnAssignW.Click  += (_, _) => AssignWorkoutPlan();
        btnEditPlan.Click += (_, _) => OpenTraineePlan();

        LoadTrainees();

        Resize += (_, _) =>
        {
            int half = Math.Max((_contentArea.Width - 30) / 2, 300);
            if (_dgvTrainees != null) { _dgvTrainees.Width = half; _dgvTrainees.Height = _contentArea.Height - 170; }
            if (_detailPanel != null) { _detailPanel.Left = half + 20; _detailPanel.Width = half; _detailPanel.Height = _contentArea.Height - 170; }
            UITheme.ResponsiveReflow(_contentArea);
        };
    }

    private void BuildContent()
    {
        lblHeading = new Label
        {
            Text      = "My Trainees",
            Location  = new Point(UITheme.S24, UITheme.S24),
            Size      = new Size(500, 36),
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        };

        _dgvTrainees = new DataGridView
        {
            Location = new Point(UITheme.S24, 70),
            Size     = new Size(440, 500)
        };
        UITheme.StyleDataGridView(_dgvTrainees);

        var colName = new DataGridViewTextBoxColumn { HeaderText = "Name",     DataPropertyName = "FullName", Width = 160 };
        var colUser = new DataGridViewTextBoxColumn { HeaderText = "Username", DataPropertyName = "Username", Width = 120 };
        var colGoal = new DataGridViewTextBoxColumn { HeaderText = "Goal",     DataPropertyName = "GoalId",   Width = 100 };
        _dgvTrainees.Columns.AddRange(new DataGridViewColumn[] { colName, colUser, colGoal });

        _detailPanel = new Panel
        {
            Location = new Point(488, 70),
            Size     = new Size(440, 500)
        };

        _lblDetail = new Label
        {
            Text      = "Select a trainee to view details.",
            Location  = new Point(16, 16),
            Size      = new Size(408, 468),
            Font      = UITheme.FontBody(9.5f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        };
        _detailPanel.Controls.Add(_lblDetail);

        btnRemove = new GoldButton
        {
            Text     = "Remove from My List",
            Location = new Point(488, 584),
            Size     = new Size(220, 40),
            Style    = GoldButton.ButtonStyle.Red
        };

        btnAssignW = new GoldButton
        {
            Text     = "Assign Workout Plan",
            Location = new Point(488, 634),
            Size     = new Size(220, 40)
        };

        btnEditPlan = new GoldButton
        {
            Text     = "✏️ Edit Trainee Plan",
            Location = new Point(488, 684),
            Size     = new Size(220, 40),
            Style    = GoldButton.ButtonStyle.Ghost,
            Enabled  = false
        };

        var lblHint = new Label
        {
            Text      = "💡 Double-click a trainee to open their plan",
            Location  = new Point(UITheme.S24, 580),
            Size      = new Size(440, 20),
            Font      = UITheme.FontSmall(8f),
            ForeColor = UITheme.TextMuted,
            BackColor = Color.Transparent
        };

        _contentArea.Controls.AddRange(new Control[]
        {
            lblHeading, _dgvTrainees, _detailPanel,
            btnRemove, btnAssignW, btnEditPlan, lblHint
        });
    }

    private void LoadTrainees()
    {
        _dgvTrainees.Rows.Clear();
        _traineeList = _userSvc.GetTraineesByTrainer(CurrentUser.PersonId);
        foreach (var t in _traineeList)
            _dgvTrainees.Rows.Add(t.FullName, t.Username,
                t.GoalId.HasValue
                    ? _goalSvc.GetById(t.GoalId.Value)?.GoalName ?? "—"
                    : "—");
    }

    private void ShowDetail()
    {
        if (_dgvTrainees.CurrentRow == null) return;
        int idx = _dgvTrainees.CurrentRow.Index;
        if (idx < 0 || idx >= _traineeList.Count) return;
        _selected = _traineeList[idx];

        var (bmi, cat, bmr, tdee, target) = _goalSvc.GetMetrics(_selected.PersonId);
        _lblDetail.Text =
            $"Name:      {_selected.FullName}\n" +
            $"Username:  {_selected.Username}\n" +
            $"Email:     {_selected.Email}\n" +
            $"Gender:    {_selected.Gender}\n" +
            $"Age:       {_selected.Age}\n\n" +
            $"Height:    {_selected.HeightCm} cm\n" +
            $"Weight:    {_selected.WeightKg} kg\n" +
            $"BMI:       {bmi:F1} ({cat})\n" +
            $"BMR:       {bmr:F0} kcal\n" +
            $"TDEE:      {tdee:F0} kcal\n" +
            $"Target:    {target:F0} kcal\n\n" +
            $"Goal:      {(_selected.GoalId.HasValue ? _goalSvc.GetById(_selected.GoalId.Value)?.GoalName ?? "None" : "None")}";

        var activePlan = _workSvc.GetActivePlan(_selected.PersonId);
        if (activePlan != null)
        {
            btnAssignW.Enabled  = false;
            btnAssignW.Text     = "Plan Already Assigned";
            btnEditPlan.Enabled = true;
        }
        else
        {
            btnAssignW.Enabled  = true;
            btnAssignW.Text     = "Assign Workout Plan";
            btnEditPlan.Enabled = false;
        }
    }

    private void OpenTraineePlan()
    {
        if (_selected == null)
        {
            MessageBox.Show("Select a trainee first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var planForm = new TrainerTraineePlanForm(CurrentUser, _selected);
        planForm.ShowDialog(this);
        ShowDetail();
    }

    private void RemoveTrainee()
    {
        if (_selected == null) return;
        var confirm = MessageBox.Show(
            $"Remove {_selected.FullName} from your trainee list?",
            "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        new UserService().RemoveTraineeFromTrainer(_selected.PersonId);
        _selected = null;
        LoadTrainees();
        _lblDetail.Text = "Select a trainee to view details.";
    }

    private void AssignWorkoutPlan()
    {
        if (_selected == null)
        {
            MessageBox.Show("Select a trainee first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        new AssignPlanDialog(CurrentUser, _selected).ShowDialog();
    }
}

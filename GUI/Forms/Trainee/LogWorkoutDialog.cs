using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;

namespace FitTrack.GUI.Forms.Trainee;

public class LogWorkoutDialog : Form
{
    private readonly WorkoutService _workouts = new();
    private readonly Person _user;
    private readonly WorkoutPlan? _activePlan;

    private ComboBox _cbExercise = null!;
    private StyledTextBox _tbReps = null!;
    private StyledTextBox _tbWeight = null!;
    private StyledTextBox _tbDuration = null!;
    private StyledTextBox _tbNotes = null!;
    private ListBox _lbSets = null!;
    private Label _lblMsg = null!;
    private Label _lblPlan = null!;

    private readonly List<(int exerciseId, int setNumber, int? actualReps, int? actualSeconds, double? weightKg)> _sets = new();
    private int _setCount = 0;

    // Parameterless constructor for Visual Studio Designer support
    public LogWorkoutDialog() : this(new FitTrack.Models.Trainee { FullName = "Trainee Designer", Role = "Trainee" })
    {
    }

    public LogWorkoutDialog(Person user)
    {
        _user = user;
        _activePlan = _workouts.GetActivePlan(_user.PersonId);

        Text = "Log Workout Session";
        Size = new Size(680, 520);
        MinimumSize = new Size(680, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.BgPage;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        
        Build();
    }

    private void Build()
    {
        // Title banner
        var titlePanel = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UITheme.BgSidebar };
        Controls.Add(titlePanel);

        var lblTitle = new Label
        {
            Text = "🏋️  Log Workout Session",
            Left = 16, Top = 12, Width = 300, Height = 28,
            Font = UITheme.FontHeading(14f), ForeColor = UITheme.White, BackColor = Color.Transparent
        };
        titlePanel.Controls.Add(lblTitle);

        string planName = _activePlan != null ? $"Active Plan: {_activePlan.PlanName}" : "No Active Plan (Individual Session)";
        _lblPlan = new Label
        {
            Text = planName,
            Left = 18, Top = 40, Width = 600, Height = 18,
            Font = UITheme.FontBody(8.5f), ForeColor = UITheme.Gold, BackColor = Color.Transparent
        };
        titlePanel.Controls.Add(_lblPlan);

        // Main split-screen flow
        var mainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(12),
            BackColor = Color.Transparent
        };
        mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        Controls.Add(mainContainer);

        // Bring TableLayoutPanel behind the Docked header
        mainContainer.BringToFront();

        // LEFT COLUMN: Add Sets
        var leftPanel = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(6), ShowBorder = true };
        mainContainer.Controls.Add(leftPanel, 0, 0);

        leftPanel.Controls.Add(new Label
        {
            Text = "Log Set details", Left = 16, Top = 16, Width = 200, Height = 20,
            Font = UITheme.FontSemiBold(11f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
        });

        int ly = 50;
        leftPanel.Controls.Add(new Label
        {
            Text = "Select Exercise", Left = 16, Top = ly, Width = 260, Height = 16,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
        });
        _cbExercise = new ComboBox { Left = 16, Top = ly + 18, Width = 268, DropDownStyle = ComboBoxStyle.DropDownList };
        UITheme.StyleComboBox(_cbExercise);
        foreach (var ex in _workouts.GetExercises()) _cbExercise.Items.Add(ex);
        if (_cbExercise.Items.Count > 0) _cbExercise.SelectedIndex = 0;
        _cbExercise.DisplayMember = "Name";
        leftPanel.Controls.Add(_cbExercise);
        ly += 62;

        leftPanel.Controls.Add(new Label
        {
            Text = "Weight (kg)", Left = 16, Top = ly, Width = 110, Height = 16,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
        });
        _tbWeight = new StyledTextBox { Left = 16, Top = ly + 18, Width = 120, Height = 40 };
        leftPanel.Controls.Add(_tbWeight);

        leftPanel.Controls.Add(new Label
        {
            Text = "Reps completed", Left = 154, Top = ly, Width = 110, Height = 16,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
        });
        _tbReps = new StyledTextBox { Left = 154, Top = ly + 18, Width = 130, Height = 40 };
        leftPanel.Controls.Add(_tbReps);
        ly += 68;

        var btnAddSet = new GoldButton { Text = "+ Add Set to Log", Left = 16, Top = ly, Width = 268, Height = 42 };
        btnAddSet.Click += AddSet_Click;
        leftPanel.Controls.Add(btnAddSet);
        ly += 54;

        _lblMsg = new Label { Left = 16, Top = ly, Width = 268, Height = 64, AutoSize = false, BackColor = Color.Transparent, Font = UITheme.FontBody(8.5f) };
        leftPanel.Controls.Add(_lblMsg);

        // RIGHT COLUMN: Session Details & List
        var rightPanel = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(6), ShowBorder = true };
        mainContainer.Controls.Add(rightPanel, 1, 0);

        rightPanel.Controls.Add(new Label
        {
            Text = "Logged Sets List", Left = 16, Top = 16, Width = 200, Height = 20,
            Font = UITheme.FontSemiBold(11f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
        });

        _lbSets = new ListBox
        {
            Left = 16, Top = 40, Width = 278, Height = 120,
            BorderStyle = BorderStyle.FixedSingle, Font = UITheme.FontBody(9f), ForeColor = UITheme.TextPrimary
        };
        rightPanel.Controls.Add(_lbSets);

        int ry = 170;
        rightPanel.Controls.Add(new Label
        {
            Text = "Session Duration (Minutes)", Left = 16, Top = ry, Width = 260, Height = 16,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
        });
        _tbDuration = new StyledTextBox { Left = 16, Top = ry + 18, Width = 278, Height = 40 };
        rightPanel.Controls.Add(_tbDuration);
        ry += 62;

        rightPanel.Controls.Add(new Label
        {
            Text = "Session Notes", Left = 16, Top = ry, Width = 260, Height = 16,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
        });
        _tbNotes = new StyledTextBox { Left = 16, Top = ry + 18, Width = 278, Height = 40 };
        rightPanel.Controls.Add(_tbNotes);
        ry += 68;

        var btnSubmit = new GoldButton { Text = "🏋️ Save Session", Left = 16, Top = ry, Width = 278, Height = 44 };
        btnSubmit.Click += LogSession_Click;
        rightPanel.Controls.Add(btnSubmit);
    }

    private void AddSet_Click(object? sender, EventArgs e)
    {
        _lblMsg.Text = "";

        if (_cbExercise.SelectedItem is not Exercise exercise)
        {
            _lblMsg.Text = "Please select an exercise.";
            _lblMsg.ForeColor = UITheme.Red;
            return;
        }

        int? reps = null;
        if (!string.IsNullOrWhiteSpace(_tbReps.Text))
        {
            if (int.TryParse(_tbReps.Text, out int r) && r >= 0)
                reps = r;
            else
            {
                _lblMsg.Text = "Reps must be a valid non-negative number.";
                _lblMsg.ForeColor = UITheme.Red;
                return;
            }
        }

        double? weight = null;
        if (!string.IsNullOrWhiteSpace(_tbWeight.Text))
        {
            if (double.TryParse(_tbWeight.Text, out double w) && w >= 0)
                weight = w;
            else
            {
                _lblMsg.Text = "Weight must be a valid non-negative number.";
                _lblMsg.ForeColor = UITheme.Red;
                return;
            }
        }

        _setCount++;
        _sets.Add((exercise.ExerciseId, _setCount, reps, null, weight));
        _lbSets.Items.Add($"Set {_setCount}: {exercise.Name} — {reps ?? 0} reps @ {weight ?? 0} kg");

        // Flash message
        _lblMsg.Text = $"Added Set {_setCount} successfully!";
        _lblMsg.ForeColor = UITheme.Success;

        // Clear local inputs
        _tbReps.Text = "";
        _tbWeight.Text = "";
    }

    private void LogSession_Click(object? sender, EventArgs e)
    {
        _lblMsg.Text = "";

        if (!int.TryParse(_tbDuration.Text, out int duration) || duration <= 0)
        {
            _lblMsg.Text = "Please enter a valid duration in minutes.";
            _lblMsg.ForeColor = UITheme.Red;
            return;
        }

        if (_sets.Count == 0)
        {
            _lblMsg.Text = "Please add at least one set before saving.";
            _lblMsg.ForeColor = UITheme.Red;
            return;
        }

        var notes = string.IsNullOrWhiteSpace(_tbNotes.Text) ? null : _tbNotes.Text;

        var result = _workouts.LogSession(_user.PersonId, _activePlan?.PlanId, duration, notes, _sets);

        if (result.success)
        {
            MessageBox.Show("Workout session logged successfully!", "Workout Logged", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _lblMsg.Text = result.message;
            _lblMsg.ForeColor = UITheme.Red;
        }
    }
}

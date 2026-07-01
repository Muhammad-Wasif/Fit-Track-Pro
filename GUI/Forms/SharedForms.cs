using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;

namespace FitTrack.GUI.Forms;

// ================================================================
//  CHANGE PASSWORD PANEL  (shared by all roles — spec §19)
// ================================================================
public class ChangePasswordPanel : UserControl
{
    private readonly AuthService  _auth = new();
    private readonly Person       _user;
    private StyledTextBox _tbCurrent = null!;
    private StyledTextBox _tbNew     = null!;
    private StyledTextBox _tbConfirm = null!;
    private Label         _lblMsg    = null!;
    private Label         _lblHint   = null!;

    // Parameterless constructor for Visual Studio Designer support
    public ChangePasswordPanel() : this(new FitTrack.Models.Trainee { FullName = "Designer Mode", Role = "Trainee" })
    {
    }

    public ChangePasswordPanel(Person user)
    {
        _user = user;
        BackColor = UITheme.BgPage;
        DoubleBuffered = true;
        Build();
    }

    private void Build()
    {
        Controls.Add(new Label { Text = "Settings", Left = 0, Top = 0, Width = 500, Height = 36,
            Font = UITheme.FontHeading(20f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent });

        var card = new RoundedPanel { Left = 0, Top = 50, Width = 480, Height = 400 };
        Controls.Add(card);

        card.Controls.Add(new Label { Text = "Change Password", Left = 16, Top = 14, Width = 400, Height = 22,
            Font = UITheme.FontSemiBold(12f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent });

        int lx = 16, y = 46, fw = 420, fh = 42, gap = 78;
        FL(card, "Current Password", lx, y);
        _tbCurrent = TB(card, lx, y + 18, fw, fh); _tbCurrent.IsPassword = true; y += gap;

        FL(card, "New Password", lx, y);
        _tbNew = TB(card, lx, y + 18, fw, fh); _tbNew.IsPassword = true;
        _tbNew.Inner.TextChanged += (_, _) => ShowHint();
        y += gap;

        FL(card, "Confirm New Password", lx, y);
        _tbConfirm = TB(card, lx, y + 18, fw, fh); _tbConfirm.IsPassword = true; y += gap;

        _lblHint = new Label { Left = lx, Top = y - 14, Width = fw, Height = 16,
            Font = UITheme.FontSmall(7.5f), ForeColor = UITheme.TextMuted, BackColor = Color.Transparent };
        card.Controls.Add(_lblHint);

        _lblMsg = new Label { Left = lx, Top = y, Width = fw, Height = 22, BackColor = Color.Transparent, Font = UITheme.FontBody(9f) };
        card.Controls.Add(_lblMsg);

        var btn = new GoldButton { Text = "Change Password", Left = lx, Top = y + 28, Width = 200, Height = 44 };
        btn.Click += (_, _) => DoChange();
        card.Controls.Add(btn);
    }

    private void ShowHint()
    {
        string p = _tbNew.Text;
        if (string.IsNullOrEmpty(p)) { _lblHint.Text = ""; return; }
        var checks = new[]
        {
            ($"Len≥8 {(p.Length >= 8 ? "✓" : "✗")}"),
            ($"Upper {(p.Any(char.IsUpper) ? "✓" : "✗")}"),
            ($"Lower {(p.Any(char.IsLower) ? "✓" : "✗")}"),
            ($"Num {(p.Any(char.IsDigit) ? "✓" : "✗")}"),
            ($"Sym {(p.Any(c => !char.IsLetterOrDigit(c)) ? "✓" : "✗")}")
        };
        _lblHint.Text = string.Join("  ", checks);
    }

    private void DoChange()
    {
        if (_tbNew.Text != _tbConfirm.Text)
        {
            _lblMsg.Text = "New passwords do not match."; _lblMsg.ForeColor = UITheme.Red; return;
        }
        var (ok, msg) = _auth.ChangePassword(_user.PersonId, _tbCurrent.Text, _tbNew.Text);
        _lblMsg.Text      = ok ? "✓ Password changed successfully." : msg;
        _lblMsg.ForeColor = ok ? UITheme.Success : UITheme.Red;
        if (ok) { _tbCurrent.Text = _tbNew.Text = _tbConfirm.Text = ""; }
    }

    private void FL(Panel p, string text, int x, int y) =>
        p.Controls.Add(new Label { Text = text, Left = x, Top = y, Width = 420, Height = 16,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent });

    private StyledTextBox TB(Panel p, int x, int y, int w, int h)
    {
        var tb = new StyledTextBox { Left = x, Top = y, Width = w, Height = h };
        p.Controls.Add(tb); return tb;
    }
}

// ================================================================
//  ASSIGN PLAN DIALOG
// ================================================================
public class AssignPlanDialog : Form
{
    private readonly WorkoutService _workSvc = new();
    private readonly GoalService    _goalSvc = new();
    private readonly Person _trainer, _trainee;
    private ComboBox      _cbGoal = null!;
    private ComboBox      _cbEx = null!;
    private ComboBox      _cbDayOfWeek = null!;
    private StyledTextBox _tbName = null!;
    private NumericUpDown _nudWeeks = null!;
    private ListBox       _lbAdded = null!;
    private Label         _lblMsg = null!;
    private List<(int exId, int day, int order, int sets, int? reps, int? secs, int rest)> _exercises = new();

    // Parameterless constructor for Visual Studio Designer support
    public AssignPlanDialog() : this(new FitTrack.Models.Trainer { FullName = "Trainer Designer", Role = "Trainer" }, new FitTrack.Models.Trainee { FullName = "Trainee Designer", Role = "Trainee" })
    {
    }

    public AssignPlanDialog(Person trainer, Person trainee)
    {
        _trainer = trainer; _trainee = trainee;
        Text = $"Assign Plan → {trainee.FullName}";
        Size = new Size(620, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Build();
    }

    private void Build()
    {
        int lx = 20, y = 16, fw = 260, fh = 40;

        AddLbl("Plan Name", lx, y);
        _tbName = new StyledTextBox { Left = lx, Top = y + 18, Width = fw, Height = fh };
        Controls.Add(_tbName); y += 64;

        AddLbl("Goal", lx, y);
        _cbGoal = new ComboBox { Left = lx, Top = y + 18, Width = fw, DropDownStyle = ComboBoxStyle.DropDownList };
        UITheme.StyleComboBox(_cbGoal);
        foreach (var g in _goalSvc.GetAll()) _cbGoal.Items.Add(g);
        _cbGoal.DisplayMember = "GoalName"; _cbGoal.SelectedIndex = 0;
        Controls.Add(_cbGoal);

        AddLbl("Duration (weeks)", lx + fw + 20, y);
        _nudWeeks = new NumericUpDown { Left = lx + fw + 20, Top = y + 18, Width = 100, Minimum = 1, Maximum = 52, Value = 4 };
        Controls.Add(_nudWeeks); y += 64;

        // Day of Week Selection Row
        AddLbl("Day of Week", lx, y);
        _cbDayOfWeek = new ComboBox { Left = lx, Top = y + 18, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
        UITheme.StyleComboBox(_cbDayOfWeek);
        _cbDayOfWeek.Items.AddRange(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" });
        _cbDayOfWeek.SelectedIndex = 0;
        Controls.Add(_cbDayOfWeek);

        // Add Exercise Selector
        AddLbl("Add Exercise", lx + 150, y);
        _cbEx = new ComboBox { Left = lx + 150, Top = y + 18, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
        UITheme.StyleComboBox(_cbEx);
        foreach (var ex in _workSvc.GetExercises()) _cbEx.Items.Add(ex);
        _cbEx.DisplayMember = "Name"; _cbEx.SelectedIndex = 0;
        Controls.Add(_cbEx);

        var btnAdd = new GoldButton { Text = "Add", Left = lx + 450, Top = y + 18, Width = 80, Height = 40, Style = GoldButton.ButtonStyle.Ghost };
        btnAdd.Click += (_, _) =>
        {
            if (_cbEx.SelectedItem is Exercise ex)
            {
                int day = _cbDayOfWeek.SelectedIndex + 1; // 1-indexed (1=Monday, 2=Tuesday...)
                string dayName = _cbDayOfWeek.SelectedItem?.ToString() ?? "";
                
                _exercises.Add((ex.ExerciseId, day, _exercises.Count + 1, ex.DefaultSets, ex.DefaultReps, ex.DefaultSecs, 60));
                _lbAdded.Items.Add($"[{dayName}] {ex.Name} — {ex.DefaultSets}×{(ex.DefaultReps.HasValue ? ex.DefaultReps.ToString() : ex.DefaultSecs + "s")}");
            }
        };
        Controls.Add(btnAdd); y += 64;

        _lbAdded = new ListBox { Left = lx, Top = y, Width = 560, Height = 160, BorderStyle = BorderStyle.FixedSingle };
        Controls.Add(_lbAdded); y += 172;

        _lblMsg = new Label { Left = lx, Top = y, Width = 560, Height = 24, BackColor = Color.Transparent, Font = UITheme.FontBody(9f) };
        Controls.Add(_lblMsg); y += 30;

        var btnCreate = new GoldButton { Text = "Create & Assign Plan", Left = lx, Top = y, Width = 260, Height = 44 };
        btnCreate.Click += (_, _) => CreatePlan();
        Controls.Add(btnCreate);
    }

    private void AddLbl(string t, int x, int y) =>
        Controls.Add(new Label { Text = t, Left = x, Top = y, Width = 260, Height = 16,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent });

    private void CreatePlan()
    {
        if (_cbGoal.SelectedItem is not Goal g) return;
        if (_exercises.Count == 0) { _lblMsg.Text = "Add at least one exercise."; _lblMsg.ForeColor = UITheme.Red; return; }
        var (ok, msg, _) = _workSvc.CreatePlan(_trainer.PersonId, _trainee.PersonId, g.GoalId,
            _tbName.Text, (int)_nudWeeks.Value, _exercises);
        _lblMsg.Text      = ok ? "✓ Plan assigned!" : msg;
        _lblMsg.ForeColor = ok ? UITheme.Success : UITheme.Red;
        if (ok) { System.Threading.Thread.Sleep(800); Close(); }
    }
}

// ================================================================
//  ADD EXERCISE TO EXISTING PLAN DIALOG
//  Used by both trainee (no trainer) and trainer (for trainee's plan).
// ================================================================
public class AddExerciseToPlanDialog : Form
{
    private readonly WorkoutService _workSvc = new();

    private readonly int _planId;
    private readonly int _defaultDay;       // pre-selected day (1=Mon…7=Sun)

    private ComboBox      _cbEx      = null!;
    private ComboBox      _cbDay     = null!;
    private NumericUpDown _nudSets   = null!;
    private NumericUpDown _nudReps   = null!;
    private NumericUpDown _nudSecs   = null!;
    private NumericUpDown _nudRest   = null!;
    private RadioButton   _rbReps    = null!;
    private RadioButton   _rbSecs    = null!;
    private Label         _lblMsg    = null!;

    /// <summary>The exercise entry that was just added (set on successful save).</summary>
    public WorkoutPlanExercise? AddedExercise { get; private set; }

    // Parameterless designer support
    public AddExerciseToPlanDialog() : this(0, 1) { }

    public AddExerciseToPlanDialog(int planId, int defaultDayOfWeek)
    {
        _planId     = planId;
        _defaultDay = defaultDayOfWeek;

        Text            = "Add Exercise to Plan";
        Size            = new Size(500, 460);
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = UITheme.BgPage;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        Build();
    }

    private void Build()
    {
        int lx = 24, y = 18, fw = 420;

        // Heading
        Controls.Add(new Label
        {
            Text = "Add Exercise", Left = lx, Top = y, Width = fw, Height = 30,
            Font = UITheme.FontHeading(14f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
        });
        y += 38;

        // Exercise picker
        AddLbl("Exercise", lx, y);
        _cbEx = new ComboBox { Left = lx, Top = y + 18, Width = fw, DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 15 };
        UITheme.StyleComboBox(_cbEx);
        foreach (var ex in _workSvc.GetExercises()) _cbEx.Items.Add(ex);
        _cbEx.DisplayMember = "Name";
        if (_cbEx.Items.Count > 0) _cbEx.SelectedIndex = 0;
        Controls.Add(_cbEx); y += 60;

        // Day of week
        AddLbl("Day of Week", lx, y);
        _cbDay = new ComboBox { Left = lx, Top = y + 18, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
        UITheme.StyleComboBox(_cbDay);
        _cbDay.Items.AddRange(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" });
        _cbDay.SelectedIndex = Math.Max(0, _defaultDay - 1);
        Controls.Add(_cbDay); y += 60;

        // Sets
        AddLbl("Sets", lx, y);
        _nudSets = new NumericUpDown { Left = lx, Top = y + 18, Width = 80, Minimum = 1, Maximum = 20, Value = 3 };
        Controls.Add(_nudSets);

        // Volume type toggle
        _rbReps = new RadioButton { Text = "Reps", Left = lx + 100, Top = y + 20, Width = 70, Checked = true, ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent };
        _rbSecs = new RadioButton { Text = "Seconds", Left = lx + 180, Top = y + 20, Width = 90, ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent };
        Controls.Add(_rbReps);
        Controls.Add(_rbSecs); y += 60;

        // Reps
        AddLbl("Reps", lx, y);
        _nudReps = new NumericUpDown { Left = lx, Top = y + 18, Width = 80, Minimum = 1, Maximum = 200, Value = 10 };
        Controls.Add(_nudReps);

        // Seconds
        AddLbl("Seconds", lx + 110, y);
        _nudSecs = new NumericUpDown { Left = lx + 110, Top = y + 18, Width = 80, Minimum = 5, Maximum = 3600, Value = 30, Enabled = false };
        Controls.Add(_nudSecs);

        // Toggle reps/secs
        _rbReps.CheckedChanged += (_, _) =>
        {
            _nudReps.Enabled = _rbReps.Checked;
            _nudSecs.Enabled = !_rbReps.Checked;
        };
        _rbSecs.CheckedChanged += (_, _) =>
        {
            _nudReps.Enabled = !_rbSecs.Checked;
            _nudSecs.Enabled = _rbSecs.Checked;
        };
        y += 60;

        // Rest seconds
        AddLbl("Rest (seconds)", lx, y);
        _nudRest = new NumericUpDown { Left = lx, Top = y + 18, Width = 80, Minimum = 0, Maximum = 600, Value = 60 };
        Controls.Add(_nudRest); y += 60;

        // Message
        _lblMsg = new Label { Left = lx, Top = y, Width = fw, Height = 22, BackColor = Color.Transparent, Font = UITheme.FontBody(9f) };
        Controls.Add(_lblMsg); y += 28;

        // Buttons
        var btnSave   = new GoldButton { Text = "✔ Add Exercise", Left = lx,         Top = y, Width = 180, Height = 44 };
        var btnCancel = new GoldButton { Text = "Cancel",          Left = lx + 190,   Top = y, Width = 100, Height = 44,
            Style = GoldButton.ButtonStyle.Ghost };

        btnSave.Click   += (_, _) => Save();
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
    }

    private void AddLbl(string t, int x, int y) =>
        Controls.Add(new Label
        {
            Text = t, Left = x, Top = y, Width = 260, Height = 16,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
        });

    private void Save()
    {
        if (_cbEx.SelectedItem is not Exercise ex)
        {
            _lblMsg.Text = "Please select an exercise."; _lblMsg.ForeColor = UITheme.Red; return;
        }

        int day  = _cbDay.SelectedIndex + 1;
        int sets = (int)_nudSets.Value;
        int? reps = _rbReps.Checked ? (int?)_nudReps.Value : null;
        int? secs = _rbSecs.Checked ? (int?)_nudSecs.Value : null;
        int rest  = (int)_nudRest.Value;

        var (ok, msg, pe) = _workSvc.AddExerciseToPlan(_planId, ex.ExerciseId, day, sets, reps, secs, rest);
        _lblMsg.Text      = ok ? "✓ Exercise added!" : msg;
        _lblMsg.ForeColor = ok ? UITheme.Success : UITheme.Red;
        if (ok)
        {
            AddedExercise = pe;
            DialogResult  = DialogResult.OK;
            System.Threading.Thread.Sleep(500);
            Close();
        }
    }
}


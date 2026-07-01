using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainee;

public class TraineeNutritionForm : AppShell
{
    // ── Services ─────────────────────────────────────────────────
    private readonly NutritionService _nutSvc     = new();
    private readonly bool             _hasTrainer;

    // ── Content controls ─────────────────────────────────────────
    private Label        lblHeading   = null!;
    private DataGridView _dgvSearch   = null!;
    private DataGridView _dgvLog      = null!;
    private StyledTextBox _tbSearch   = null!;
    private StyledTextBox _tbGrams    = null!;
    private ComboBox     _cbMeal      = null!;
    private Label        _lblWarn     = null!;
    private Label        _lblSummary  = null!;
    private GoldButton   btnSearch    = null!;
    private GoldButton   btnLog       = null!;
    private List<FoodItem> _results   = new();

    // ── Designer support ─────────────────────────────────────────
    public TraineeNutritionForm() : this(new Models.Trainee { FullName = "Trainee Designer", Role = "Trainee" }) { }

    public TraineeNutritionForm(Person user) : base(user, "Nutrition Tracker")
    {
        _hasTrainer = user.TrainerId.HasValue;

        // ── Nav ──
        var btnDash = AddNavButton("🏠", "Dashboard", "dashboard");
        var btnProf = AddNavButton("👤", "Profile",   "profile");
        var btnWork = AddNavButton("💪", "Workout",   "workout");
        var btnNut  = AddNavButton("🥗", "Nutrition", "nutrition");
        var btnProg = AddNavButton("📈", "Progress",  "progress");
        var btnSets = AddNavButton("⚙️", "Settings",  "settings");

        btnDash.Click += (_, _) => NavigateToForm<TraineeDashboardForm>();
        btnProf.Click += (_, _) => NavigateToForm<TraineeProfileForm>();
        btnWork.Click += (_, _) => NavigateToForm<TraineeWorkoutForm>();
        btnNut.Click  += (_, _) => NavigateToForm<TraineeNutritionForm>();
        btnProg.Click += (_, _) => NavigateToForm<TraineeProgressForm>();
        btnSets.Click += (_, _) => NavigateToForm<TraineeSettingsForm>();

        SetActiveNav("nutrition");
        BuildContent();
        RefreshLog();

        Resize += (_, _) =>
        {
            int w = _contentArea.Width - 10;
            _dgvSearch.Width = w;
            _dgvLog.Width    = w;
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  BUILD CONTENT
    // ─────────────────────────────────────────────────────────────
    private void BuildContent()
    {
        lblHeading = new Label
        {
            Text = "Nutrition Tracker", Location = new Point(0, 0), Size = new Size(500, 36),
            BackColor = Color.Transparent, Font = UITheme.FontHeading(20f), ForeColor = UITheme.TextPrimary
        };

        int y = 44;
        if (_hasTrainer)
        {
            _contentArea.Controls.Add(new Label
            {
                Text = "🏋️  Trainer assigned — you can still log and edit your own nutrition freely.",
                Location = new Point(0, y), Size = new Size(800, 18), BackColor = Color.Transparent,
                Font = UITheme.FontBody(8.5f), ForeColor = UITheme.TextMuted
            });
            y += 26;
        }

        // Search row
        FL("Search Food", 0, y);
        _tbSearch = new StyledTextBox { Location = new Point(0, y + 18), Size = new Size(320, UITheme.TextBoxHeight) };
        btnSearch = new GoldButton { Text = "Search", Location = new Point(328, y + 18), Size = new Size(100, UITheme.ButtonHeight) };
        btnSearch.Click += (_, _) => DoSearch();
        _tbSearch.Inner.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) DoSearch(); };

        FL("Meal Type", 440, y);
        _cbMeal = new ComboBox
        {
            Location = new Point(440, y + 18), Size = new Size(140, UITheme.TextBoxHeight),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        UITheme.StyleComboBox(_cbMeal);
        _cbMeal.Items.AddRange(new object[] { "Breakfast", "Lunch", "Dinner", "Snack" });
        _cbMeal.SelectedIndex = 0;

        FL("Grams", 592, y);
        _tbGrams = new StyledTextBox { Location = new Point(592, y + 18), Size = new Size(90, UITheme.TextBoxHeight) };

        btnLog = new GoldButton { Text = "Log", Location = new Point(692, y + 18), Size = new Size(70, UITheme.ButtonHeight) };
        btnLog.Click += (_, _) => LogMeal();

        y += 66;

        // Search results grid
        _dgvSearch = new DataGridView { Location = new Point(0, y), Size = new Size(760, 160) };
        _dgvSearch.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Food Name",  Width = 240 });
        _dgvSearch.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "kcal/100g",  Width = 80 });
        _dgvSearch.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Protein",    Width = 70 });
        _dgvSearch.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Carbs",      Width = 70 });
        _dgvSearch.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fat",        Width = 70 });
        UITheme.StyleDataGridView(_dgvSearch);
        y += 168;

        // Warning label
        _lblWarn = new Label
        {
            Location = new Point(0, y), Size = new Size(760, 26),
            BackColor = Color.FromArgb(20, UITheme.Red), ForeColor = UITheme.Red,
            Font = UITheme.FontSemiBold(9f), TextAlign = ContentAlignment.MiddleCenter, Visible = false
        };
        y += 32;

        // Summary
        _lblSummary = new Label
        {
            Location = new Point(0, y), Size = new Size(760, 18),
            BackColor = Color.Transparent, Font = UITheme.FontBody(9f), ForeColor = UITheme.TextSecondary
        };
        y += 26;

        // Today's log title
        _contentArea.Controls.Add(new Label
        {
            Text = "Today's Log", Location = new Point(0, y), Size = new Size(400, 22),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(10f), ForeColor = UITheme.TextPrimary
        });
        y += 28;

        // Log grid
        _dgvLog = new DataGridView { Location = new Point(0, y), Size = new Size(760, 280) };
        _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Time",    Width = 70 });
        _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Meal",    Width = 90 });
        _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Food",    Width = 220 });
        _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Grams",   Width = 70 });
        _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kcal",    Width = 75 });
        _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Protein", Width = 70 });
        _dgvLog.Columns.Add(new DataGridViewButtonColumn { HeaderText = "Del", Text = "✕", UseColumnTextForButtonValue = true, Width = 46 });
        UITheme.StyleDataGridView(_dgvLog);
        _dgvLog.CellClick += (_, e) =>
        {
            if (e.ColumnIndex == _dgvLog.Columns.Count - 1 && e.RowIndex >= 0)
            {
                int logId = (int)_dgvLog.Rows[e.RowIndex].Tag!;
                _nutSvc.DeleteLog(logId);
                RefreshLog();
            }
        };

        // Add all controls
        _contentArea.Controls.AddRange(new Control[]
        {
            lblHeading, _tbSearch, btnSearch, _cbMeal, _tbGrams, btnLog,
            _dgvSearch, _lblWarn, _lblSummary, _dgvLog
        });
    }

    private void FL(string text, int x, int y)
    {
        _contentArea.Controls.Add(new Label
        {
            Text = text, Location = new Point(x, y), Size = new Size(220, 16),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  LOGIC
    // ─────────────────────────────────────────────────────────────
    private void DoSearch()
    {
        _dgvSearch.Rows.Clear();
        _results = _nutSvc.Search(_tbSearch.Text.Trim());
        foreach (var f in _results)
            _dgvSearch.Rows.Add(f.FoodName, f.CaloriesPer100g.ToString("F1"),
                f.ProteinPer100g.ToString("F1"), f.CarbsPer100g.ToString("F1"), f.FatPer100g.ToString("F1"));
    }

    private void LogMeal()
    {
        if (_dgvSearch.CurrentRow == null || _results.Count == 0) return;
        if (!double.TryParse(_tbGrams.Text, out double grams) || grams <= 0) return;
        int idx = _dgvSearch.CurrentRow.Index;
        if (idx < 0 || idx >= _results.Count) return;
        _nutSvc.LogMeal(CurrentUser.PersonId, _results[idx].FoodItemId,
            _cbMeal.SelectedItem!.ToString()!, grams, loggedByPersonId: CurrentUser.PersonId);
        RefreshLog();
    }

    private void RefreshLog()
    {
        if (_dgvLog == null) return;
        _dgvLog.Rows.Clear();
        var (cal, prot, carbs, fat, meals) = _nutSvc.GetDailySummary(CurrentUser.PersonId, DateTime.Today);
        foreach (var m in meals)
        {
            int r = _dgvLog.Rows.Add(m.LoggedAt.ToString("HH:mm"), m.MealType, m.FoodName,
                m.ServingGrams.ToString("F0"), m.Calories.ToString("F0"), m.ProteinG.ToString("F1"));
            _dgvLog.Rows[r].Tag = m.NutritionLogId;
        }
        _lblSummary.Text = $"Total: {cal:F0} kcal  •  Protein {prot:F1}g  •  Carbs {carbs:F1}g  •  Fat {fat:F1}g";
        var (over, target) = _nutSvc.CheckCalorieLimit(CurrentUser.PersonId, cal);
        _lblWarn.Visible = over;
        if (over) _lblWarn.Text = $"⚠  Calorie limit exceeded!  Target: {target:F0} kcal  |  Logged: {cal:F0} kcal";
    }
}

using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainer;

public class TrainerNutritionForm : AppShell
{
    private readonly UserService      _userSvc = new();
    private readonly NutritionService _nutSvc  = new();

    // Content controls
    private Label        lblHeading   = null!;
    private DataGridView _dgvTrainees = null!;
    private DataGridView _dgvFood     = null!;
    private StyledTextBox _tbSearch   = null!;
    private Label        _lblFoodHint = null!;
    private GoldButton   btnSearch    = null!;

    private List<Person>   _allTrainees = new();
    private List<FoodItem> _foodResults = new();

    public TrainerNutritionForm() : this(new FitTrack.Models.Trainer { FullName = "Trainer Designer", Role = "Trainer" }) { }

    public TrainerNutritionForm(Person user) : base(user, "Nutrition")
    {
        // Nav buttons
        AddNavButton("🏠", "Dashboard",   "dashboard").Click += (_, _) => NavigateToForm<TrainerDashboardForm>();
        AddNavButton("👤", "Profile",     "profile"  ).Click += (_, _) => NavigateToForm<TrainerProfileForm>();
        AddNavButton("👥", "My Trainees", "trainees" ).Click += (_, _) => NavigateToForm<TraineeManagementForm>();
        AddNavButton("💪", "Exercises",   "exercises").Click += (_, _) => NavigateToForm<TrainerExerciseForm>();
        AddNavButton("🥗", "Nutrition",   "nutrition").Click += (_, _) => NavigateToForm<TrainerNutritionForm>();
        AddNavButton("⚙️", "Settings",   "settings" ).Click += (_, _) => NavigateToForm<TrainerSettingsForm>();
        SetActiveNav("nutrition");

        BuildContent();

        btnSearch.Click += (_, _) => DoSearch();
        _tbSearch.Inner.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) DoSearch(); };

        _dgvTrainees.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _allTrainees.Count) return;
            var trainee = _allTrainees[e.RowIndex];
            using var popup = new TrainerTraineeNutritionForm(CurrentUser, trainee);
            popup.ShowDialog(this);
        };

        Resize += (_, _) =>
        {
            LayoutContent();
            UITheme.ResponsiveReflow(_contentArea);
        };

        LoadData();
    }

    private void BuildContent()
    {
        lblHeading = new Label
        {
            Text      = "🥗 Nutrition — Trainee Manager",
            Location  = new Point(UITheme.S24, 20),
            Size      = new Size(700, 36),
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        };

        // ── LEFT PANEL: Trainee list ─────────────────────────────
        var lblTraineeTitle = new Label
        {
            Text      = "My Trainees  — Double-click to edit nutrition",
            Location  = new Point(UITheme.S24, 68),
            Size      = new Size(360, 20),
            Font      = UITheme.FontSemiBold(9f),
            ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };

        _dgvTrainees = new DataGridView
        {
            Location          = new Point(UITheme.S24, 92),
            Size              = new Size(360, 420),
            AllowUserToAddRows = false,
            ReadOnly           = true,
            SelectionMode      = DataGridViewSelectionMode.FullRowSelect
        };
        UITheme.StyleDataGridView(_dgvTrainees);
        _dgvTrainees.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trainee",    Width = 150 });
        _dgvTrainees.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trainer",    Width = 100 });
        _dgvTrainees.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kcal Today", Width = 80  });

        // ── RIGHT PANEL: Food Database ───────────────────────────
        int rightX = 408;

        var lblFoodTitle = new Label
        {
            Text      = "Food Database  — Browse & Reference",
            Location  = new Point(rightX, 68),
            Size      = new Size(600, 20),
            Font      = UITheme.FontSemiBold(9f),
            ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };

        _tbSearch = new StyledTextBox
        {
            Location = new Point(rightX, 92),
            Size     = new Size(300, 42)
        };
        _tbSearch.Inner.PlaceholderText = "Search food items...";

        btnSearch = new GoldButton
        {
            Text     = "Search",
            Location = new Point(rightX + 308, 92),
            Size     = new Size(90, 42),
            Style    = GoldButton.ButtonStyle.Ghost
        };

        _lblFoodHint = new Label
        {
            Text      = "💡 Double-click a trainee on the left to open their nutrition editor.",
            Location  = new Point(rightX, 140),
            Size      = new Size(600, 18),
            Font      = UITheme.FontBody(8.5f),
            ForeColor = UITheme.TextMuted,
            BackColor = Color.Transparent
        };

        _dgvFood = new DataGridView
        {
            Location          = new Point(rightX, 164),
            Size              = new Size(580, 350),
            AllowUserToAddRows = false,
            ReadOnly           = true,
            SelectionMode      = DataGridViewSelectionMode.FullRowSelect
        };
        UITheme.StyleDataGridView(_dgvFood);
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Food",      DataPropertyName = "FoodName",        Width = 240 });
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kcal/100g", DataPropertyName = "CaloriesPer100g", Width = 100 });
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "P",         DataPropertyName = "ProteinPer100g",  Width = 70  });
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "C",         DataPropertyName = "CarbsPer100g",    Width = 70  });
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "F",         DataPropertyName = "FatPer100g",      Width = 70  });

        _contentArea.Controls.AddRange(new Control[]
        {
            lblHeading, lblTraineeTitle, _dgvTrainees,
            lblFoodTitle, _tbSearch, btnSearch,
            _lblFoodHint, _dgvFood
        });
    }

    private void LoadData()
    {
        _allTrainees = _userSvc.GetTraineesByTrainer(CurrentUser.PersonId);
        _dgvTrainees.Rows.Clear();
        foreach (var t in _allTrainees)
        {
            string trainerLabel = t.TrainerId.HasValue ? "Assigned" : "Independent";
            var (cal, _, _, _, _) = _nutSvc.GetDailySummary(t.PersonId, DateTime.Today);
            _dgvTrainees.Rows.Add(t.FullName, trainerLabel, $"{cal:F0}");
        }

        DoSearch();
    }

    private void DoSearch()
    {
        string q = _tbSearch.Text.Trim();
        _dgvFood.Rows.Clear();
        _foodResults = string.IsNullOrEmpty(q) ? _nutSvc.GetAll() : _nutSvc.Search(q);
        foreach (var f in _foodResults)
            _dgvFood.Rows.Add(f.FoodName, f.CaloriesPer100g.ToString("F1"),
                f.ProteinPer100g.ToString("F1"), f.CarbsPer100g.ToString("F1"), f.FatPer100g.ToString("F1"));
    }

    private void LayoutContent()
    {
        int cw     = _contentArea.ClientSize.Width;
        int leftW  = Math.Max(300, cw / 3);
        int rightX = leftW + 48;

        _dgvTrainees.Width = leftW;
        foreach (Control c in _contentArea.Controls)
        {
            if (c == lblHeading || c == _dgvTrainees) continue;
            if (c.Left >= leftW + 20)
            {
                c.Left  = rightX;
                c.Width = Math.Max(200, cw - rightX - 28);
            }
        }
    }
}

using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainer;

public class TrainerExerciseForm : AppShell
{
    private readonly WorkoutService _workSvc = new();

    // Content controls
    private Label lblHeading = null!;
    private DataGridView _dgv = null!;
    private ComboBox cbCat = null!;

    public TrainerExerciseForm() : this(new FitTrack.Models.Trainer { FullName = "Trainer Designer", Role = "Trainer" }) { }

    public TrainerExerciseForm(Person user) : base(user, "Exercise Library")
    {
        // Nav buttons
        AddNavButton("🏠", "Dashboard",   "dashboard").Click += (_, _) => NavigateToForm<TrainerDashboardForm>();
        AddNavButton("👤", "Profile",     "profile"  ).Click += (_, _) => NavigateToForm<TrainerProfileForm>();
        AddNavButton("👥", "My Trainees", "trainees" ).Click += (_, _) => NavigateToForm<TraineeManagementForm>();
        AddNavButton("💪", "Exercises",   "exercises").Click += (_, _) => NavigateToForm<TrainerExerciseForm>();
        AddNavButton("🥗", "Nutrition",   "nutrition").Click += (_, _) => NavigateToForm<TrainerNutritionForm>();
        AddNavButton("⚙️", "Settings",   "settings" ).Click += (_, _) => NavigateToForm<TrainerSettingsForm>();
        SetActiveNav("exercises");

        BuildContent();

        // Populate category combo
        cbCat.Items.Add("All Categories");
        foreach (var c in _workSvc.GetCategories()) cbCat.Items.Add(c);
        cbCat.DisplayMember = "CategoryName";
        cbCat.SelectedIndex = 0;

        cbCat.SelectedIndexChanged += (_, _) => LoadExercises(cbCat.SelectedItem as ExerciseCategory);
        LoadExercises(null);

        Resize += (_, _) =>
        {
            _dgv.Width = _contentArea.Width - 20;
            UITheme.ResponsiveReflow(_contentArea);
        };
    }

    private void BuildContent()
    {
        lblHeading = new Label
        {
            Text      = "Exercise Library",
            Location  = new Point(0, UITheme.S24),
            Size      = new Size(500, 36),
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        };

        cbCat = new ComboBox
        {
            Location      = new Point(0, 78),
            Size          = new Size(200, 34),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        UITheme.StyleComboBox(cbCat);

        _dgv = new DataGridView
        {
            Location = new Point(0, 128),
            Size     = new Size(900, 500)
        };
        UITheme.StyleDataGridView(_dgv);

        var colName = new DataGridViewTextBoxColumn { HeaderText = "Name",      DataPropertyName = "Name",         Width = 200 };
        var colCat  = new DataGridViewTextBoxColumn { HeaderText = "Category",  DataPropertyName = "CategoryName", Width = 120 };
        var colMus  = new DataGridViewTextBoxColumn { HeaderText = "Muscle",    DataPropertyName = "MuscleGroup",  Width = 140 };
        var colEq   = new DataGridViewTextBoxColumn { HeaderText = "Equipment", DataPropertyName = "Equipment",    Width = 140 };
        var colDef  = new DataGridViewTextBoxColumn { HeaderText = "Default",   Name = "Vol",                      Width = 100 };
        _dgv.Columns.AddRange(new DataGridViewColumn[] { colName, colCat, colMus, colEq, colDef });

        _contentArea.Controls.AddRange(new Control[] { lblHeading, cbCat, _dgv });
    }

    private void LoadExercises(ExerciseCategory? cat)
    {
        _dgv.Rows.Clear();
        var exs = cat == null ? _workSvc.GetExercises() : _workSvc.GetByCategory(cat.CategoryId);
        foreach (var ex in exs)
        {
            string vol = ex.DefaultReps.HasValue
                ? $"{ex.DefaultSets}×{ex.DefaultReps}"
                : $"{ex.DefaultSets}×{ex.DefaultSecs}s";
            _dgv.Rows.Add(ex.Name, ex.CategoryName, ex.MuscleGroup, ex.Equipment, vol);
        }
    }
}

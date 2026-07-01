using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainer;

/// <summary>
/// Popup opened by TrainerNutritionForm when trainer double-clicks a trainee.
/// Shows that trainee's today nutrition log. Trainer can add new entries.
/// Old entries added by the trainee are shown as locked (grey). 
/// Trainer-added entries are shown unlocked (gold tint).
/// Nutrition resets daily — yesterday is archived to NutritionHistory.
/// </summary>
public class TrainerTraineeNutritionForm : Form
{
    private readonly Person _trainer;
    private readonly Person _trainee;
    private readonly NutritionService _nutSvc = new();

    private Panel       _scrollLog  = null!;
    private Label       _lblStatus  = null!;
    private Label       _lblSummary = null!;
    private DataGridView _dgvFood   = null!;
    private StyledTextBox _tbSearch = null!;
    private StyledTextBox _tbGrams  = null!;
    private ComboBox    _cbMeal     = null!;
    private List<FoodItem> _foodResults = new();

    public TrainerTraineeNutritionForm() : this(
        new FitTrack.Models.Trainer { FullName = "Trainer", Role = "Trainer" },
        new FitTrack.Models.Trainee { FullName = "Trainee", Role = "Trainee" }) { }

    public TrainerTraineeNutritionForm(Person trainer, Person trainee)
    {
        _trainer = trainer;
        _trainee = trainee;
        Build();
    }

    private void Build()
    {
        Text            = $"Nutrition — {_trainee.FullName}";
        Size            = new Size(920, 750);
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = UITheme.BgPage;
        FormBorderStyle = FormBorderStyle.Sizable;
        DoubleBuffered  = true;

        // ── Top strip ───────────────────────────────────────────
        var topStrip = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = UITheme.BgSidebar };
        topStrip.Controls.Add(new Label
        {
            Text      = $"🥗  {_trainee.FullName}'s Nutrition — Today",
            Left = 20, Top = 0, Width = 600, Height = 62,
            Font      = UITheme.FontHeading(13f),
            ForeColor = UITheme.Gold,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var btnRefresh = new GoldButton { Text = "🔄 Refresh", Left = 650, Top = 12, Width = 110, Height = 36, Style = GoldButton.ButtonStyle.Ghost };
        btnRefresh.Click += (_, _) => LoadLog();
        topStrip.Controls.Add(btnRefresh);

        // Status bar
        _lblStatus = new Label
        {
            Left = 20, Top = 62, Width = 860, Height = 20,
            Font = UITheme.FontBody(8.5f), ForeColor = UITheme.TextSecondary, BackColor = Color.Transparent
        };

        // ── Food search row (trainer can always add) ────────────
        var searchPanel = new Panel { Left = 20, Top = 86, Width = 860, Height = 50, BackColor = Color.Transparent };

        _tbSearch = new StyledTextBox { Left = 0, Top = 4, Width = 260, Height = 42 };
        _tbSearch.Inner.PlaceholderText = "Search food...";

        var btnSearch = new GoldButton { Text = "Search", Left = 268, Top = 4, Width = 86, Height = 42, Style = GoldButton.ButtonStyle.Ghost };
        btnSearch.Click += (_, _) => DoSearch();
        _tbSearch.Inner.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) DoSearch(); };

        _cbMeal = new ComboBox { Left = 362, Top = 8, Width = 130, Height = 34, DropDownStyle = ComboBoxStyle.DropDownList };
        _cbMeal.Items.AddRange(new[] { "Breakfast", "Lunch", "Dinner", "Snack" });
        _cbMeal.SelectedIndex = 0;
        UITheme.StyleComboBox(_cbMeal);

        _tbGrams = new StyledTextBox { Left = 500, Top = 4, Width = 90, Height = 42 };
        _tbGrams.Inner.PlaceholderText = "Grams";

        var btnLog = new GoldButton { Text = "➕ Log", Left = 598, Top = 4, Width = 86, Height = 42, Style = GoldButton.ButtonStyle.Gold };
        btnLog.Click += (_, _) => DoLog();

        searchPanel.Controls.AddRange(new Control[] { _tbSearch, btnSearch, _cbMeal, _tbGrams, btnLog });

        // ── Food results grid ────────────────────────────────────
        _dgvFood = new DataGridView { Left = 20, Top = 142, Width = 860, Height = 160, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Food",      DataPropertyName = "FoodName",        Width = 280 });
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kcal/100g", DataPropertyName = "CaloriesPer100g", Width = 110 });
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Protein",   DataPropertyName = "ProteinPer100g",  Width = 80  });
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Carbs",     DataPropertyName = "CarbsPer100g",    Width = 80  });
        _dgvFood.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fat",       DataPropertyName = "FatPer100g",      Width = 80  });
        UITheme.StyleDataGridView(_dgvFood);

        // ── Summary ──────────────────────────────────────────────
        _lblSummary = new Label
        {
            Left = 20, Top = 308, Width = 860, Height = 22,
            Font = UITheme.FontSemiBold(9f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
        };

        // ── Today's log (scrollable cards) ──────────────────────
        var logTitle = new Label
        {
            Text      = "Today's Log",
            Left = 20, Top = 334, Width = 400, Height = 20,
            Font      = UITheme.FontSemiBold(10f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
        };

        _scrollLog = new Panel { Left = 20, Top = 358, Width = 860, Height = 330, AutoScroll = true, BackColor = Color.Transparent };

        this.Controls.Add(topStrip);
        this.Controls.Add(_lblStatus);
        this.Controls.Add(searchPanel);
        this.Controls.Add(_dgvFood);
        this.Controls.Add(_lblSummary);
        this.Controls.Add(logTitle);
        this.Controls.Add(_scrollLog);

        Resize += (_, _) =>
        {
            int cw = this.ClientSize.Width - 40;
            searchPanel.Width = cw;
            _dgvFood.Width    = cw;
            _lblSummary.Width = cw;
            logTitle.Width    = cw;
            _scrollLog.Width  = cw;
            _scrollLog.Height = this.ClientSize.Height - 358 - 10;
        };

        // Archive yesterday then load today
        try { _nutSvc.ArchiveAndReset(_trainee.PersonId); } catch { /* ignore if table not yet created */ }
        LoadLog();
    }

    private void DoSearch()
    {
        string q = _tbSearch.Text.Trim();
        if (string.IsNullOrEmpty(q)) return;
        _dgvFood.Rows.Clear();
        _foodResults = _nutSvc.Search(q);
        foreach (var f in _foodResults)
            _dgvFood.Rows.Add(f.FoodName, f.CaloriesPer100g.ToString("F1"),
                f.ProteinPer100g.ToString("F1"), f.CarbsPer100g.ToString("F1"), f.FatPer100g.ToString("F1"));
    }

    private void DoLog()
    {
        if (_dgvFood.CurrentRow == null || _foodResults.Count == 0) return;
        int idx = _dgvFood.CurrentRow.Index;
        if (idx < 0 || idx >= _foodResults.Count) return;
        if (!double.TryParse(_tbGrams.Text.Trim(), out double grams) || grams <= 0)
        {
            MessageBox.Show("Enter valid grams.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var meal = _cbMeal.SelectedItem?.ToString() ?? "Snack";
        var (ok, msg, _) = _nutSvc.LogMeal(_trainee.PersonId, _foodResults[idx].FoodItemId, meal, grams,
            loggedByPersonId: _trainer.PersonId);
        if (!ok) { MessageBox.Show(msg, "Error"); return; }
        _tbGrams.Text = "";
        LoadLog();
    }

    private void LoadLog()
    {
        _scrollLog.Controls.Clear();
        var (cal, prot, carbs, fat, meals) = _nutSvc.GetDailySummary(_trainee.PersonId, DateTime.Today);
        _lblSummary.Text = $"Today: {cal:F0} kcal  •  Protein {prot:F1}g  •  Carbs {carbs:F1}g  •  Fat {fat:F1}g";
        _lblStatus.Text  = $"Viewing: {_trainee.FullName}  •  {DateTime.Now:dd MMM yyyy}  •  {meals.Count} meal(s) logged";

        int y = 0;
        foreach (var m in meals)
        {
            bool lockedEntry = m.LoggedByPersonId.HasValue && m.LoggedByPersonId != _trainer.PersonId;
            string icon      = lockedEntry ? "🔒" : "✏️";
            Color  bg        = lockedEntry
                ? Color.FromArgb(10, 150, 150, 150)    // grey — trainee-added
                : Color.FromArgb(15, 180, 150, 50);    // gold — trainer-added

            var row = new Panel { Left = 0, Top = y, Width = 840, Height = 36, BackColor = bg };

            row.Controls.Add(new Label
            {
                Text = $"{icon}  {m.MealType,-10}  {m.FoodName,-32}  {m.ServingGrams:F0}g  →  {m.Calories:F0} kcal  |  P:{m.ProteinG:F1}g  C:{m.CarbsG:F1}g  F:{m.FatG:F1}g",
                Left = 8, Top = 8, Width = 700, Height = 20,
                Font = UITheme.FontBody(9f), ForeColor = UITheme.TextPrimary, BackColor = Color.Transparent
            });

            if (!lockedEntry)
            {
                int capturedId = m.NutritionLogId;
                var btnDel = new GoldButton { Text = "✕", Left = 792, Top = 4, Width = 32, Height = 28, Style = GoldButton.ButtonStyle.Red };
                btnDel.Click += (_, _) =>
                {
                    _nutSvc.DeleteLog(capturedId);
                    LoadLog();
                };
                row.Controls.Add(btnDel);
            }

            _scrollLog.Controls.Add(row);
            y += 38;
        }

        if (meals.Count == 0)
            _scrollLog.Controls.Add(new Label
            {
                Text = "No meals logged today for this trainee.",
                Left = 0, Top = 0, Width = 800, Height = 30,
                Font = UITheme.FontBody(9.5f), ForeColor = UITheme.TextMuted, BackColor = Color.Transparent
            });

        _scrollLog.AutoScrollMinSize = new Size(840, y + 10);
    }
}

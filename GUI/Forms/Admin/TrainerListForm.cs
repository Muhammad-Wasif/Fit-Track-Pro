using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Admin;

public class TrainerListForm : AppShell
{
    // ── Services ────────────────────────────────────────────────
    private readonly UserService       _userSvc  = new();
    private readonly AttendanceService _attendSvc = new();

    // ── Content controls ────────────────────────────────────────
    private Label       lblHeading = null!;
    private DataGridView _dgv      = null!;
    private StyledTextBox tbSearch = null!;
    private GoldButton  btnRefresh = null!;

    private List<Person> _allTrainers = new();

    // ── Designer support ─────────────────────────────────────────
    public TrainerListForm() : this(new FitTrack.Models.Admin { FullName = "Admin Designer", Role = "Admin" }) { }

    public TrainerListForm(Person user) : base(user, "Trainer Management")
    {
        // ── Navigation buttons ───────────────────────────────────
        var btnDash  = AddNavButton("🏠", "Dashboard",   "dashboard");
        var btnTrain = AddNavButton("👥", "Trainers",    "trainers");
        var btnAtten = AddNavButton("📅", "Attendance",  "attendance");
        var btnAdd   = AddNavButton("➕", "Add Trainer", "addtrainer");

        btnDash.Click  += (_, _) => NavigateToForm<AdminDashboardForm>();
        btnTrain.Click += (_, _) => NavigateToForm<TrainerListForm>();
        btnAtten.Click += (_, _) => NavigateToForm<AttendanceForm>();
        btnAdd.Click   += (_, _) => NavigateToForm<AddTrainerForm>();

        SetActiveNav("trainers");

        // ── Build content ────────────────────────────────────────
        BuildContent();

        // ── Wire content events ──────────────────────────────────
        tbSearch.Inner.TextChanged += (_, _) => FilterGrid(tbSearch.Text);
        btnRefresh.Click           += (_, _) => LoadTrainers();

        Resize += (_, _) =>
        {
            _dgv.Width = _contentArea.Width - UITheme.S32 * 2;
        };

        LoadTrainers();
    }

    private void BuildContent()
    {
        // lblHeading
        lblHeading = new Label
        {
            Text      = "Trainer Management",
            Location  = new Point(UITheme.S24, UITheme.S24),
            Size      = new Size(600, 36),
            BackColor = Color.Transparent,
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary
        };

        // tbSearch
        tbSearch = new StyledTextBox
        {
            Location = new Point(UITheme.S24, 70),
            Size     = new Size(280, 40)
        };
        tbSearch.Inner.PlaceholderText = "Search trainers...";

        // btnRefresh
        btnRefresh = new GoldButton
        {
            Text     = "Refresh",
            Location = new Point(UITheme.S24 + 280 + UITheme.S16, 70),
            Size     = new Size(100, 40),
            Style    = GoldButton.ButtonStyle.Ghost
        };

        // _dgv
        _dgv = new DataGridView
        {
            Location = new Point(UITheme.S24, 126),
            Size     = new Size(900, 500)
        };

        var colId     = new DataGridViewTextBoxColumn { HeaderText = "ID",           DataPropertyName = "PersonId",  Width = 50  };
        var colName   = new DataGridViewTextBoxColumn { HeaderText = "Full Name",    DataPropertyName = "FullName",  Width = 180 };
        var colUser   = new DataGridViewTextBoxColumn { HeaderText = "Username",     DataPropertyName = "Username",  Width = 140 };
        var colEmail  = new DataGridViewTextBoxColumn { HeaderText = "Email",        DataPropertyName = "Email",     Width = 200 };
        var colCount  = new DataGridViewTextBoxColumn { HeaderText = "Trainees",     Name = "TraineeCount",          Width = 90  };
        var colStatus = new DataGridViewTextBoxColumn { HeaderText = "Today Status", Name = "TodayStatus",           Width = 110 };
        _dgv.Columns.AddRange(new DataGridViewColumn[] { colId, colName, colUser, colEmail, colCount, colStatus });

        UITheme.StyleDataGridView(_dgv);

        _contentArea.Controls.Add(lblHeading);
        _contentArea.Controls.Add(tbSearch);
        _contentArea.Controls.Add(btnRefresh);
        _contentArea.Controls.Add(_dgv);
    }

    // ── Data logic ───────────────────────────────────────────────

    private void LoadTrainers()
    {
        _allTrainers = _userSvc.GetAllTrainers();
        PopulateGrid(_allTrainers);
    }

    private void FilterGrid(string q)
    {
        var filtered = string.IsNullOrWhiteSpace(q)
            ? _allTrainers
            : _allTrainers.Where(t =>
                t.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.Username.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        PopulateGrid(filtered);
    }

    private void PopulateGrid(List<Person> trainers)
    {
        _dgv.Rows.Clear();
        foreach (var t in trainers)
        {
            int    count = _userSvc.GetTraineesByTrainer(t.PersonId).Count;
            string st    = _attendSvc.GetTodayStatus(t.PersonId);
            int    r     = _dgv.Rows.Add(t.PersonId, t.FullName, t.Username, t.Email, count, st);

            var cell = _dgv.Rows[r].Cells["TodayStatus"];
            cell.Style.ForeColor = st switch
            {
                "Present"  => UITheme.Success,
                "On Leave" => UITheme.Warning,
                _          => UITheme.TextMuted
            };
        }
    }
}

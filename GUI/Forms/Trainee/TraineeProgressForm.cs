using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainee;

public class TraineeProgressForm : AppShell
{
    // ── Services ─────────────────────────────────────────────────
    private readonly ProgressService _progSvc = new();

    // ── Content controls ─────────────────────────────────────────
    private Label             lblHeading    = null!;
    private RoundedPanel      logCard       = null!;
    private Label             lblWeightLabel= null!;
    private StyledTextBox     _tbWeight     = null!;
    private Label             lblFatLabel   = null!;
    private StyledTextBox     _tbBF         = null!;
    private Label             lblNotesLabel = null!;
    private StyledTextBox     _tbNotes      = null!;
    private GoldButton        btnLog        = null!;
    private Label             _lblMsg       = null!;
    private LineChartControl  _chart        = null!;
    private Label             lblHistTitle  = null!;
    private DataGridView      _dgv          = null!;

    // ── Designer support ─────────────────────────────────────────
    public TraineeProgressForm() : this(new Models.Trainee { FullName = "Trainee Designer", Role = "Trainee" }) { }

    public TraineeProgressForm(Person user) : base(user, "Progress Tracking")
    {
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

        SetActiveNav("progress");
        BuildContent(user);
        LoadHistory();

        Resize += (_, _) =>
        {
            int w = _contentArea.Width - 10;
            _chart.Width  = w;
            _dgv.Width    = w;
            logCard.Width = w;
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  BUILD CONTENT
    // ─────────────────────────────────────────────────────────────
    private void BuildContent(Person user)
    {
        lblHeading = new Label
        {
            Text = "Progress Tracking", Location = new Point(0, 0), Size = new Size(500, 36),
            BackColor = Color.Transparent, Font = UITheme.FontHeading(20f), ForeColor = UITheme.TextPrimary
        };

        // Log card
        logCard = new RoundedPanel { Location = new Point(0, 48), Size = new Size(760, 106) };

        lblWeightLabel = new Label
        {
            Text = "Weight (kg)", Location = new Point(UITheme.S16, 10), Size = new Size(110, 16),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary
        };
        _tbWeight = new StyledTextBox { Location = new Point(UITheme.S16, 28), Size = new Size(110, UITheme.TextBoxHeight) };
        _tbWeight.Inner.Text = user.WeightKg.ToString();

        lblFatLabel = new Label
        {
            Text = "Body Fat %", Location = new Point(140, 10), Size = new Size(110, 16),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary
        };
        _tbBF = new StyledTextBox { Location = new Point(140, 28), Size = new Size(110, UITheme.TextBoxHeight) };
        _tbBF.Inner.Text = user.BodyFatPct?.ToString() ?? "";

        lblNotesLabel = new Label
        {
            Text = "Notes", Location = new Point(264, 10), Size = new Size(260, 16),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary
        };
        _tbNotes = new StyledTextBox { Location = new Point(264, 28), Size = new Size(260, UITheme.TextBoxHeight) };

        btnLog = new GoldButton
        {
            Text = "Log Progress", Location = new Point(540, 28), Size = new Size(200, UITheme.TextBoxHeight)
        };
        btnLog.Click += (_, _) => LogProgress();

        _lblMsg = new Label
        {
            Location = new Point(UITheme.S16, 78), Size = new Size(720, 20),
            BackColor = Color.Transparent, Font = UITheme.FontBody(9f)
        };

        logCard.Controls.AddRange(new Control[]
        {
            lblWeightLabel, _tbWeight, lblFatLabel, _tbBF,
            lblNotesLabel, _tbNotes, btnLog, _lblMsg
        });

        // Chart
        _chart = new LineChartControl
        {
            Location   = new Point(0, 164),
            Size       = new Size(760, 240),
            ChartTitle = "Weight Over Time",
            YAxisLabel = "kg"
        };

        lblHistTitle = new Label
        {
            Text = "History", Location = new Point(0, 416), Size = new Size(300, 22),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(11f), ForeColor = UITheme.TextPrimary
        };

        _dgv = new DataGridView { Location = new Point(0, 442), Size = new Size(760, 300) };
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date",       Width = 130 });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Weight kg",  Width = 100 });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Body Fat %", Width = 100 });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BMI",        Width = 80 });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Notes",      Width = 300 });
        UITheme.StyleDataGridView(_dgv);

        _contentArea.Controls.AddRange(new Control[]
        {
            lblHeading, logCard, _chart, lblHistTitle, _dgv
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  LOGIC
    // ─────────────────────────────────────────────────────────────
    private void LogProgress()
    {
        if (!double.TryParse(_tbWeight.Text, out double wt) || wt <= 0)
        {
            _lblMsg.Text = "Enter a valid weight."; _lblMsg.ForeColor = UITheme.Red; return;
        }
        double? bf = double.TryParse(_tbBF.Text, out double bfv) ? bfv : (double?)null;
        var (ok, msg, _) = _progSvc.LogSnapshot(CurrentUser.PersonId, wt, bf, _tbNotes.Text.Trim());
        _lblMsg.Text = ok ? "✓ Progress logged." : msg;
        _lblMsg.ForeColor = ok ? UITheme.Success : UITheme.Red;
        if (ok)
        {
            CurrentUser.WeightKg   = wt;
            CurrentUser.BodyFatPct = bf;
            LoadHistory();
        }
    }

    private void LoadHistory()
    {
        _dgv.Rows.Clear();
        var snaps = _progSvc.GetSnapshots(CurrentUser.PersonId, 60);
        foreach (var s in snaps)
            _dgv.Rows.Add(s.SnapshotDate.ToString("yyyy-MM-dd HH:mm"),
                s.WeightKg.ToString("F1"), s.BodyFatPct?.ToString("F1") ?? "—",
                s.BMI.ToString("F1"), s.Notes ?? "");
        var chartData = snaps.OrderBy(s => s.SnapshotDate).Select(s => (s.SnapshotDate, s.WeightKg));
        _chart.SetData(chartData);
    }
}

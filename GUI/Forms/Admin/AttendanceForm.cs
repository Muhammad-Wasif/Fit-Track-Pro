using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Admin;

public class AttendanceForm : AppShell
{
    // ── Services ────────────────────────────────────────────────
    private readonly UserService       _userSvc   = new();
    private readonly AttendanceService _attendSvc = new();

    // ── Content controls ────────────────────────────────────────
    private Label        lblHeading  = null!;
    private RoundedPanel card        = null!;
    private ComboBox     _cbTrainer  = null!;
    private ComboBox     _cbStatus   = null!;
    private StyledTextBox _tbNotes   = null!;
    private GoldButton   btnMark     = null!;
    private Label        _lblMsg     = null!;
    private Label        lblHistTitle = null!;
    private DataGridView _dgvHistory = null!;

    private List<Person> _trainers = new();

    // ── Designer support ─────────────────────────────────────────
    public AttendanceForm() : this(new FitTrack.Models.Admin { FullName = "Admin Designer", Role = "Admin" }) { }

    public AttendanceForm(Person user) : base(user, "Trainer Attendance")
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

        SetActiveNav("attendance");

        // ── Build content ────────────────────────────────────────
        BuildContent();

        // ── Wire content events ──────────────────────────────────
        _cbTrainer.SelectedIndexChanged += (_, _) => LoadHistory();
        btnMark.Click                   += (_, _) => MarkAttendance();

        Resize += (_, _) =>
        {
            card.Width        = _contentArea.Width - UITheme.S32 * 2;
            _dgvHistory.Width = _contentArea.Width - UITheme.S32 * 2;
        };

        LoadTrainers();
    }

    private void BuildContent()
    {
        // lblHeading
        lblHeading = new Label
        {
            Text      = "Trainer Attendance",
            Location  = new Point(UITheme.S24, UITheme.S24),
            Size      = new Size(600, 36),
            BackColor = Color.Transparent,
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary
        };

        // card
        card = new RoundedPanel
        {
            Location   = new Point(UITheme.S24, 70),
            Size       = new Size(760, 140),
            ShowShadow = true
        };

        // ── Trainer label + combo ────────────────────────────────
        var lblTrainerLabel = new Label
        {
            Text      = "Trainer",
            Location  = new Point(UITheme.S16, UITheme.S16),
            Size      = new Size(80, 18),
            Font      = UITheme.FontSemiBold(9f),
            ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };
        card.Controls.Add(lblTrainerLabel);

        _cbTrainer = new ComboBox
        {
            Location      = new Point(UITheme.S16, 34),
            Size          = new Size(220, 34),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        UITheme.StyleComboBox(_cbTrainer);
        card.Controls.Add(_cbTrainer);

        // ── Status label + combo ─────────────────────────────────
        var lblStatusLabel = new Label
        {
            Text      = "Status",
            Location  = new Point(256, UITheme.S16),
            Size      = new Size(80, 18),
            Font      = UITheme.FontSemiBold(9f),
            ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };
        card.Controls.Add(lblStatusLabel);

        _cbStatus = new ComboBox
        {
            Location      = new Point(256, 34),
            Size          = new Size(160, 34),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbStatus.Items.AddRange(new object[] { "Present", "On Leave", "Absent" });
        _cbStatus.SelectedIndex = 0;
        UITheme.StyleComboBox(_cbStatus);
        card.Controls.Add(_cbStatus);

        // ── Notes label + textbox ────────────────────────────────
        var lblNotesLabel = new Label
        {
            Text      = "Notes (optional)",
            Location  = new Point(436, UITheme.S16),
            Size      = new Size(160, 18),
            Font      = UITheme.FontSemiBold(9f),
            ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };
        card.Controls.Add(lblNotesLabel);

        _tbNotes = new StyledTextBox
        {
            Location = new Point(436, 34),
            Size     = new Size(240, 40)
        };
        _tbNotes.Inner.PlaceholderText = "Notes (optional)";
        card.Controls.Add(_tbNotes);

        // ── Mark button ──────────────────────────────────────────
        btnMark = new GoldButton
        {
            Text     = "Mark Attendance",
            Location = new Point(UITheme.S16, 88),
            Size     = new Size(180, 40),
            Style    = GoldButton.ButtonStyle.Gold
        };
        card.Controls.Add(btnMark);

        // ── Message label ────────────────────────────────────────
        _lblMsg = new Label
        {
            Location  = new Point(210, 96),
            Size      = new Size(400, 22),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(9f),
            ForeColor = UITheme.Success
        };
        card.Controls.Add(_lblMsg);

        // ── History title ────────────────────────────────────────
        lblHistTitle = new Label
        {
            Text      = "Attendance History (Last 30 Days)",
            Location  = new Point(UITheme.S24, 224),
            Size      = new Size(500, 22),
            BackColor = Color.Transparent,
            Font      = UITheme.FontSemiBold(11f),
            ForeColor = UITheme.TextPrimary
        };

        // ── History grid ─────────────────────────────────────────
        _dgvHistory = new DataGridView
        {
            Location = new Point(UITheme.S24, 250),
            Size     = new Size(760, 400)
        };
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date",   DataPropertyName = "AttendDate", Width = 120 });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status",     Width = 120 });
        _dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Notes",  DataPropertyName = "Notes",      Width = 300 });
        UITheme.StyleDataGridView(_dgvHistory);

        _contentArea.Controls.Add(lblHeading);
        _contentArea.Controls.Add(card);
        _contentArea.Controls.Add(lblHistTitle);
        _contentArea.Controls.Add(_dgvHistory);
    }

    // ── Data logic ───────────────────────────────────────────────

    private void LoadTrainers()
    {
        _cbTrainer.Items.Clear();
        _trainers = _userSvc.GetAllTrainers();
        foreach (var t in _trainers) _cbTrainer.Items.Add(t);
        _cbTrainer.DisplayMember = "FullName";
        if (_cbTrainer.Items.Count > 0)
        {
            _cbTrainer.SelectedIndex = 0;
            LoadHistory();
        }
    }

    private void MarkAttendance()
    {
        if (_cbTrainer.SelectedItem is not Person t) return;
        _attendSvc.MarkAttendance(t.PersonId, _cbStatus.SelectedItem!.ToString()!, _tbNotes.Text.Trim());
        _lblMsg.Text     = $"✓ Attendance marked as {_cbStatus.SelectedItem}";
        _lblMsg.ForeColor = UITheme.Success;
        LoadHistory();
    }

    private void LoadHistory()
    {
        _dgvHistory.Rows.Clear();
        if (_cbTrainer.SelectedItem is not Person t) return;
        var records = _attendSvc.GetHistory(t.PersonId, 30);
        foreach (var rec in records)
        {
            int r = _dgvHistory.Rows.Add(rec.AttendDate.ToString("yyyy-MM-dd"), rec.Status, rec.Notes ?? "");
            var statusCell = _dgvHistory.Rows[r].Cells["Status"];
            statusCell.Style.ForeColor = rec.Status switch
            {
                "Present"  => UITheme.Success,
                "On Leave" => UITheme.Warning,
                _          => UITheme.Red
            };
        }
    }
}

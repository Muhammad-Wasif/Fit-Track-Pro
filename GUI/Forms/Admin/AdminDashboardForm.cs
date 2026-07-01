using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Admin;

public class AdminDashboardForm : AppShell
{
    // ── Services ────────────────────────────────────────────────
    private readonly UserService _userSvc = new();

    // ── Content controls ────────────────────────────────────────
    private Label      lblTitle    = null!;
    private Label      lblSub      = null!;
    private MetricCard cardTrainers = null!;
    private MetricCard cardTrainees = null!;
    private MetricCard cardToday    = null!;
    private MetricCard cardPlans    = null!;
    private MetricCard cardActive   = null!;

    // ── Designer support ─────────────────────────────────────────
    public AdminDashboardForm() : this(new FitTrack.Models.Admin { FullName = "Admin Designer", Role = "Admin" }) { }

    public AdminDashboardForm(Person user) : base(user, "Dashboard")
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

        SetActiveNav("dashboard");

        // ── Build content ────────────────────────────────────────
        BuildContent();
        LoadDashboardData();
    }

    private void BuildContent()
    {
        // lblTitle
        lblTitle = new Label
        {
            Text      = "Admin Dashboard",
            Location  = new Point(UITheme.S24, UITheme.S24),
            Size      = new Size(700, 36),
            BackColor = Color.Transparent,
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary
        };

        // lblSub
        lblSub = new Label
        {
            Location  = new Point(UITheme.S24, UITheme.S24 + 40),
            Size      = new Size(700, 22),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(9.5f),
            ForeColor = UITheme.TextSecondary
        };

        // ── Metric cards — y=112, 16px gaps, Size(220,96) ────────
        int cardY = 112;
        int gap   = UITheme.S16;

        cardTrainers = new MetricCard
        {
            Label       = "Total Trainers",
            Metric      = "0",
            AccentColor = UITheme.Gold,
            Location    = new Point(UITheme.S24, cardY),
            Size        = new Size(220, 96)
        };

        cardTrainees = new MetricCard
        {
            Label       = "Total Trainees",
            Metric      = "0",
            AccentColor = UITheme.Red,
            Location    = new Point(UITheme.S24 + 220 + gap, cardY),
            Size        = new Size(220, 96)
        };

        cardToday = new MetricCard
        {
            Label       = "Today",
            Metric      = "",
            AccentColor = UITheme.TextSecondary,
            Location    = new Point(UITheme.S24 + (220 + gap) * 2, cardY),
            Size        = new Size(220, 96)
        };

        cardPlans = new MetricCard
        {
            Label       = "Active Plans",
            Metric      = "—",
            AccentColor = UITheme.AccentBlue,
            Location    = new Point(UITheme.S24 + (220 + gap) * 3, cardY),
            Size        = new Size(220, 96)
        };

        cardActive = new MetricCard
        {
            Label       = "Members Active",
            Metric      = "—",
            AccentColor = UITheme.Success,
            Location    = new Point(UITheme.S24 + (220 + gap) * 4, cardY),
            Size        = new Size(220, 96)
        };

        _contentArea.Controls.Add(lblTitle);
        _contentArea.Controls.Add(lblSub);
        _contentArea.Controls.Add(cardTrainers);
        _contentArea.Controls.Add(cardTrainees);
        _contentArea.Controls.Add(cardToday);
        _contentArea.Controls.Add(cardPlans);
        _contentArea.Controls.Add(cardActive);
    }

    private void LoadDashboardData()
    {
        lblSub.Text = $"System Administrator  •  {DateTime.Now:dddd, dd MMMM yyyy}";

        int trainers = _userSvc.GetAllTrainers().Count;
        int trainees = _userSvc.GetAllTrainees().Count;

        cardTrainers.Metric = trainers.ToString();
        cardTrainees.Metric = trainees.ToString();
        cardToday.Metric    = DateTime.Now.ToString("MMM dd");
        cardPlans.Metric    = "—";
        cardActive.Metric   = trainees.ToString();
    }
}

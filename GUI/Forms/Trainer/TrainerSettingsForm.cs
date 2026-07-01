using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainer;

public class TrainerSettingsForm : AppShell
{
    private readonly AuthService _auth = new();

    // Content controls
    private Label lblHeading   = null!;
    private RoundedPanel card  = null!;
    private Label lblCardTitle = null!;
    private StyledTextBox _tbCurrent = null!;
    private StyledTextBox _tbNew     = null!;
    private StyledTextBox _tbConfirm = null!;
    private Label _lblMsg  = null!;
    private Label _lblHint = null!;
    private GoldButton btnChange = null!;

    public TrainerSettingsForm() : this(new FitTrack.Models.Trainer { FullName = "Trainer Designer", Role = "Trainer" }) { }

    public TrainerSettingsForm(Person user) : base(user, "Settings")
    {
        // Nav buttons
        AddNavButton("🏠", "Dashboard",   "dashboard").Click += (_, _) => NavigateToForm<TrainerDashboardForm>();
        AddNavButton("👤", "Profile",     "profile"  ).Click += (_, _) => NavigateToForm<TrainerProfileForm>();
        AddNavButton("👥", "My Trainees", "trainees" ).Click += (_, _) => NavigateToForm<TraineeManagementForm>();
        AddNavButton("💪", "Exercises",   "exercises").Click += (_, _) => NavigateToForm<TrainerExerciseForm>();
        AddNavButton("🥗", "Nutrition",   "nutrition").Click += (_, _) => NavigateToForm<TrainerNutritionForm>();
        AddNavButton("⚙️", "Settings",   "settings" ).Click += (_, _) => NavigateToForm<TrainerSettingsForm>();
        SetActiveNav("settings");

        BuildContent();

        _tbNew.Inner.TextChanged += (_, _) => ShowHint();
        btnChange.Click          += (_, _) => DoChange();

        Resize += (_, _) => UITheme.ResponsiveReflow(_contentArea);
    }

    private void BuildContent()
    {
        lblHeading = new Label
        {
            Text      = "Settings",
            Location  = new Point(UITheme.S24, UITheme.S24),
            Size      = new Size(500, 36),
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        };

        card = new RoundedPanel
        {
            Location = new Point(UITheme.S24, 70),
            Size     = new Size(480, 340)
        };

        // Card title
        lblCardTitle = new Label
        {
            Text      = "Change Password",
            Location  = new Point(16, 14),
            Size      = new Size(400, 22),
            Font      = UITheme.FontSemiBold(12f),
            ForeColor = UITheme.TextPrimary,
            BackColor = Color.Transparent
        };
        card.Controls.Add(lblCardTitle);

        // Current Password label + field  (y=46)
        FL(card, "Current Password", 16, 46);
        _tbCurrent = new StyledTextBox
        {
            Location   = new Point(16, 64),
            Size       = new Size(420, 42),
            IsPassword = true
        };
        card.Controls.Add(_tbCurrent);

        // New Password label + field  (y=110)
        FL(card, "New Password", 16, 110);
        _tbNew = new StyledTextBox
        {
            Location   = new Point(16, 128),
            Size       = new Size(420, 42),
            IsPassword = true
        };
        card.Controls.Add(_tbNew);

        // Confirm Password label + field  (y=174)
        FL(card, "Confirm New Password", 16, 174);
        _tbConfirm = new StyledTextBox
        {
            Location   = new Point(16, 192),
            Size       = new Size(420, 42),
            IsPassword = true
        };
        card.Controls.Add(_tbConfirm);

        // Hint label  (y=240)
        _lblHint = new Label
        {
            Location  = new Point(16, 240),
            Size      = new Size(420, 16),
            Font      = UITheme.FontSmall(7.5f),
            ForeColor = UITheme.TextMuted,
            BackColor = Color.Transparent
        };
        card.Controls.Add(_lblHint);

        // Message label  (y=252 — just below hint)
        _lblMsg = new Label
        {
            Location  = new Point(16, 252),
            Size      = new Size(420, 22),
            Font      = UITheme.FontBody(9f),
            BackColor = Color.Transparent
        };
        card.Controls.Add(_lblMsg);

        // Button  (y=278)
        btnChange = new GoldButton
        {
            Text     = "Change Password",
            Location = new Point(16, 278),
            Size     = new Size(200, 40)
        };
        card.Controls.Add(btnChange);

        _contentArea.Controls.Add(lblHeading);
        _contentArea.Controls.Add(card);
    }

    private void FL(Panel p, string text, int x, int y)
    {
        p.Controls.Add(new Label
        {
            Text      = text,
            Location  = new Point(x, y),
            Size      = new Size(420, 16),
            Font      = UITheme.FontSemiBold(8.5f),
            ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        });
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
        var (ok, msg) = _auth.ChangePassword(CurrentUser.PersonId, _tbCurrent.Text, _tbNew.Text);
        _lblMsg.Text      = ok ? "✓ Password changed successfully." : msg;
        _lblMsg.ForeColor = ok ? UITheme.Success : UITheme.Red;
        if (ok) { _tbCurrent.Text = _tbNew.Text = _tbConfirm.Text = ""; }
    }
}

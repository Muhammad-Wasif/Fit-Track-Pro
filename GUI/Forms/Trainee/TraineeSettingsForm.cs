using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainee;

public class TraineeSettingsForm : AppShell
{
    // ── Services ─────────────────────────────────────────────────
    private readonly AuthService _auth = new();

    // ── Content controls ─────────────────────────────────────────
    private Label        lblHeading  = null!;
    private RoundedPanel card        = null!;
    private Label        lblCardTitle= null!;
    private StyledTextBox _tbCurrent = null!;
    private StyledTextBox _tbNew     = null!;
    private StyledTextBox _tbConfirm = null!;
    private Label        _lblMsg     = null!;
    private Label        _lblHint    = null!;
    private GoldButton   btnChange   = null!;

    // ── Designer support ─────────────────────────────────────────
    public TraineeSettingsForm() : this(new Models.Trainee { FullName = "Trainee Designer", Role = "Trainee" }) { }

    public TraineeSettingsForm(Person user) : base(user, "Settings")
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

        SetActiveNav("settings");
        BuildContent();
    }

    // ─────────────────────────────────────────────────────────────
    //  BUILD CONTENT
    // ─────────────────────────────────────────────────────────────
    private void BuildContent()
    {
        lblHeading = new Label
        {
            Text = "Settings", Location = new Point(0, 0), Size = new Size(500, 36),
            BackColor = Color.Transparent, Font = UITheme.FontHeading(20f), ForeColor = UITheme.TextPrimary
        };

        card = new RoundedPanel { Location = new Point(0, 48), Size = new Size(520, 380) };

        lblCardTitle = new Label
        {
            Text = "Change Password", Location = new Point(UITheme.S16, 14), Size = new Size(400, 24),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(12f), ForeColor = UITheme.TextPrimary
        };
        card.Controls.Add(lblCardTitle);

        // Current Password
        FL(card, "Current Password", UITheme.S16, 46);
        _tbCurrent = new StyledTextBox { Location = new Point(UITheme.S16, 64), Size = new Size(460, UITheme.TextBoxHeight), IsPassword = true };
        card.Controls.Add(_tbCurrent);

        // New Password
        FL(card, "New Password", UITheme.S16, 116);
        _tbNew = new StyledTextBox { Location = new Point(UITheme.S16, 134), Size = new Size(460, UITheme.TextBoxHeight), IsPassword = true };
        _tbNew.Inner.TextChanged += (_, _) => ShowHint();
        card.Controls.Add(_tbNew);

        // Confirm Password
        FL(card, "Confirm New Password", UITheme.S16, 186);
        _tbConfirm = new StyledTextBox { Location = new Point(UITheme.S16, 204), Size = new Size(460, UITheme.TextBoxHeight), IsPassword = true };
        card.Controls.Add(_tbConfirm);

        // Hint
        _lblHint = new Label
        {
            Location = new Point(UITheme.S16, 254), Size = new Size(460, 18),
            BackColor = Color.Transparent, Font = UITheme.FontSmall(7.5f), ForeColor = UITheme.TextMuted
        };
        card.Controls.Add(_lblHint);

        // Message
        _lblMsg = new Label
        {
            Location = new Point(UITheme.S16, 278), Size = new Size(460, 22),
            BackColor = Color.Transparent, Font = UITheme.FontBody(9f)
        };
        card.Controls.Add(_lblMsg);

        // Button
        btnChange = new GoldButton { Text = "Change Password", Location = new Point(UITheme.S16, 312), Size = new Size(220, UITheme.ButtonHeight) };
        btnChange.Click += (_, _) => DoChange();
        card.Controls.Add(btnChange);

        _contentArea.Controls.AddRange(new Control[] { lblHeading, card });
    }

    private void FL(Panel p, string text, int x, int y)
    {
        p.Controls.Add(new Label
        {
            Text = text, Location = new Point(x, y), Size = new Size(460, 18),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  LOGIC
    // ─────────────────────────────────────────────────────────────
    private void ShowHint()
    {
        string p = _tbNew.Text;
        if (string.IsNullOrEmpty(p)) { _lblHint.Text = ""; return; }
        var checks = new[]
        {
            $"Len≥8 {(p.Length >= 8 ? "✓" : "✗")}",
            $"Upper {(p.Any(char.IsUpper) ? "✓" : "✗")}",
            $"Lower {(p.Any(char.IsLower) ? "✓" : "✗")}",
            $"Num {(p.Any(char.IsDigit) ? "✓" : "✗")}",
            $"Sym {(p.Any(c => !char.IsLetterOrDigit(c)) ? "✓" : "✗")}"
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

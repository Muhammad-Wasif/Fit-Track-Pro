using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Admin;

public class AddTrainerForm : AppShell
{
    // ── Services ────────────────────────────────────────────────
    private readonly AuthService _auth = new();

    // ── Content controls ────────────────────────────────────────
    private Label        lblHeading = null!;
    private RoundedPanel card       = null!;
    private StyledTextBox _tbFN     = null!;
    private StyledTextBox _tbUser   = null!;
    private StyledTextBox _tbEmail  = null!;
    private StyledTextBox _tbPass   = null!;
    private StyledTextBox _tbAge    = null!;
    private ComboBox      _cbGender = null!;
    private StyledTextBox _tbHeight = null!;
    private StyledTextBox _tbWeight = null!;
    private Label         _lblErr   = null!;
    private Label         _lblOk    = null!;
    private GoldButton    btnAdd    = null!;

    // ── Designer support ─────────────────────────────────────────
    public AddTrainerForm() : this(new FitTrack.Models.Admin { FullName = "Admin Designer", Role = "Admin" }) { }

    public AddTrainerForm(Person user) : base(user, "Add New Trainer")
    {
        // ── Navigation buttons ───────────────────────────────────
        var btnDash  = AddNavButton("🏠", "Dashboard",   "dashboard");
        var btnTrain = AddNavButton("👥", "Trainers",    "trainers");
        var btnAtten = AddNavButton("📅", "Attendance",  "attendance");
        var btnAdd2  = AddNavButton("➕", "Add Trainer", "addtrainer");

        btnDash.Click  += (_, _) => NavigateToForm<AdminDashboardForm>();
        btnTrain.Click += (_, _) => NavigateToForm<TrainerListForm>();
        btnAtten.Click += (_, _) => NavigateToForm<AttendanceForm>();
        btnAdd2.Click  += (_, _) => NavigateToForm<AddTrainerForm>();

        SetActiveNav("addtrainer");

        // ── Build content ────────────────────────────────────────
        BuildContent();

        // ── Wire content events ──────────────────────────────────
        btnAdd.Click += (_, _) => DoAdd();
    }

    private void BuildContent()
    {
        // lblHeading
        lblHeading = new Label
        {
            Text      = "Add New Trainer",
            Location  = new Point(UITheme.S24, UITheme.S24),
            Size      = new Size(500, 36),
            BackColor = Color.Transparent,
            Font      = UITheme.FontHeading(20f),
            ForeColor = UITheme.TextPrimary
        };

        // card
        card = new RoundedPanel
        {
            Location = new Point(UITheme.S24, 70),
            Size     = new Size(620, 460)
        };

        // ── Row 1: Full Name | Username ──────────────────────────
        AddField(card, "Full Name", 16, 16);
        _tbFN = new StyledTextBox { Location = new Point(16, 34), Size = new Size(260, 40) };
        _tbFN.Inner.PlaceholderText = "Full Name";
        card.Controls.Add(_tbFN);

        AddField(card, "Username", 296, 16);
        _tbUser = new StyledTextBox { Location = new Point(296, 34), Size = new Size(260, 40) };
        _tbUser.Inner.PlaceholderText = "Username";
        card.Controls.Add(_tbUser);

        // ── Row 2: Email | Password ──────────────────────────────
        AddField(card, "Email", 16, 92);
        _tbEmail = new StyledTextBox { Location = new Point(16, 110), Size = new Size(260, 40) };
        _tbEmail.Inner.PlaceholderText = "Email";
        card.Controls.Add(_tbEmail);

        AddField(card, "Password", 296, 92);
        _tbPass = new StyledTextBox { Location = new Point(296, 110), Size = new Size(260, 40), IsPassword = true };
        _tbPass.Inner.PlaceholderText = "Password";
        card.Controls.Add(_tbPass);

        // ── Row 3: Age | Gender ──────────────────────────────────
        AddField(card, "Age", 16, 168);
        _tbAge = new StyledTextBox { Location = new Point(16, 186), Size = new Size(120, 40) };
        _tbAge.Inner.PlaceholderText = "Age";
        card.Controls.Add(_tbAge);

        AddField(card, "Gender", 296, 168);
        _cbGender = new ComboBox
        {
            Location      = new Point(296, 186),
            Size          = new Size(260, 40),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
        _cbGender.SelectedIndex = 0;
        UITheme.StyleComboBox(_cbGender);
        card.Controls.Add(_cbGender);

        // ── Row 4: Height | Weight ───────────────────────────────
        AddField(card, "Height (cm)", 16, 244);
        _tbHeight = new StyledTextBox { Location = new Point(16, 262), Size = new Size(120, 40) };
        _tbHeight.Inner.PlaceholderText = "Height (cm)";
        card.Controls.Add(_tbHeight);

        AddField(card, "Weight (kg)", 296, 244);
        _tbWeight = new StyledTextBox { Location = new Point(296, 262), Size = new Size(120, 40) };
        _tbWeight.Inner.PlaceholderText = "Weight (kg)";
        card.Controls.Add(_tbWeight);

        // ── Status labels ─────────────────────────────────────────
        _lblErr = new Label
        {
            Location  = new Point(16, 322),
            Size      = new Size(560, 24),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(9f),
            ForeColor = UITheme.Red
        };
        card.Controls.Add(_lblErr);

        _lblOk = new Label
        {
            Location  = new Point(16, 348),
            Size      = new Size(560, 24),
            BackColor = Color.Transparent,
            Font      = UITheme.FontBody(9f),
            ForeColor = UITheme.Success
        };
        card.Controls.Add(_lblOk);

        // ── Add button ────────────────────────────────────────────
        btnAdd = new GoldButton
        {
            Text     = "Add Trainer",
            Location = new Point(16, 390),
            Size     = new Size(200, 40)
        };
        card.Controls.Add(btnAdd);

        _contentArea.Controls.Add(lblHeading);
        _contentArea.Controls.Add(card);
    }

    // ── Helper: adds a field label ────────────────────────────────
    private static void AddField(Panel p, string labelText, int x, int y)
    {
        var lbl = new Label
        {
            Text      = labelText,
            Location  = new Point(x, y),
            Size      = new Size(260, 16),
            BackColor = Color.Transparent,
            Font      = UITheme.FontSemiBold(8.5f),
            ForeColor = UITheme.TextSecondary
        };
        p.Controls.Add(lbl);
    }

    // ── Business logic ────────────────────────────────────────────
    private void DoAdd()
    {
        _lblErr.Text = _lblOk.Text = string.Empty;

        if (!int.TryParse(_tbAge.Text, out int age))
        {
            _lblErr.Text = "Invalid age.";
            return;
        }
        if (!double.TryParse(_tbHeight.Text, out double ht))
        {
            _lblErr.Text = "Invalid height.";
            return;
        }
        if (!double.TryParse(_tbWeight.Text, out double wt))
        {
            _lblErr.Text = "Invalid weight.";
            return;
        }

        var (ok, msg, _) = _auth.Register(
            _tbFN.Text, _tbUser.Text, _tbPass.Text, _tbEmail.Text,
            "Trainer", _cbGender.SelectedItem!.ToString()!, age, ht, wt, null, null);

        if (!ok)
        {
            _lblErr.Text = msg;
            return;
        }

        _lblOk.Text = $"✓ Trainer '{_tbUser.Text}' added successfully.";

        // Clear fields
        foreach (Control c in new Control[] { _tbFN, _tbUser, _tbEmail, _tbPass, _tbAge, _tbHeight, _tbWeight })
            if (c is StyledTextBox s) s.Text = "";
    }
}

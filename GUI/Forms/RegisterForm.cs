using FitTrack.GUI.Controls;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using FitTrack.Services;
using FitTrack.Models;

namespace FitTrack.GUI.Forms;

public class RegisterForm : Form
{
    private readonly AuthService _auth = new();
    private readonly GoalService _goals = new();

    private BufferedPanel bg = null!;
    private StyledTextBox _tbFullName = null!;
    private StyledTextBox _tbUser = null!;
    private StyledTextBox _tbEmail = null!;
    private StyledTextBox _tbPass = null!;
    private StyledTextBox _tbHeight = null!;
    private StyledTextBox _tbWeight = null!;
    private StyledTextBox _tbAge = null!;
    private ComboBox _cbRole = null!;
    private ComboBox _cbGender = null!;
    private ComboBox _cbGoal = null!;
    private Label _lblErr = null!;
    private Label _lblPassHint = null!;
    private GoldButton btnReg = null!;
    private GoldButton btnBack = null!;

    public RegisterForm()
    {
        InitializeComponent();
        
        _tbUser.Inner.TextChanged += (_, _) => ValidateUsernameRealtime();
        _tbPass.Inner.TextChanged += (_, _) => ValidatePasswordRealtime();
        btnReg.Click += (_, _) => DoRegister();
        btnBack.Click += (_, _) => { new LandingForm().Show(); Close(); };
    }

    private void InitializeComponent()
    {
        bg = new BufferedPanel();
        _tbFullName = new StyledTextBox();
        _tbUser = new StyledTextBox();
        _tbEmail = new StyledTextBox();
        _tbPass = new StyledTextBox();
        _lblPassHint = new Label();
        _tbAge = new StyledTextBox();
        _cbGender = new ComboBox();
        _tbHeight = new StyledTextBox();
        _tbWeight = new StyledTextBox();
        _cbGoal = new ComboBox();
        _cbRole = new ComboBox();
        _lblErr = new Label();
        btnReg = new GoldButton();
        btnBack = new GoldButton();
        SuspendLayout();

        // bg
        bg.Dock = DockStyle.Fill;
        bg.BackColor = Color.White;
        bg.AutoScroll = true;
        bg.Paint += Bg_Paint;

        int lx = 60, fw = 460, fh = 46;
        int col2x = lx + 250;
        int colW = 210;
        // gap = label(18) + spacing(4) + textbox(46) + breathing(12) = 80
        int gap = 80;
        int y = 70; // start Y after title

        // Title
        var logo = new Label
        {
            Text = "CREATE ACCOUNT", Left = 0, Top = 20, Width = 580, Height = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UITheme.FontBrand(22f), ForeColor = UITheme.Gold,
            BackColor = Color.Transparent
        };
        bg.Controls.Add(logo);

        // Role
        AddLbl("I am a", lx, y, fw);
        _cbRole.Left = lx; _cbRole.Top = y + 22; _cbRole.Width = fw; _cbRole.Height = 34;
        _cbRole.DropDownStyle = ComboBoxStyle.DropDownList;
        UITheme.StyleComboBox(_cbRole);
        _cbRole.Items.AddRange(new[] { "Trainee", "Trainer" });
        _cbRole.SelectedIndex = 0;
        bg.Controls.Add(_cbRole);
        y += gap;

        // Full Name
        AddLbl("Full Name", lx, y, fw);
        _tbFullName.Left = lx; _tbFullName.Top = y + 22; _tbFullName.Width = fw; _tbFullName.Height = fh;
        bg.Controls.Add(_tbFullName);
        y += gap;

        // Username
        AddLbl("Username  (letters + numbers, no symbols)", lx, y, fw);
        _tbUser.Left = lx; _tbUser.Top = y + 22; _tbUser.Width = fw; _tbUser.Height = fh;
        bg.Controls.Add(_tbUser);
        y += gap;

        // Email
        AddLbl("Email", lx, y, fw);
        _tbEmail.Left = lx; _tbEmail.Top = y + 22; _tbEmail.Width = fw; _tbEmail.Height = fh;
        bg.Controls.Add(_tbEmail);
        y += gap;

        // Password
        AddLbl("Password  (8+ chars, upper, lower, number, symbol)", lx, y, fw);
        _tbPass.Left = lx; _tbPass.Top = y + 22; _tbPass.Width = fw; _tbPass.Height = fh;
        _tbPass.IsPassword = true;
        bg.Controls.Add(_tbPass);

        _lblPassHint.Left = lx; _lblPassHint.Top = y + 72; _lblPassHint.Width = fw; _lblPassHint.Height = 16;
        _lblPassHint.Font = UITheme.FontSmall(7.5f);
        _lblPassHint.ForeColor = UITheme.TextMuted;
        _lblPassHint.BackColor = Color.Transparent;
        bg.Controls.Add(_lblPassHint);
        y += gap + 14; // extra room for password hint

        // Age + Gender
        AddLbl("Age", lx, y, 120);
        AddLbl("Gender", col2x, y, colW);
        _tbAge.Left = lx; _tbAge.Top = y + 22; _tbAge.Width = 120; _tbAge.Height = fh;
        bg.Controls.Add(_tbAge);
        _cbGender.Left = col2x; _cbGender.Top = y + 22; _cbGender.Width = colW; _cbGender.Height = 34;
        _cbGender.DropDownStyle = ComboBoxStyle.DropDownList;
        UITheme.StyleComboBox(_cbGender);
        _cbGender.Items.AddRange(new[] { "Male", "Female", "Other" });
        _cbGender.SelectedIndex = 0;
        bg.Controls.Add(_cbGender);
        y += gap;

        // Height + Weight
        AddLbl("Height (cm)", lx, y, 120);
        AddLbl("Weight (kg)", col2x, y, 120);
        _tbHeight.Left = lx; _tbHeight.Top = y + 22; _tbHeight.Width = 120; _tbHeight.Height = fh;
        bg.Controls.Add(_tbHeight);
        _tbWeight.Left = col2x; _tbWeight.Top = y + 22; _tbWeight.Width = 120; _tbWeight.Height = fh;
        bg.Controls.Add(_tbWeight);
        y += gap;

        // Goal
        AddLbl("Fitness Goal", lx, y, fw);
        _cbGoal.Left = lx; _cbGoal.Top = y + 22; _cbGoal.Width = fw; _cbGoal.Height = 34;
        _cbGoal.DropDownStyle = ComboBoxStyle.DropDownList;
        UITheme.StyleComboBox(_cbGoal);
        _cbGoal.Items.Add("— Select Goal —");
        _cbGoal.SelectedIndex = 0;
        LoadGoals();
        bg.Controls.Add(_cbGoal);
        y += gap;

        // Error
        _lblErr.Left = lx; _lblErr.Top = y; _lblErr.Width = fw; _lblErr.Height = 30;
        _lblErr.Font = UITheme.FontBody(9f); _lblErr.ForeColor = UITheme.Red;
        _lblErr.BackColor = Color.Transparent; _lblErr.TextAlign = ContentAlignment.MiddleCenter;
        bg.Controls.Add(_lblErr);
        y += 38;

        // Submit
        btnReg.Text = "CREATE ACCOUNT"; btnReg.Left = lx; btnReg.Top = y;
        btnReg.Width = fw; btnReg.Height = 50;
        btnReg.Font = UITheme.FontSemiBold(10.5f);
        btnReg.Style = GoldButton.ButtonStyle.Gold;
        bg.Controls.Add(btnReg);
        y += 64;

        // Back
        btnBack.Text = "← Back to Login"; btnBack.Left = lx; btnBack.Top = y;
        btnBack.Width = fw; btnBack.Height = 42;
        btnBack.Style = GoldButton.ButtonStyle.Ghost;
        bg.Controls.Add(btnBack);

        // Form
        Text = "Fit Track Pro — Create Account";
        ClientSize = new Size(580, 960);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.White;
        DoubleBuffered = true;
        Controls.Add(bg);
        ResumeLayout(false);
    }

    private void Bg_Paint(object? sender, PaintEventArgs e)
    {
        UITheme.SetHighQuality(e.Graphics);
        using var br = new LinearGradientBrush(
            new Rectangle(0, 0, bg.Width, bg.Height),
            Color.White, Color.FromArgb(243, 241, 250), 160f);
        e.Graphics.FillRectangle(br, 0, 0, bg.Width, bg.Height);
    }

    private void LoadGoals()
    {
        try { foreach (var g in _goals.GetAll()) _cbGoal.Items.Add(g); }
        catch { }
    }

    private void ValidateUsernameRealtime()
    {
        var (ok, _) = AuthService.ValidateUsername(_tbUser.Text);
        _tbUser.Inner.ForeColor = ok || string.IsNullOrEmpty(_tbUser.Text) ? UITheme.TextPrimary : UITheme.Red;
    }

    private void ValidatePasswordRealtime()
    {
        string p = _tbPass.Text;
        if (string.IsNullOrEmpty(p)) { _lblPassHint.Text = ""; return; }
        bool up = p.Any(char.IsUpper), lo = p.Any(char.IsLower);
        bool dg = p.Any(char.IsDigit), sym = p.Any(c => !char.IsLetterOrDigit(c));
        bool len = p.Length >= 8;
        _lblPassHint.Text = $"Length ≥8 {Chk(len)}  Upper {Chk(up)}  Lower {Chk(lo)}  Number {Chk(dg)}  Symbol {Chk(sym)}";
        _lblPassHint.ForeColor = (up && lo && dg && sym && len) ? UITheme.Success : UITheme.Warning;
    }

    private string Chk(bool ok) => ok ? "✓" : "✗";

    private void DoRegister()
    {
        _lblErr.Text = string.Empty;
        if (!int.TryParse(_tbAge.Text, out int age)) { _lblErr.Text = "Enter a valid age."; return; }
        if (!double.TryParse(_tbHeight.Text, out double ht)) { _lblErr.Text = "Enter a valid height (cm)."; return; }
        if (!double.TryParse(_tbWeight.Text, out double wt)) { _lblErr.Text = "Enter a valid weight (kg)."; return; }

        int? goalId = _cbGoal.SelectedItem is Goal g ? g.GoalId : null;
        string role = _cbRole.SelectedItem?.ToString() ?? "Trainee";

        var (ok, msg, _) = _auth.Register(
            _tbFullName.Text.Trim(), _tbUser.Text.Trim(), _tbPass.Text, _tbEmail.Text.Trim(),
            role, _cbGender.SelectedItem?.ToString() ?? "Other", age, ht, wt, null, goalId);

        if (!ok) { _lblErr.Text = msg; return; }
        MessageBox.Show("Account created successfully! Please sign in.",
            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        new LoginForm().Show();
        Close();
    }

    private void AddLbl(string text, int x, int y, int width) =>
        bg.Controls.Add(new Label
        {
            Text = text, Left = x, Top = y, Width = width, Height = 18,
            Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        });
}

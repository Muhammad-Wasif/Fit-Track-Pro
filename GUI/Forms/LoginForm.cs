using FitTrack.GUI.Controls;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FitTrack.Services;
using FitTrack.Models;

namespace FitTrack.GUI.Forms;

public class LoginForm : Form
{
    private readonly AuthService _auth = new();

    private BufferedPanel bg = null!;
    private StyledTextBox _tbUser = null!;
    private StyledTextBox _tbPass = null!;
    private Label _lblErr = null!;
    private GoldButton _btnLogin = null!;
    private GoldButton btnBack = null!;

    public LoginForm()
    {
        InitializeComponent();
        
        // ── Move styling and event bindings here ──
        this._btnLogin.Click += (_, _) => DoLogin();
        this.btnBack.Click += (_, _) => { new LandingForm().Show(); Close(); };
        this.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) DoLogin(); };
    }

    private void InitializeComponent()
    {
        bg = new BufferedPanel();
        _tbUser = new StyledTextBox();
        _tbPass = new StyledTextBox();
        _lblErr = new Label();
        _btnLogin = new GoldButton();
        SuspendLayout();

        // bg
        bg.Dock = DockStyle.Fill;
        bg.BackColor = Color.White;
        bg.Paint += PaintBg;

        int cx = 80, fw = 320, fh = 46;

        // Title
        var logo = new Label
        {
            Text = "FIT TRACK PRO", Left = 0, Top = 60, Width = 480, Height = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UITheme.FontBrand(24f), ForeColor = UITheme.Gold,
            BackColor = Color.Transparent
        };

        var sub = new Label
        {
            Text = "Sign in to your account", Left = 0, Top = 108, Width = 480, Height = 24,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UITheme.FontBody(10.5f), ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };

        // Username
        var lblUser = new Label
        {
            Text = "Username", Left = cx, Top = 160, Width = fw, Height = 18,
            Font = UITheme.FontSemiBold(9f), ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };
        _tbUser.Left = cx; _tbUser.Top = 182; _tbUser.Width = fw; _tbUser.Height = fh;

        // Password
        var lblPass = new Label
        {
            Text = "Password", Left = cx, Top = 248, Width = fw, Height = 18,
            Font = UITheme.FontSemiBold(9f), ForeColor = UITheme.TextSecondary,
            BackColor = Color.Transparent
        };
        _tbPass.Left = cx; _tbPass.Top = 270; _tbPass.Width = fw; _tbPass.Height = fh;
        _tbPass.IsPassword = true;

        // Error label
        _lblErr.Left = cx; _lblErr.Top = 334; _lblErr.Width = fw; _lblErr.Height = 28;
        _lblErr.Font = UITheme.FontBody(9f); _lblErr.ForeColor = UITheme.Red;
        _lblErr.BackColor = Color.Transparent;
        _lblErr.TextAlign = ContentAlignment.MiddleCenter;

        // Sign In button
        _btnLogin.Text = "SIGN IN"; _btnLogin.Left = cx; _btnLogin.Top = 372;
        _btnLogin.Width = fw; _btnLogin.Height = 50;
        _btnLogin.Font = UITheme.FontSemiBold(10.5f);
        _btnLogin.Style = GoldButton.ButtonStyle.Gold;

        // Back button
        btnBack = new GoldButton
        {
            Text = "← Back", Left = cx, Top = 438, Width = fw, Height = 42,
            Style = GoldButton.ButtonStyle.Ghost
        };

        // Add all to bg
        bg.Controls.AddRange(new Control[] { logo, sub, lblUser, _tbUser, lblPass, _tbPass, _lblErr, _btnLogin, btnBack });

        // Form
        Text = "Fit Track Pro — Sign In";
        ClientSize = new Size(480, 530);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.White;
        DoubleBuffered = true;
        KeyPreview = true;
        Controls.Add(bg);
        ResumeLayout(false);
    }

    private void PaintBg(object? sender, PaintEventArgs e)
    {
        UITheme.SetHighQuality(e.Graphics);
        using var br = new LinearGradientBrush(
            new Rectangle(0, 0, bg.Width, bg.Height),
            Color.White, Color.FromArgb(243, 241, 250), 160f);
        e.Graphics.FillRectangle(br, 0, 0, bg.Width, bg.Height);

        using var arcPen = new Pen(Color.FromArgb(12, UITheme.Gold), 140f);
        e.Graphics.DrawEllipse(arcPen, -200, -350, 900, 600);
    }

    private void DoLogin()
    {
        _lblErr.Text = string.Empty;
        string user = _tbUser.Text.Trim(), pass = _tbPass.Text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        { _lblErr.Text = "Please enter username and password."; return; }

        _btnLogin.Enabled = false;
        Cursor = Cursors.WaitCursor;
        try
        {
            var (ok, msg, person) = _auth.Login(user, pass);
            if (!ok || person == null) { _lblErr.Text = msg; return; }

            Form next = person.Role switch
            {
                "Admin"   => new Admin.AdminDashboardForm(person),
                "Trainer" => new Trainer.TrainerDashboardForm(person),
                _         => new Trainee.TraineeDashboardForm(person)
            };
            next.Show();
            Close();
        }
        catch (Exception ex) { _lblErr.Text = $"Error: {ex.Message}"; }
        finally { _btnLogin.Enabled = true; Cursor = Cursors.Default; }
    }
}

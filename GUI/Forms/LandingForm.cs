using FitTrack.GUI.Controls;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms;

/// <summary>
/// App entry point — premium animated landing with smooth glow.
/// Uses BufferedPanel for flicker-free rendering.
/// </summary>
public class LandingForm : Form
{
    // Smooth sine-based animation (no blink)
    private readonly System.Windows.Forms.Timer _animTimer = new() { Interval = 16 }; // ~60fps
    private float _animT = 0f; // 0→1→0 continuous cycle

    private BufferedPanel bg = null!;
    private BufferedPanel logoPanel = null!;
    private Label tagline = null!;
    private Panel div = null!;
    private GoldButton btnSignIn = null!;
    private GoldButton btnSignUp = null!;
    private Label footer = null!;

    // Cached background bitmap (paint once, reuse)
    private Bitmap? _bgCache;
    private Size _bgCacheSize;

    public LandingForm()
    {
        InitializeComponent();
        _animTimer.Tick += (_, _) => AnimateLogo();
        _animTimer.Start();
    }

    private void InitializeComponent()
    {
        bg = new BufferedPanel();
        logoPanel = new BufferedPanel();
        tagline = new Label();
        div = new Panel();
        btnSignIn = new GoldButton();
        btnSignUp = new GoldButton();
        footer = new Label();
        bg.SuspendLayout();
        SuspendLayout();

        // ── bg (full backdrop) ───────────────────────────────────
        bg.BackColor = Color.White;
        bg.Controls.Add(logoPanel);
        bg.Controls.Add(tagline);
        bg.Controls.Add(div);
        bg.Controls.Add(btnSignIn);
        bg.Controls.Add(btnSignUp);
        bg.Controls.Add(footer);
        bg.Dock = DockStyle.Fill;
        bg.Name = "bg";
        bg.Paint += PaintBackground;

        // ── logoPanel (animated area only) ───────────────────────
        logoPanel.BackColor = Color.Transparent;
        logoPanel.Location = new Point(0, 60);
        logoPanel.Size = new Size(500, 200);
        logoPanel.Name = "logoPanel";
        logoPanel.Paint += PaintLogo;

        // ── tagline ──────────────────────────────────────────────
        tagline.BackColor = Color.Transparent;
        tagline.Font = UITheme.FontBody(10.5f);
        tagline.ForeColor = UITheme.TextSecondary;
        tagline.Location = new Point(0, 274);
        tagline.Size = new Size(500, 26);
        tagline.Text = "Your Premium Fitness Companion";
        tagline.TextAlign = ContentAlignment.MiddleCenter;

        // ── separator ────────────────────────────────────────────
        div.BackColor = Color.FromArgb(230, 228, 240);
        div.Location = new Point(100, 318);
        div.Size = new Size(300, 1);

        // ── SIGN IN ──────────────────────────────────────────────
        btnSignIn.Location = new Point(76, 342);
        btnSignIn.Size = new Size(348, 52);
        btnSignIn.Style = GoldButton.ButtonStyle.Gold;
        btnSignIn.Text = "SIGN IN";
        btnSignIn.Font = UITheme.FontSemiBold(10.5f);
        btnSignIn.Click += btnSignIn_Click;

        // ── CREATE ACCOUNT ───────────────────────────────────────
        btnSignUp.Location = new Point(76, 412);
        btnSignUp.Size = new Size(348, 48);
        btnSignUp.Style = GoldButton.ButtonStyle.Ghost;
        btnSignUp.Text = "CREATE ACCOUNT";
        btnSignUp.Font = UITheme.FontSemiBold(10f);
        btnSignUp.Click += btnSignUp_Click;

        // ── footer ───────────────────────────────────────────────
        footer.BackColor = Color.Transparent;
        footer.Font = UITheme.FontSmall(8f);
        footer.ForeColor = UITheme.TextMuted;
        footer.Location = new Point(0, 560);
        footer.Size = new Size(500, 20);
        footer.Text = "© 2025 Fit Track Pro — All Rights Reserved";
        footer.TextAlign = ContentAlignment.MiddleCenter;

        // ── LandingForm ──────────────────────────────────────────
        BackColor = Color.White;
        ClientSize = new Size(500, 600);
        Controls.Add(bg);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "LandingForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Fit Track Pro";

        bg.ResumeLayout(false);
        ResumeLayout(false);
    }

    // ─── NAVIGATION ──────────────────────────────────────────────

    private void btnSignIn_Click(object? sender, EventArgs e)
    {
        _animTimer.Stop();
        new LoginForm().Show();
        Hide();
    }

    private void btnSignUp_Click(object? sender, EventArgs e)
    {
        _animTimer.Stop();
        new RegisterForm().Show();
        Hide();
    }

    // ─── BACKGROUND (cached — only redraws on resize) ────────────

    private void PaintBackground(object? sender, PaintEventArgs e)
    {
        var sz = bg.Size;
        if (_bgCache == null || _bgCacheSize != sz)
        {
            _bgCache?.Dispose();
            _bgCache = new Bitmap(sz.Width, sz.Height);
            _bgCacheSize = sz;
            using var g = Graphics.FromImage(_bgCache);
            UITheme.SetHighQuality(g);

            // Premium gradient — white to cool lavender
            using var bgBrush = new LinearGradientBrush(
                new Rectangle(0, 0, sz.Width, sz.Height),
                Color.White, Color.FromArgb(243, 241, 250), 160f);
            g.FillRectangle(bgBrush, 0, 0, sz.Width, sz.Height);

            // Top decorative arc — gold aura
            using var arcPen = new Pen(Color.FromArgb(18, UITheme.Gold), 180f);
            g.DrawEllipse(arcPen, -250, -380, 1000, 660);

            // Bottom subtle accent
            using var botBrush = new LinearGradientBrush(
                new Rectangle(0, sz.Height - 80, sz.Width, 80),
                Color.FromArgb(0, 212, 175, 55),
                Color.FromArgb(12, 212, 175, 55), 90f);
            g.FillRectangle(botBrush, 0, sz.Height - 80, sz.Width, 80);
        }
        e.Graphics.DrawImageUnscaled(_bgCache, 0, 0);
    }

    // ─── LOGO (smooth animation — no flicker) ────────────────────

    private void PaintLogo(object? sender, PaintEventArgs e)
    {
        UITheme.SetHighQuality(e.Graphics);
        Graphics g = e.Graphics;
        int w = logoPanel.Width;

        // Smooth glow halo using sine easing
        float glow = UITheme.EaseInOut(_animT);
        int glowAlpha = (int)(glow * 35);
        if (glowAlpha > 0)
        {
            using var glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, UITheme.Gold));
            g.FillEllipse(glowBrush, w / 2 - 180, 10, 360, 120);

            // Inner sharper glow
            int innerAlpha = (int)(glow * 18);
            using var innerGlow = new SolidBrush(Color.FromArgb(innerAlpha, UITheme.GoldLight));
            g.FillEllipse(innerGlow, w / 2 - 120, 30, 240, 80);
        }

        // "FIT TRACK" — gold gradient text
        using var f1 = UITheme.FontBrand(36f);
        string line1 = "FIT TRACK";
        SizeF sz1 = g.MeasureString(line1, f1);
        float x1 = (w - sz1.Width) / 2;

        using var textBrush = new LinearGradientBrush(
            new PointF(x1, 15), new PointF(x1 + sz1.Width, 15),
            UITheme.GoldDark, UITheme.GoldLight);
        g.DrawString(line1, f1, textBrush, x1, 15);

        // "PRO" — red accent, slightly larger
        using var f2 = UITheme.FontBrand(44f);
        string line2 = "PRO";
        SizeF sz2 = g.MeasureString(line2, f2);
        float x2 = (w - sz2.Width) / 2;
        using var redBr = new SolidBrush(UITheme.Red);
        g.DrawString(line2, f2, redBr, x2, 62);

        // Decorative gold underline with animated width
        float lineW = 100 + glow * 40;
        float ulX = (w - lineW) / 2;
        using var ulPen = new Pen(UITheme.Gold, 2.5f);
        ulPen.StartCap = ulPen.EndCap = LineCap.Round;
        g.DrawLine(ulPen, ulX, 140, ulX + lineW, 140);

        // Small diamond accent
        float dX = w / 2f;
        float dY = 156;
        float ds = 3.5f;
        PointF[] diamond = { new(dX, dY - ds), new(dX + ds, dY), new(dX, dY + ds), new(dX - ds, dY) };
        using var diamondBrush = new SolidBrush(UITheme.Gold);
        g.FillPolygon(diamondBrush, diamond);
    }

    // ─── SMOOTH ANIMATION ────────────────────────────────────────

    private void AnimateLogo()
    {
        _animT += 0.006f; // slow, smooth cycle
        if (_animT > 1f) _animT -= 1f;

        // Only invalidate the logo panel (not the entire form)
        logoPanel.Invalidate();
    }

    // ─── CLEANUP ─────────────────────────────────────────────────

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _animTimer.Stop();
        _bgCache?.Dispose();
        base.OnFormClosed(e);
        Application.Exit();
    }
}

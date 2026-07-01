using FitTrack.GUI.Controls;
using FitTrack.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FitTrack.GUI;

/// <summary>
/// Shared application shell — sidebar, topbar, clock, navigation, fade transitions.
/// All role dashboards inherit this. Only the content area changes per screen.
/// Business/Data logic is NOT touched here — pure GUI infrastructure.
/// </summary>
public abstract class AppShell : Form
{
    // ── Shared user context ───────────────────────────────────────
    protected readonly Person CurrentUser;

    // ── Shell panels (protected so subclasses can add content) ───
    protected Panel _sidebar     = null!;
    protected Panel _topBar      = null!;
    protected Panel _contentArea = null!;

    // ── Topbar controls ──────────────────────────────────────────
    private Label  _lblPageTitle = null!;
    private Label  _lblClock     = null!;
    private Label  _lblDate      = null!;
    private Label  _lblDay       = null!;
    private GoldButton _btnLogout = null!;
    private System.Windows.Forms.Timer _clockTimer = null!;

    // ── Nav tracking ─────────────────────────────────────────────
    protected int  _navY          = UITheme.SidebarHeaderHeight + 8;
    protected bool _isNavigating  = false;

    // ─────────────────────────────────────────────────────────────
    //  CONSTRUCTOR
    // ─────────────────────────────────────────────────────────────
    protected AppShell(Person user, string pageTitle)
    {
        CurrentUser = user;
        InitShell();
        _lblPageTitle.Text = pageTitle;

        // Clock
        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        // Responsive topbar layout
        _topBar.SizeChanged += (_, _) => LayoutTopBar();
        LayoutTopBar();
    }

    // ─────────────────────────────────────────────────────────────
    //  SHELL INIT
    // ─────────────────────────────────────────────────────────────
    private void InitShell()
    {
        // ── Controls ──
        _sidebar     = new Panel();
        _topBar      = new Panel();
        _contentArea = new Panel();
        _lblPageTitle = new Label();
        _lblClock    = new Label();
        _lblDate     = new Label();
        _lblDay      = new Label();
        _btnLogout   = new GoldButton();

        // ── TOP BAR ─────────────────────────────────────────────
        _topBar.Dock      = DockStyle.Top;
        _topBar.Height    = UITheme.TopBarHeight;
        _topBar.BackColor = Color.White;
        _topBar.Paint    += OnTopBarPaint;

        // Page title (left side)
        _lblPageTitle.Location  = new Point(UITheme.S24, 0);
        _lblPageTitle.Size      = new Size(400, UITheme.TopBarHeight);
        _lblPageTitle.TextAlign = ContentAlignment.MiddleLeft;
        _lblPageTitle.BackColor = Color.Transparent;
        _lblPageTitle.Font      = UITheme.FontHeading(16f);   // Georgia Bold — elegant
        _lblPageTitle.ForeColor = UITheme.TextPrimary;

        // Clock (right side — positions set in LayoutTopBar)
        _lblClock.Size      = new Size(160, 22);
        _lblClock.TextAlign = ContentAlignment.MiddleRight;
        _lblClock.BackColor = Color.Transparent;
        _lblClock.Font      = UITheme.FontMono(12f);
        _lblClock.ForeColor = UITheme.TextPrimary;

        _lblDate.Size      = new Size(160, 14);
        _lblDate.TextAlign = ContentAlignment.MiddleRight;
        _lblDate.BackColor = Color.Transparent;
        _lblDate.Font      = UITheme.FontSmall(8f);
        _lblDate.ForeColor = UITheme.TextSecondary;

        _lblDay.Size      = new Size(160, 14);
        _lblDay.TextAlign = ContentAlignment.MiddleRight;
        _lblDay.BackColor = Color.Transparent;
        _lblDay.Font      = UITheme.FontSemiBold(7.5f);
        _lblDay.ForeColor = UITheme.Gold;

        // Logout button
        _btnLogout.Text   = "Logout";
        _btnLogout.Size   = new Size(88, 36);
        _btnLogout.Style  = GoldButton.ButtonStyle.Red;
        _btnLogout.Click += (_, _) => LogoutClick();

        _topBar.Controls.AddRange(new Control[]
        {
            _lblPageTitle, _lblClock, _lblDate, _lblDay, _btnLogout
        });

        // ── SIDEBAR ─────────────────────────────────────────────
        _sidebar.Dock      = DockStyle.Left;
        _sidebar.Width     = UITheme.SidebarExpanded;
        _sidebar.BackColor = UITheme.BgSidebar;
        _sidebar.Paint    += OnSidebarPaint;

        // ── CONTENT AREA ────────────────────────────────────────
        _contentArea.Dock       = DockStyle.Fill;
        _contentArea.BackColor  = UITheme.BgPage;
        _contentArea.Padding    = new Padding(UITheme.S32, UITheme.S24, UITheme.S32, UITheme.S24);
        _contentArea.AutoScroll = true;

        // ── FORM ────────────────────────────────────────────────
        Text            = "Fit Track Pro";
        WindowState     = FormWindowState.Maximized;
        MinimumSize     = new Size(1080, 700);
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor       = UITheme.BgPage;
        DoubleBuffered  = true;

        // Z-order: content fills → sidebar docks left → topbar docks top
        Controls.Add(_contentArea);
        Controls.Add(_sidebar);
        Controls.Add(_topBar);
    }

    // ─────────────────────────────────────────────────────────────
    //  SIDEBAR PAINT — header with brand / role badge / username
    // ─────────────────────────────────────────────────────────────
    private void OnSidebarPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        UITheme.SetHighQuality(g);
        int w = _sidebar.Width;
        int h = UITheme.SidebarHeaderHeight;

        // ── Header background (slightly lighter than sidebar) ──
        using var hdrBrush = new SolidBrush(Color.FromArgb(26, 28, 46));
        g.FillRectangle(hdrBrush, 0, 0, w, h);

        // ── App name — Georgia Bold Italic Gold ──
        using var appFont   = UITheme.FontBrand(13f);
        using var goldBrush = new SolidBrush(UITheme.Gold);
        var sfLeft = new StringFormat { LineAlignment = StringAlignment.Center };
        g.DrawString("FIT TRACK PRO", appFont, goldBrush,
            new RectangleF(UITheme.S16, 10, w - UITheme.S32, 26), sfLeft);

        // ── Role badge pill ──
        Color roleColor = CurrentUser.Role switch
        {
            "Admin"   => UITheme.Red,
            "Trainer" => UITheme.AccentBlue,
            _         => UITheme.Success
        };
        string roleText = CurrentUser.Role?.ToUpper() ?? "USER";
        int badgeW = 72, badgeH = 20;
        var badgeRect = new Rectangle(UITheme.S16, 42, badgeW, badgeH);
        using var badgePath  = UITheme.RoundedRect(badgeRect, 10);
        using var badgeFill  = new SolidBrush(Color.FromArgb(50, roleColor));
        using var badgeBorder = new Pen(Color.FromArgb(120, roleColor), 1f);
        g.FillPath(badgeFill, badgePath);
        g.DrawPath(badgeBorder, badgePath);
        using var roleFont  = UITheme.FontSmall(7.5f);
        using var roleTextBr= new SolidBrush(roleColor);
        var sfCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(roleText, roleFont, roleTextBr, badgeRect, sfCenter);

        // ── Username ──
        using var nameFont = UITheme.FontSemiBold(9f);
        using var nameBr   = new SolidBrush(Color.FromArgb(195, 200, 220));
        string name = CurrentUser.FullName ?? "User";
        if (name.Length > 22) name = name.Substring(0, 22) + "…";
        g.DrawString(name, nameFont, nameBr,
            new RectangleF(UITheme.S16, 68, w - UITheme.S32, 22), sfLeft);

        // ── Gold separator line ──
        using var sepPen = new Pen(Color.FromArgb(45, UITheme.Gold), 1f);
        g.DrawLine(sepPen, UITheme.S16, h - 1, w - UITheme.S16, h - 1);
    }

    // ─────────────────────────────────────────────────────────────
    //  TOPBAR PAINT — subtle bottom border
    // ─────────────────────────────────────────────────────────────
    private void OnTopBarPaint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(UITheme.BorderLight, 1f);
        e.Graphics.DrawLine(pen, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
    }

    // ─────────────────────────────────────────────────────────────
    //  NAV BUTTON FACTORY
    // ─────────────────────────────────────────────────────────────
    protected SidebarNavButton AddNavButton(string emoji, string text, string key)
    {
        var btn = new SidebarNavButton
        {
            NavText    = text,
            Emoji      = emoji,
            IsExpanded = true,
            Tag        = key,
            Location   = new Point(8, _navY),
            Size       = new Size(UITheme.SidebarExpanded - 16, 48)
        };
        _sidebar.Controls.Add(btn);
        _navY += 52;
        return btn;
    }

    protected void SetActiveNav(string key)
    {
        foreach (Control c in _sidebar.Controls)
        {
            if (c is SidebarNavButton btn)
                btn.IsActive = string.Equals(btn.Tag as string, key, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  PAGE TITLE SETTER
    // ─────────────────────────────────────────────────────────────
    protected void SetPageTitle(string title) => _lblPageTitle.Text = title;

    // ─────────────────────────────────────────────────────────────
    //  FADE NAVIGATION (200ms cross-fade)
    // ─────────────────────────────────────────────────────────────
    protected void NavigateToForm<TForm>() where TForm : Form
    {
        if (_isNavigating) return;
        _isNavigating = true;
        _clockTimer.Stop();

        // Instantiate next form (all take Person user as first constructor arg)
        var next = (Form)Activator.CreateInstance(typeof(TForm), new object[] { CurrentUser })!;
        next.Opacity     = 0;
        next.WindowState = WindowState;
        if (WindowState == FormWindowState.Normal)
        {
            next.Size     = Size;
            next.Location = Location;
        }
        next.Show();

        // Cross-fade: this form fades out, next fades in simultaneously
        float alpha = 1f;
        var t = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 fps
        t.Tick += (_, _) =>
        {
            alpha -= 0.08f;   // ~200ms total (1.0 / 0.08 * 16ms ≈ 200ms)
            if (alpha <= 0f)
            {
                t.Stop();
                Close();
                return;
            }
            Opacity      = alpha;
            next.Opacity = 1.0 - alpha;
        };
        t.Start();
    }

    // ─────────────────────────────────────────────────────────────
    //  LOGOUT (fade to landing)
    // ─────────────────────────────────────────────────────────────
    private void LogoutClick()
    {
        if (_isNavigating) return;
        _isNavigating = true;
        _clockTimer.Stop();

        var landing = new Forms.LandingForm();
        landing.Opacity = 0;
        landing.Show();

        float alpha = 1f;
        var t = new System.Windows.Forms.Timer { Interval = 16 };
        t.Tick += (_, _) =>
        {
            alpha -= 0.08f;
            if (alpha <= 0f) { t.Stop(); Close(); return; }
            Opacity         = alpha;
            landing.Opacity = 1.0 - alpha;
        };
        t.Start();
    }

    // ─────────────────────────────────────────────────────────────
    //  CLOCK
    // ─────────────────────────────────────────────────────────────
    private void UpdateClock()
    {
        if (_lblClock == null || _lblClock.IsDisposed) return;
        var now = DateTime.Now;
        _lblClock.Text = now.ToString("hh:mm:ss tt");
        _lblDate.Text  = now.ToString("dd MMM yyyy");
        _lblDay.Text   = now.DayOfWeek.ToString().ToUpper();
    }

    // ─────────────────────────────────────────────────────────────
    //  TOPBAR LAYOUT (responsive)
    // ─────────────────────────────────────────────────────────────
    private void LayoutTopBar()
    {
        if (_topBar == null || _btnLogout == null) return;
        int w = _topBar.Width;
        int clockLeft = w - 190;
        _lblClock.Location    = new Point(clockLeft, 10);
        _lblDate.Location     = new Point(clockLeft, 33);
        _lblDay.Location      = new Point(clockLeft, 47);
        _btnLogout.Location   = new Point(w - 104, (UITheme.TopBarHeight - 36) / 2);
        _lblPageTitle.Width   = Math.Max(_btnLogout.Left - UITheme.S24 - UITheme.S24, 120);
    }

    // ─────────────────────────────────────────────────────────────
    //  FORM CLOSING
    // ─────────────────────────────────────────────────────────────
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.CloseReason == CloseReason.UserClosing && !_isNavigating)
            Application.Exit();
    }
}

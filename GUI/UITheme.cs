using System.Drawing.Drawing2D;

namespace FitTrack.GUI;

/// <summary>
/// Central design system for Fit Track Pro.
/// Premium dark/gold/white palette with modern typography.
/// </summary>
public static class UITheme
{
    // ─── PALETTE ──────────────────────────────────────────────────
    public static readonly Color White      = Color.White;
    public static readonly Color Gold       = Color.FromArgb(212, 175, 55);   // #D4AF37
    public static readonly Color GoldLight  = Color.FromArgb(255, 223, 100);  // lighter gold
    public static readonly Color GoldDark   = Color.FromArgb(160, 128, 20);   // darker gold
    public static readonly Color Red        = Color.FromArgb(192, 57, 43);    // #C0392B
    public static readonly Color RedLight   = Color.FromArgb(231, 76, 60);
    public static readonly Color BgPage     = Color.FromArgb(247, 248, 252);  // cool near-white
    public static readonly Color BgSidebar  = Color.FromArgb(16, 18, 30);     // deep dark sidebar
    public static readonly Color BgCard     = Color.White;
    public static readonly Color TextPrimary  = Color.FromArgb(24, 24, 38);
    public static readonly Color TextSecondary = Color.FromArgb(100, 100, 120);
    public static readonly Color TextMuted    = Color.FromArgb(155, 155, 175);
    public static readonly Color BorderLight  = Color.FromArgb(220, 222, 235);
    public static readonly Color Success    = Color.FromArgb(16, 185, 129);   // emerald
    public static readonly Color Warning    = Color.FromArgb(245, 158, 11);   // amber

    // ─── PREMIUM ACCENT COLORS ────────────────────────────────────
    public static readonly Color AccentBlue   = Color.FromArgb(59, 130, 246);
    public static readonly Color AccentPurple = Color.FromArgb(139, 92, 246);
    public static readonly Color BgDark       = Color.FromArgb(15, 23, 42);   // slate-900

    // ─── FONTS ────────────────────────────────────────────────────
    public static Font FontBrand(float size)   => new Font("Georgia", size, FontStyle.Bold | FontStyle.Italic);
    public static Font FontHeading(float size) => new Font("Georgia", size, FontStyle.Bold);
    public static Font FontSemiBold(float size)=> new Font("Segoe UI Semibold", size, FontStyle.Regular);
    public static Font FontBody(float size)    => new Font("Segoe UI", size, FontStyle.Regular);
    public static Font FontSmall(float size)   => new Font("Segoe UI", size, FontStyle.Regular);
    public static Font FontMono(float size)    => new Font("Consolas", size, FontStyle.Regular);

    // ─── STANDARD SIZES ──────────────────────────────────────────
    public const int S8               = 8;
    public const int S16              = 16;
    public const int S24              = 24;
    public const int S32              = 32;
    public const int SidebarExpanded  = 240;
    public const int SidebarCollapsed = 68;
    public const int SidebarHeaderHeight = 80;
    public const int TopBarHeight     = 64;
    public const int CardRadius       = 14;
    public const int ButtonRadius     = 10;
    public const int InputHeight      = 40;
    public const int ButtonHeight     = 40;   // alias — all GoldButtons use this height
    public const int TextBoxHeight    = 40;   // alias — all StyledTextBoxes use this height
    public const int CardWidth        = 220;
    public const int CardHeight       = 96;


    // ─── DRAWING HELPERS ─────────────────────────────────────────

    /// <summary>
    /// Universal responsive reflow: wraps MetricCards, stretches panels/grids/labels to container width.
    /// Shifts all content below cards when cards wrap to additional rows.
    /// </summary>
    public static void ResponsiveReflow(Control container)
    {
        int w = container.ClientSize.Width;
        if (w < 100) return;

        // 1. Collect MetricCards vs everything else
        var cards = new List<Control>();
        var others = new List<(Control ctrl, int origTop)>();
        foreach (Control c in container.Controls)
        {
            if (c is Controls.MetricCard) cards.Add(c);
            else others.Add((c, 0));
        }

        // 2. Reflow MetricCards into rows
        int cardsBottomY = 0;
        int origCardsBottom = 0;
        if (cards.Count > 0)
        {
            cards.Sort((a, b) => a.Top == b.Top ? a.Left.CompareTo(b.Left) : a.Top.CompareTo(b.Top));
            int firstTop = cards[0].Top;
            int cardH = cards[0].Height;
            origCardsBottom = firstTop + cardH; // single-row bottom

            int cx = 0, cy = firstTop;
            foreach (var mc in cards)
            {
                if (cx + mc.Width > w && cx > 0)
                {
                    cx = 0;
                    cy += cardH + 10;
                }
                mc.Left = cx;
                mc.Top = cy;
                cx += mc.Width + 10;
            }
            cardsBottomY = cy + cardH + 14; // new bottom after wrapping
        }

        int shiftY = cardsBottomY > 0 ? cardsBottomY - origCardsBottom - 14 : 0;

        // 3. Stretch + shift non-card controls
        // Store original Top on first resize via Tag convention: "origY:###"
        foreach (var (c, _) in others)
        {
            // Save original top position if not saved yet
            string? tag = c.Tag as string;
            int origTop;
            if (tag != null && tag.StartsWith("origY:"))
            {
                origTop = int.Parse(tag.Substring(6));
            }
            else
            {
                origTop = c.Top;
                c.Tag = $"origY:{origTop}";
            }

            // Only shift controls that were below the cards
            bool belowCards = origTop >= origCardsBottom;

            if (c is Controls.RoundedPanel rp)
            {
                rp.Width = Math.Max(w - 10, 300);
                if (belowCards && shiftY != 0) rp.Top = origTop + shiftY;
            }
            else if (c is DataGridView dgv)
            {
                dgv.Width = Math.Max(w - 10, 300);
                if (belowCards && shiftY != 0) dgv.Top = origTop + shiftY;
            }
            else if (c is Label lbl && lbl.Width > 200)
            {
                lbl.Width = Math.Max(w - 10, 200);
                if (belowCards && shiftY != 0) lbl.Top = origTop + shiftY;
            }
            else if (c is Controls.GoldButton btn)
            {
                if (belowCards && shiftY != 0) btn.Top = origTop + shiftY;
            }
        }
    }


    public static void FillRoundRect(Graphics g, Brush fill, Rectangle r, int radius)
    {
        using GraphicsPath path = RoundedRect(r, radius);
        g.FillPath(fill, path);
    }

    public static void DrawRoundRect(Graphics g, Pen pen, Rectangle r, int radius)
    {
        using GraphicsPath path = RoundedRect(r, radius);
        g.DrawPath(pen, path);
    }

    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        GraphicsPath path = new();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void DrawShadow(Graphics g, Rectangle r, int radius, int blur = 6)
    {
        for (int i = blur; i > 0; i--)
        {
            int alpha = (int)(12.0 * i / blur);
            Rectangle sr = new(r.X + 1, r.Y + 1, r.Width, r.Height);
            using var pen = new Pen(Color.FromArgb(alpha, 0, 0, 0), 1);
            DrawRoundRect(g, pen, sr, radius + 1);
        }
    }

    public static void DrawSeparator(Graphics g, int x, int y, int length, bool horizontal = true)
    {
        using var pen = new Pen(BorderLight, 1);
        if (horizontal) g.DrawLine(pen, x, y, x + length, y);
        else g.DrawLine(pen, x, y, x, y + length);
    }

    public static void DrawSidebarDivider(Graphics g, int width)
    {
        using var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1);
        g.DrawLine(pen, 20, 0, width - 20, 0);
    }

    public static void SetHighQuality(Graphics g)
    {
        g.SmoothingMode      = SmoothingMode.AntiAlias;
        g.TextRenderingHint  = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
    }

    public static LinearGradientBrush GoldGradient(Rectangle r, float angle = 135f)
        => new(r, GoldDark, GoldLight, angle);

    public static LinearGradientBrush RedGradient(Rectangle r, float angle = 135f)
        => new(r, Red, RedLight, angle);

    // ─── APPLY FLAT STYLE TO STANDARD CONTROLS ───────────────────
    public static void StyleLabel(Label lbl, Color? color = null, float size = 9.5f)
    {
        lbl.Font      = FontBody(size);
        lbl.ForeColor = color ?? TextPrimary;
        lbl.BackColor = Color.Transparent;
    }

    public static void StyleTextBox(TextBox tb)
    {
        tb.Font            = FontBody(10f);
        tb.ForeColor       = TextPrimary;
        tb.BackColor       = Color.White;
        tb.BorderStyle     = BorderStyle.FixedSingle;
        tb.Height          = InputHeight;
    }

    public static void StyleComboBox(ComboBox cb)
    {
        cb.Font      = FontBody(10f);
        cb.ForeColor = TextPrimary;
        cb.BackColor = Color.White;
        cb.FlatStyle = FlatStyle.Flat;
    }

    public static void StyleDataGridView(DataGridView dgv)
    {
        dgv.BorderStyle                  = BorderStyle.None;
        dgv.BackgroundColor              = BgPage;
        dgv.GridColor                    = BorderLight;
        dgv.RowHeadersVisible            = false;
        dgv.EnableHeadersVisualStyles    = false;
        dgv.DefaultCellStyle.Font        = FontBody(9.5f);
        dgv.DefaultCellStyle.ForeColor   = TextPrimary;
        dgv.DefaultCellStyle.BackColor   = Color.White;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 247, 220);
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.DefaultCellStyle.Padding     = new Padding(6, 4, 6, 4);
        dgv.ColumnHeadersDefaultCellStyle.Font      = FontSemiBold(9f);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 254);
        dgv.ColumnHeadersBorderStyle     = DataGridViewHeaderBorderStyle.Single;
        dgv.ColumnHeadersHeight          = 42;
        dgv.RowTemplate.Height           = 40;
        dgv.AutoSizeColumnsMode          = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.SelectionMode                = DataGridViewSelectionMode.FullRowSelect;
        dgv.CellBorderStyle              = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 254);
        dgv.ReadOnly                     = true;
        dgv.AllowUserToAddRows           = false;
        dgv.AllowUserToDeleteRows        = false;
    }

    // ─── EASING HELPERS ──────────────────────────────────────────
    /// <summary>Sine-based ease-in-out for smooth animations (0→1)</summary>
    public static float EaseInOut(float t)
        => (float)(0.5 - 0.5 * Math.Cos(Math.PI * t));
}

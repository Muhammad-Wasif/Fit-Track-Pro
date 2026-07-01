using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using ReaLTaiizor.Controls;

namespace FitTrack.GUI.Controls;

public class BufferedPanel : System.Windows.Forms.Panel
{
    public BufferedPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
    }
}

public class RoundedPanel : System.Windows.Forms.Panel
{
    public int CornerRadius { get; set; } = 16;
    public bool ShowShadow { get; set; } = true;
    public bool ShowBorder { get; set; } = false;
    public Color ThemeColor { get; set; } = UITheme.BgCard;
    public Color FillColor { get => ThemeColor; set => ThemeColor = value; }

    public RoundedPanel()
    {
        BackColor = Color.Transparent;
        Padding = new Padding(16);
    }
    
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = new GraphicsPath();
        int r = CornerRadius;
        if(r > 0) {
            path.AddArc(0, 0, r, r, 180, 90);
            path.AddArc(Width - r, 0, r, r, 270, 90);
            path.AddArc(Width - r, Height - r, r, r, 0, 90);
            path.AddArc(0, Height - r, r, r, 90, 90);
            path.CloseFigure();
        } else {
            path.AddRectangle(new Rectangle(0,0,Width,Height));
        }
        using var brush = new SolidBrush(ThemeColor);
        e.Graphics.FillPath(brush, path);
    }
}

public class GoldButton : HopeButton
{
    public enum ButtonStyle { Gold, Red, Ghost, White }
    private ButtonStyle _style = ButtonStyle.Gold;
    
    public ButtonStyle Style 
    { 
        get => _style; 
        set { _style = value; ApplyStyle(); } 
    }

    public GoldButton()
    {
        Font = UITheme.FontSemiBold(9.5f);
        Cursor = Cursors.Hand;
        Height = 44;
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        switch (Style)
        {
            case ButtonStyle.Gold:
                PrimaryColor = UITheme.GoldDark;
                HoverTextColor = Color.White;
                TextColor = Color.White;
                break;
            case ButtonStyle.Red:
                PrimaryColor = UITheme.Red;
                HoverTextColor = Color.White;
                TextColor = Color.White;
                break;
            case ButtonStyle.Ghost:
                PrimaryColor = Color.Transparent;
                TextColor = UITheme.Gold;
                break;
            case ButtonStyle.White:
                PrimaryColor = Color.White;
                TextColor = UITheme.TextPrimary;
                break;
        }
    }
}

public class MetricCard : System.Windows.Forms.Panel
{
    private string _metric = "-";
    private string _label = "Label";
    private Color _accentColor = UITheme.Gold;

    public string Metric { get => _metric; set { _metric = value; Invalidate(); } }
    public string Label  { get => _label;  set { _label = value;  Invalidate(); } }
    public string Unit   { get; set; } = "";
    public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }
    public Color ThemeColor { get; set; } = Color.White;

    public MetricCard()
    {
        Size = new Size(180, 100);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        
        using var path = new GraphicsPath();
        int r = UITheme.CardRadius;
        path.AddArc(0, 0, r, r, 180, 90);
        path.AddArc(Width - r, 0, r, r, 270, 90);
        path.AddArc(Width - r, Height - r, r, r, 0, 90);
        path.AddArc(0, Height - r, r, r, 90, 90);
        path.CloseFigure();
        using var bgBrush = new SolidBrush(ThemeColor);
        g.FillPath(bgBrush, path);

        using var acBr = new SolidBrush(_accentColor);
        g.FillRectangle(acBr, 4, 2, Width - 8, 4);

        string display = string.IsNullOrEmpty(Unit) ? _metric : $"{_metric} {Unit}";
        float fontSize = display.Length > 16 ? 10f : display.Length > 12 ? 11.5f : display.Length > 8 ? 13f : display.Length > 5 ? 15f : 18f;
        using var metricFont = UITheme.FontHeading(fontSize);
        using var metricBr = new SolidBrush(UITheme.TextPrimary);
        var metricRect = new RectangleF(6, 10, Width - 12, Height - 34);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord };
        g.DrawString(display, metricFont, metricBr, metricRect, sf);

        using var lblFont = UITheme.FontSmall(8.5f);
        using var lblBr = new SolidBrush(UITheme.TextSecondary);
        var lblRect = new RectangleF(4, Height - 26, Width - 8, 22);
        g.DrawString(_label, lblFont, lblBr, lblRect, sf);
    }
    public void SetValue(string value) => Metric = value;
}

public class StyledTextBox : HopeTextBox
{
    private bool _isPassword;
    public bool IsPassword
    {
        get => _isPassword;
        set
        {
            _isPassword = value;
            UseSystemPasswordChar = value;
        }
    }
    public StyledTextBox Inner => this;
    public string PlaceholderText { get; set; } = "";
    public Color BorderColor { get; set; } = Color.Gray;

    public StyledTextBox()
    {
        Height = 44;
        Font = UITheme.FontBody(10f);
        ForeColor = UITheme.TextPrimary;
        BaseColor = Color.FromArgb(252, 252, 255);
        BorderColor = UITheme.BorderLight;
    }
}

public class ProgressRingControl : UserControl
{
    private float _progress = 0f;
    private float _displayProgress = 0f;
    public float Progress
    {
        get => _progress;
        set { _progress = Math.Clamp(value, 0f, 1f); AnimateToTarget(); }
    }
    public string CenterText { get; set; } = "";
    public string SubText { get; set; } = "Daily Goal";
    public Color RingColor { get; set; } = UITheme.Gold;
    public Color TrackColor { get; set; } = Color.FromArgb(235, 235, 245);
    public int RingThickness { get; set; } = 10;
    private System.Windows.Forms.Timer? _animTimer;

    public ProgressRingControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(140, 140);
        BackColor = Color.Transparent;
    }
    private void AnimateToTarget()
    {
        _animTimer?.Stop();
        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (_, _) =>
        {
            float diff = _progress - _displayProgress;
            if (Math.Abs(diff) < 0.005f) { _displayProgress = _progress; _animTimer.Stop(); }
            else { _displayProgress += diff * 0.08f; }
            Invalidate();
        };
        _animTimer.Start();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        int pad = RingThickness + 4;
        Rectangle ring = new(pad, pad, Width - pad * 2, Height - pad * 2);
        using var trackPen = new Pen(TrackColor, RingThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(trackPen, ring, 0, 360);
        if (_displayProgress > 0.001f)
        {
            float sweep = _displayProgress * 360f;
            using var ringPen = new Pen(RingColor, RingThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(ringPen, ring, -90, sweep);
        }
        string ct = string.IsNullOrEmpty(CenterText) ? $"{(int)(_displayProgress * 100)}%" : CenterText;
        using var f1 = UITheme.FontHeading(14f);
        using var f2 = UITheme.FontSmall(7.5f);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var tb = new SolidBrush(UITheme.TextPrimary);
        using var sb = new SolidBrush(UITheme.TextSecondary);
        g.DrawString(ct, f1, tb, new RectangleF(ring.X, ring.Y - 8, ring.Width, ring.Height), sf);
        g.DrawString(SubText, f2, sb, new RectangleF(ring.X, ring.Y + 14, ring.Width, ring.Height), sf);
    }
}

public class SidebarNavButton : HopeButton
{
    private string _emoji = "⚡";
    public string Emoji { get => _emoji; set { _emoji = value; UpdateText(); } }
    private string _navText = "Menu";
    public string NavText { get => _navText; set { _navText = value; UpdateText(); } }
    private bool _isActive;
    public bool IsActive { get => _isActive; set { _isActive = value; UpdateStyle(); } }
    private bool _isExpanded = true;
    public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; UpdateText(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        string t = Text;
        Text = string.Empty;
        base.OnPaint(e);
        Text = t;

        Color c = _isActive ? UITheme.Gold : Color.FromArgb(180, 185, 205);
        TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
        
        Rectangle r = new Rectangle(0, 0, Width, Height);
        if (_isExpanded)
        {
            r.X += 20;
            r.Width -= 20;
        }
        else
        {
            flags = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;
        }

        TextRenderer.DrawText(e.Graphics, t, Font, r, c, flags);
    }

    public SidebarNavButton()
    {
        Height = 48;
        Cursor = Cursors.Hand;
        UpdateStyle();
    }
    private void UpdateText()
    {
        if (_isExpanded && Width > UITheme.SidebarCollapsed + 20) Text = $"{_emoji}   {_navText}";
        else Text = $"{_emoji}";
    }
    private void UpdateStyle()
    {
        if (_isActive)
        {
            PrimaryColor = Color.FromArgb(25, UITheme.Gold);
            TextColor = UITheme.Gold;
            Font = UITheme.FontSemiBold(10f);
        }
        else
        {
            PrimaryColor = Color.Transparent;
            TextColor = Color.FromArgb(180, 185, 205);
            Font = UITheme.FontBody(10f);
        }
    }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateText();
    }
}

public class LineChartControl : UserControl
{
    public string ChartTitle { get; set; } = "Progress";
    public string YAxisLabel { get; set; } = "kg";
    private List<(DateTime date, double value)> _data = new();

    public LineChartControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
        Size = new Size(500, 220);
    }

    public void SetData(IEnumerable<(DateTime date, double value)> data)
    {
        _data = data.OrderBy(d => d.date).ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        Graphics g = e.Graphics;
        
        int pad = 50, padR = 20, padB = 40, padT = 30;
        Rectangle plot = new(pad, padT, Width - pad - padR, Height - padT - padB);
        g.Clear(Color.White);
        using var titleFont = UITheme.FontSemiBold(10f);
        using var titleBrush = new SolidBrush(UITheme.TextPrimary);
        g.DrawString(ChartTitle, titleFont, titleBrush, new PointF(pad, 6));

        if (_data.Count < 2) return;
        double minV = _data.Min(d => d.value) * 0.97;
        double maxV = _data.Max(d => d.value) * 1.03;
        double rangeV = maxV - minV;
        using var gridPen = new Pen(Color.FromArgb(230, 230, 238), 1) { DashStyle = DashStyle.Dot };
        int gridLines = 5;
        for (int i = 0; i <= gridLines; i++)
        {
            int y = plot.Bottom - (int)(plot.Height * i / (double)gridLines);
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            double val = minV + rangeV * i / gridLines;
            using var axFont = UITheme.FontSmall(7.5f);
            using var axBrush = new SolidBrush(UITheme.TextMuted);
            g.DrawString(val.ToString("F1"), axFont, axBrush, new PointF(2, y - 7));
        }

        PointF[] points = _data.Select((d, i) => new PointF(plot.Left + (float)(i * plot.Width / (double)(_data.Count - 1)), plot.Bottom - (float)((d.value - minV) / rangeV * plot.Height))).ToArray();
        using var gradPath = new GraphicsPath();
        gradPath.AddLines(points);
        gradPath.AddLine(points.Last(), new PointF(points.Last().X, plot.Bottom));
        gradPath.AddLine(new PointF(points.Last().X, plot.Bottom), new PointF(points.First().X, plot.Bottom));
        gradPath.CloseFigure();
        using var gradBrush = new LinearGradientBrush(new PointF(0, plot.Top), new PointF(0, plot.Bottom), Color.FromArgb(50, UITheme.Gold), Color.FromArgb(0, UITheme.Gold));
        g.FillPath(gradBrush, gradPath);

        using var linePen = new Pen(UITheme.Gold, 2.5f) { LineJoin = LineJoin.Round };
        g.DrawLines(linePen, points);

        foreach (var p in points)
        {
            g.FillEllipse(Brushes.White, p.X - 5, p.Y - 5, 10, 10);
            using var dot = new SolidBrush(UITheme.Gold);
            g.FillEllipse(dot, p.X - 4, p.Y - 4, 8, 8);
            g.FillEllipse(Brushes.White, p.X - 2, p.Y - 2, 4, 4);
        }

        int labelStep = Math.Max(1, _data.Count / 6);
        for (int i = 0; i < _data.Count; i += labelStep)
        {
            float x = plot.Left + (float)(i * plot.Width / (double)(_data.Count - 1));
            using var axFont = UITheme.FontSmall(7.5f);
            using var axBrush = new SolidBrush(UITheme.TextMuted);
            var sf = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(_data[i].date.ToString("MM/dd"), axFont, axBrush, new RectangleF(x - 20, plot.Bottom + 4, 40, 16), sf);
        }
        g.TranslateTransform(10, plot.Top + plot.Height / 2);
        g.RotateTransform(-90);
        using var yFont = UITheme.FontSmall(8f);
        using var yBrush = new SolidBrush(UITheme.TextSecondary);
        g.DrawString(YAxisLabel, yFont, yBrush, PointF.Empty);
        g.ResetTransform();
    }
}

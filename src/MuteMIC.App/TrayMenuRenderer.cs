using System.Drawing.Drawing2D;

namespace MuteMIC.App;

internal sealed class ThemedContextMenuStrip : ContextMenuStrip
{
    public bool IsLightTheme { get; private set; }

    public ThemedContextMenuStrip()
    {
        ShowImageMargin = false;
        ShowCheckMargin = false;
        Padding = new Padding(6);
        DropShadowEnabled = true;
        ApplyTheme(lightTheme: false);
    }

    public void ApplyTheme(bool lightTheme)
    {
        IsLightTheme = lightTheme;
        Renderer = new TrayMenuRenderer(lightTheme);
        BackColor = TrayMenuRenderer.MenuBackColor(lightTheme);
        ForeColor = TrayMenuRenderer.MenuForeColor(lightTheme);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        using GraphicsPath path = RoundedRect(new Rectangle(Point.Empty, Size), 8);
        Region?.Dispose();
        Region = new Region(path);
    }

    protected override void OnClosed(ToolStripDropDownClosedEventArgs e)
    {
        Region?.Dispose();
        Region = null;
        base.OnClosed(e);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        Rectangle arc = new(bounds.Location, new Size(diameter, diameter));
        GraphicsPath path = new();

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter - 1;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter - 1;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }
}

internal sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
{
    private readonly bool _lightTheme;

    public TrayMenuRenderer(bool lightTheme)
        : base(new TrayMenuColorTable(lightTheme))
    {
        _lightTheme = lightTheme;
        RoundedEdges = true;
    }

    public static Color MenuBackColor(bool lightTheme)
    {
        return lightTheme ? Color.FromArgb(249, 249, 249) : Color.FromArgb(32, 32, 32);
    }

    public static Color MenuForeColor(bool lightTheme)
    {
        return lightTheme ? Color.FromArgb(24, 24, 24) : Color.White;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using SolidBrush brush = new(MenuBackColor(_lightTheme));
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        Color border = _lightTheme ? Color.FromArgb(214, 214, 214) : Color.FromArgb(68, 68, 68);
        using Pen pen = new(border);
        Rectangle rect = new(Point.Empty, new Size(e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
        e.Graphics.DrawRectangle(pen, rect);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected)
        {
            base.OnRenderMenuItemBackground(e);
            return;
        }

        Color selected = _lightTheme ? Color.FromArgb(232, 232, 232) : Color.FromArgb(52, 52, 52);
        using SolidBrush brush = new(selected);
        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        Color separator = _lightTheme ? Color.FromArgb(218, 218, 218) : Color.FromArgb(74, 74, 74);
        using Pen pen = new(separator);
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
    }

    private sealed class TrayMenuColorTable : ProfessionalColorTable
    {
        private readonly bool _lightTheme;

        public TrayMenuColorTable(bool lightTheme)
        {
            _lightTheme = lightTheme;
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground => MenuBackColor(_lightTheme);
        public override Color ImageMarginGradientBegin => MenuBackColor(_lightTheme);
        public override Color ImageMarginGradientMiddle => MenuBackColor(_lightTheme);
        public override Color ImageMarginGradientEnd => MenuBackColor(_lightTheme);
        public override Color MenuBorder => _lightTheme ? Color.FromArgb(214, 214, 214) : Color.FromArgb(68, 68, 68);
        public override Color MenuItemSelected => _lightTheme ? Color.FromArgb(232, 232, 232) : Color.FromArgb(52, 52, 52);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color SeparatorDark => _lightTheme ? Color.FromArgb(218, 218, 218) : Color.FromArgb(74, 74, 74);
        public override Color SeparatorLight => SeparatorDark;
    }
}

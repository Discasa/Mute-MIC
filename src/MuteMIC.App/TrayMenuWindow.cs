using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MuteMIC.App;

internal sealed class TrayMenuWindow : Form
{
    private const int ShadowMargin = 18;
    private const int MenuWidth = 232;
    private const int SubMenuWidth = 184;
    private const int MenuGap = 6;
    private const int ItemHeight = 34;
    private const int SeparatorHeight = 9;
    private const int MenuPadding = 8;
    private const int MenuCornerRadius = 8;
    private const int ItemCornerRadius = 4;

    private readonly bool _lightTheme;
    private readonly TrayMenuText _text;
    private readonly string _currentLanguage;
    private readonly Action _showHotkeys;
    private readonly Action<string> _setLanguage;
    private readonly Action _exit;
    private readonly Color _menuBackColor;
    private readonly Color _menuBorderColor;
    private readonly Color _hoverColor;
    private readonly Color _separatorColor;
    private readonly Color _textColor;
    private readonly Color _mutedTextColor;
    private Rectangle _hotkeysRect;
    private Rectangle _languageRect;
    private Rectangle _exitRect;
    private Rectangle _englishRect;
    private Rectangle _portugueseRect;
    private string? _hoveredItem;
    private bool _submenuVisible;
    private bool _closingByAction;

    private static int MainMenuHeight => (MenuPadding * 2) + (ItemHeight * 3) + SeparatorHeight;
    private static int SubMenuHeight => (MenuPadding * 2) + (ItemHeight * 2);
    private static int CollapsedWindowWidth => MenuWidth + (ShadowMargin * 2);
    private static int ExpandedWindowWidth => MenuWidth + MenuGap + SubMenuWidth + (ShadowMargin * 2);
    private static int WindowHeight => Math.Max(MainMenuHeight, SubMenuHeight + ItemHeight) + (ShadowMargin * 2);

    public TrayMenuWindow(
        bool lightTheme,
        TrayMenuText text,
        string currentLanguage,
        Point anchor,
        Action showHotkeys,
        Action<string> setLanguage,
        Action exit)
    {
        _lightTheme = lightTheme;
        _text = text;
        _currentLanguage = currentLanguage;
        _showHotkeys = showHotkeys;
        _setLanguage = setLanguage;
        _exit = exit;

        _menuBackColor = lightTheme ? Color.FromArgb(249, 249, 249) : Color.FromArgb(31, 31, 31);
        _menuBorderColor = lightTheme ? Color.FromArgb(222, 222, 222) : Color.FromArgb(42, 42, 42);
        _hoverColor = lightTheme ? Color.FromArgb(238, 238, 238) : Color.FromArgb(47, 47, 47);
        _separatorColor = lightTheme ? Color.FromArgb(224, 224, 224) : Color.FromArgb(72, 72, 72);
        _textColor = lightTheme ? Color.FromArgb(24, 24, 24) : Color.White;
        _mutedTextColor = lightTheme ? Color.FromArgb(80, 80, 80) : Color.FromArgb(210, 210, 210);

        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        ClientSize = new Size(CollapsedWindowWidth, WindowHeight);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        PositionNear(anchor);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RenderLayeredWindow();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (!_closingByAction)
        {
            Close();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        string? hovered = HitTest(e.Location);
        bool showSubmenu = _languageRect.Contains(e.Location)
            || (_submenuVisible && (IsInSubmenu(e.Location) || IsBetweenLanguageAndSubmenu(e.Location)));

        if (hovered != _hoveredItem || showSubmenu != _submenuVisible)
        {
            _hoveredItem = hovered;
            SetSubmenuVisible(showSubmenu);
            RenderLayeredWindow();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoveredItem is not null || _submenuVisible)
        {
            _hoveredItem = null;
            SetSubmenuVisible(false);
            RenderLayeredWindow();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        switch (HitTest(e.Location))
        {
            case "hotkeys":
                CloseThen(_showHotkeys);
                break;
            case "exit":
                CloseThen(_exit);
                break;
            case "english":
                CloseThen(() => _setLanguage("en"));
                break;
            case "portuguese":
                CloseThen(() => _setLanguage("pt-BR"));
                break;
        }
    }

    private void CloseThen(Action action)
    {
        _closingByAction = true;
        Close();
        action();
    }

    private void SetSubmenuVisible(bool visible)
    {
        if (_submenuVisible == visible)
        {
            return;
        }

        _submenuVisible = visible;
        ClientSize = new Size(visible ? ExpandedWindowWidth : CollapsedWindowWidth, WindowHeight);
    }

    private string? HitTest(Point point)
    {
        if (_hotkeysRect.Contains(point))
        {
            return "hotkeys";
        }

        if (_languageRect.Contains(point))
        {
            return "language";
        }

        if (_exitRect.Contains(point))
        {
            return "exit";
        }

        if (_submenuVisible && _englishRect.Contains(point))
        {
            return "english";
        }

        if (_submenuVisible && _portugueseRect.Contains(point))
        {
            return "portuguese";
        }

        return null;
    }

    private bool IsInSubmenu(Point point)
    {
        Rectangle submenu = new(ShadowMargin + MenuWidth + MenuGap, ShadowMargin + MenuPadding + ItemHeight, SubMenuWidth, SubMenuHeight);
        return submenu.Contains(point);
    }

    private bool IsBetweenLanguageAndSubmenu(Point point)
    {
        Rectangle bridge = new(_languageRect.Right, _languageRect.Top, MenuGap + 4, _languageRect.Height);
        return bridge.Contains(point);
    }

    private void RenderLayeredWindow()
    {
        using Bitmap bitmap = new(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawMainMenu(graphics);
            if (_submenuVisible)
            {
                DrawLanguageSubmenu(graphics);
            }
        }

        ApplyBitmap(bitmap);
    }

    private void DrawMainMenu(Graphics graphics)
    {
        Rectangle panel = new(ShadowMargin, ShadowMargin, MenuWidth, MainMenuHeight);
        DrawPanel(graphics, panel);

        int x = panel.Left + 7;
        int y = panel.Top + MenuPadding;
        int width = MenuWidth - 14;

        _hotkeysRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _hotkeysRect, _text.Hotkeys, isHovered: _hoveredItem == "hotkeys");
        y += ItemHeight;

        _languageRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _languageRect, _text.Language, isHovered: _hoveredItem == "language" || _submenuVisible, hasSubmenu: true);
        y += ItemHeight;

        DrawSeparator(graphics, new Rectangle(x + 10, y + 4, width - 20, 1));
        y += SeparatorHeight;

        _exitRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _exitRect, _text.Exit, isHovered: _hoveredItem == "exit");
    }

    private void DrawLanguageSubmenu(Graphics graphics)
    {
        Rectangle panel = new(ShadowMargin + MenuWidth + MenuGap, ShadowMargin + MenuPadding + ItemHeight, SubMenuWidth, SubMenuHeight);
        DrawPanel(graphics, panel);

        int x = panel.Left + 7;
        int y = panel.Top + MenuPadding;
        int width = SubMenuWidth - 14;

        _englishRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _englishRect, _text.English, isHovered: _hoveredItem == "english", isChecked: _currentLanguage == "en");
        y += ItemHeight;

        _portugueseRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _portugueseRect, _text.Portuguese, isHovered: _hoveredItem == "portuguese", isChecked: _currentLanguage == "pt-BR");
    }

    private void DrawPanel(Graphics graphics, Rectangle rect)
    {
        for (int i = 8; i >= 1; i--)
        {
            int alpha = 5 + (i * 3);
            Rectangle shadowRect = Rectangle.Inflate(rect, i, i);
            shadowRect.Offset(0, 2);
            using GraphicsPath shadowPath = RoundedRect(shadowRect, MenuCornerRadius + i);
            using SolidBrush shadowBrush = new(Color.FromArgb(alpha, Color.Black));
            graphics.FillPath(shadowBrush, shadowPath);
        }

        using GraphicsPath path = RoundedRect(rect, MenuCornerRadius);
        using SolidBrush backBrush = new(_menuBackColor);
        using Pen borderPen = new(_menuBorderColor);
        graphics.FillPath(backBrush, path);
        graphics.DrawPath(borderPen, path);
    }

    private void DrawItem(Graphics graphics, Rectangle rect, string text, bool isHovered, bool hasSubmenu = false, bool isChecked = false)
    {
        if (isHovered)
        {
            Rectangle hoverRect = Rectangle.Inflate(rect, -2, -3);
            using GraphicsPath hoverPath = RoundedRect(hoverRect, ItemCornerRadius);
            using SolidBrush hoverBrush = new(_hoverColor);
            graphics.FillPath(hoverBrush, hoverPath);
        }

        if (isChecked)
        {
            using Pen checkPen = new(_mutedTextColor, 1.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            PointF[] points =
            [
                new(rect.Left + 11, rect.Top + 18),
                new(rect.Left + 16, rect.Top + 23),
                new(rect.Left + 27, rect.Top + 12)
            ];
            graphics.DrawLines(checkPen, points);
        }

        RectangleF textRect = new(rect.Left + 30, rect.Top, rect.Width - 48, rect.Height);
        using Font font = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        using SolidBrush textBrush = new(_textColor);
        using StringFormat format = new()
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        graphics.DrawString(text, font, textBrush, textRect, format);

        if (hasSubmenu)
        {
            using Pen arrowPen = new(_textColor, 1.4f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            float centerY = rect.Top + (rect.Height / 2f);
            PointF[] arrow =
            [
                new(rect.Right - 18, centerY - 4),
                new(rect.Right - 14, centerY),
                new(rect.Right - 18, centerY + 4)
            ];
            graphics.DrawLines(arrowPen, arrow);
        }
    }

    private void DrawSeparator(Graphics graphics, Rectangle rect)
    {
        using SolidBrush brush = new(_separatorColor);
        graphics.FillRectangle(brush, rect);
    }

    private void PositionNear(Point anchor)
    {
        Screen screen = Screen.FromPoint(anchor);
        Rectangle workArea = screen.WorkingArea;
        int left = anchor.X - MenuWidth + 16 - ShadowMargin;
        int top = anchor.Y - MainMenuHeight - ShadowMargin - 4;

        left = Math.Max(workArea.Left, Math.Min(left, workArea.Right - CollapsedWindowWidth));
        top = Math.Max(workArea.Top, Math.Min(top, workArea.Bottom - WindowHeight));

        Location = new Point(left, top);
    }

    private void ApplyBitmap(Bitmap bitmap)
    {
        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memoryDc = CreateCompatibleDC(screenDc);
        IntPtr bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
        IntPtr oldBitmap = SelectObject(memoryDc, bitmapHandle);

        try
        {
            Size size = new(bitmap.Width, bitmap.Height);
            Point source = Point.Empty;
            Point topPosition = new(Left, Top);
            BlendFunction blend = new()
            {
                BlendOp = 0,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = 1
            };

            UpdateLayeredWindow(Handle, screenDc, ref topPosition, ref size, memoryDc, ref source, 0, ref blend, 2);
        }
        finally
        {
            SelectObject(memoryDc, oldBitmap);
            DeleteObject(bitmapHandle);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        GraphicsPath path = new();
        Rectangle arc = new(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(
        IntPtr hwnd,
        IntPtr hdcDst,
        ref Point pptDst,
        ref Size psize,
        IntPtr hdcSrc,
        ref Point pptSrc,
        int crKey,
        ref BlendFunction pblend,
        int dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);
}

internal readonly record struct TrayMenuText(
    string Hotkeys,
    string Language,
    string English,
    string Portuguese,
    string Exit);

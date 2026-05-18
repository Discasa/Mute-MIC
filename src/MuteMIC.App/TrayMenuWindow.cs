using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MuteMIC.App;

internal sealed class TrayMenuWindow : Form
{
    private const int ShadowMargin = 10;
    private const int MenuWidth = 232;
    private const int SubMenuWidth = 184;
    private const int MenuGap = 0;
    private const int ItemHeight = 34;
    private const int SeparatorHeight = 9;
    private const int MenuPadding = 8;
    private const int MenuCornerRadius = 8;
    private const int ItemCornerRadius = 4;

    private readonly bool _lightTheme;
    private readonly TrayMenuText _text;
    private readonly IconColorScheme _currentColorScheme;
    private readonly string _currentLanguage;
    private readonly Action _showHotkeys;
    private readonly Action<IconColorScheme> _setColorScheme;
    private readonly Action<string> _setLanguage;
    private readonly Action _exit;
    private readonly Color _menuBackColor;
    private readonly Color _menuBorderColor;
    private readonly Color _hoverColor;
    private readonly Color _separatorColor;
    private readonly Color _textColor;
    private readonly Color _mutedTextColor;
    private readonly System.Windows.Forms.Timer _outsideClickTimer = new() { Interval = 30 };
    private Rectangle _mainPanelRect;
    private Rectangle _submenuPanelRect;
    private Rectangle _hotkeysRect;
    private Rectangle _colorSchemeRect;
    private Rectangle _languageRect;
    private Rectangle _exitRect;
    private Rectangle _monochromeRect;
    private Rectangle _colorfulRect;
    private Rectangle _englishRect;
    private Rectangle _portugueseRect;
    private string? _hoveredItem;
    private ActiveSubmenu _activeSubmenu = ActiveSubmenu.None;
    private bool _closingByAction;
    private bool _isClosing;

    private static int MainMenuHeight => (MenuPadding * 2) + (ItemHeight * 4) + SeparatorHeight;
    private static int SubMenuHeight => (MenuPadding * 2) + (ItemHeight * 2);
    private static int CollapsedWindowWidth => MenuWidth + (ShadowMargin * 2);
    private static int ExpandedWindowWidth => MenuWidth + MenuGap + SubMenuWidth + (ShadowMargin * 2);
    private static int WindowHeight => MainMenuHeight + (ShadowMargin * 2);

    public TrayMenuWindow(
        bool lightTheme,
        TrayMenuText text,
        IconColorScheme currentColorScheme,
        string currentLanguage,
        Point anchor,
        Action showHotkeys,
        Action<IconColorScheme> setColorScheme,
        Action<string> setLanguage,
        Action exit)
    {
        _lightTheme = lightTheme;
        _text = text;
        _currentColorScheme = currentColorScheme;
        _currentLanguage = currentLanguage;
        _showHotkeys = showHotkeys;
        _setColorScheme = setColorScheme;
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
        KeyPreview = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        _outsideClickTimer.Tick += OutsideClickTimer_Tick;
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
        Activate();
        Capture = true;
        _outsideClickTimer.Start();
        RenderLayeredWindow();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _isClosing = true;
        _outsideClickTimer.Stop();
        Capture = false;
        base.OnFormClosing(e);
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
        ActiveSubmenu submenu = GetActiveSubmenuForPoint(e.Location);

        if (hovered != _hoveredItem || submenu != _activeSubmenu)
        {
            _hoveredItem = hovered;
            SetActiveSubmenu(submenu);
            RenderLayeredWindow();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoveredItem is not null)
        {
            _hoveredItem = null;
            RenderLayeredWindow();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!IsInOpenMenuSurface(e.Location))
        {
            Close();
            return;
        }

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
            case "monochrome":
                CloseThen(() => _setColorScheme(IconColorScheme.Monochrome));
                break;
            case "colorful":
                CloseThen(() => _setColorScheme(IconColorScheme.Colorful));
                break;
            case "english":
                CloseThen(() => _setLanguage("en"));
                break;
            case "portuguese":
                CloseThen(() => _setLanguage("pt-BR"));
                break;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_CAPTURECHANGED = 0x0215;
        if (m.Msg == WM_CAPTURECHANGED && !_closingByAction && !_isClosing && !IsDisposed)
        {
            BeginInvoke(new Action(Close));
        }

        base.WndProc(ref m);
    }

    private void OutsideClickTimer_Tick(object? sender, EventArgs e)
    {
        if (_isClosing || Control.MouseButtons == MouseButtons.None)
        {
            return;
        }

        Point clientPoint = PointToClient(Cursor.Position);
        if (!IsInOpenMenuSurface(clientPoint))
        {
            Close();
        }
    }

    private void CloseThen(Action action)
    {
        _closingByAction = true;
        Close();
        action();
    }

    private void SetActiveSubmenu(ActiveSubmenu submenu)
    {
        if (_activeSubmenu == submenu)
        {
            return;
        }

        _activeSubmenu = submenu;
        ClientSize = new Size(submenu == ActiveSubmenu.None ? CollapsedWindowWidth : ExpandedWindowWidth, WindowHeight);
    }

    private ActiveSubmenu GetActiveSubmenuForPoint(Point point)
    {
        if (_colorSchemeRect.Contains(point))
        {
            return ActiveSubmenu.ColorScheme;
        }

        if (_languageRect.Contains(point))
        {
            return ActiveSubmenu.Language;
        }

        if (_activeSubmenu == ActiveSubmenu.ColorScheme && (IsInSubmenu(point, ActiveSubmenu.ColorScheme) || IsBetweenItemAndSubmenu(point, _colorSchemeRect)))
        {
            return ActiveSubmenu.ColorScheme;
        }

        if (_activeSubmenu == ActiveSubmenu.Language && (IsInSubmenu(point, ActiveSubmenu.Language) || IsBetweenItemAndSubmenu(point, _languageRect)))
        {
            return ActiveSubmenu.Language;
        }

        return ActiveSubmenu.None;
    }

    private string? HitTest(Point point)
    {
        if (_hotkeysRect.Contains(point))
        {
            return "hotkeys";
        }

        if (_colorSchemeRect.Contains(point))
        {
            return "colorScheme";
        }

        if (_languageRect.Contains(point))
        {
            return "language";
        }

        if (_exitRect.Contains(point))
        {
            return "exit";
        }

        if (_activeSubmenu == ActiveSubmenu.ColorScheme)
        {
            if (_monochromeRect.Contains(point))
            {
                return "monochrome";
            }

            if (_colorfulRect.Contains(point))
            {
                return "colorful";
            }
        }

        if (_activeSubmenu == ActiveSubmenu.Language)
        {
            if (_englishRect.Contains(point))
            {
                return "english";
            }

            if (_portugueseRect.Contains(point))
            {
                return "portuguese";
            }
        }

        return null;
    }

    private bool IsInOpenMenuSurface(Point point)
    {
        return _mainPanelRect.Contains(point) || (_activeSubmenu != ActiveSubmenu.None && _submenuPanelRect.Contains(point));
    }

    private bool IsInSubmenu(Point point, ActiveSubmenu submenu)
    {
        Rectangle submenuRect = GetSubmenuPanelRect(submenu);
        return submenuRect.Contains(point);
    }

    private bool IsBetweenItemAndSubmenu(Point point, Rectangle itemRect)
    {
        Rectangle bridge = new(itemRect.Right - 2, itemRect.Top - 2, MenuGap + 14, itemRect.Height + 4);
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
            if (_activeSubmenu != ActiveSubmenu.None)
            {
                DrawSubmenu(graphics, _activeSubmenu);
            }
        }

        ApplyBitmap(bitmap);
    }

    private void DrawMainMenu(Graphics graphics)
    {
        _mainPanelRect = new Rectangle(ShadowMargin, ShadowMargin, MenuWidth, MainMenuHeight);
        DrawPanel(graphics, _mainPanelRect);

        int x = _mainPanelRect.Left + 7;
        int y = _mainPanelRect.Top + MenuPadding;
        int width = MenuWidth - 14;

        _hotkeysRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _hotkeysRect, _text.Hotkeys, isHovered: _hoveredItem == "hotkeys");
        y += ItemHeight;

        _colorSchemeRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _colorSchemeRect, _text.ColorScheme, isHovered: _hoveredItem == "colorScheme" || _activeSubmenu == ActiveSubmenu.ColorScheme, hasSubmenu: true);
        y += ItemHeight;

        _languageRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _languageRect, _text.Language, isHovered: _hoveredItem == "language" || _activeSubmenu == ActiveSubmenu.Language, hasSubmenu: true);
        y += ItemHeight;

        DrawSeparator(graphics, new Rectangle(x + 10, y + 4, width - 20, 1));
        y += SeparatorHeight;

        _exitRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _exitRect, _text.Exit, isHovered: _hoveredItem == "exit");
    }

    private void DrawSubmenu(Graphics graphics, ActiveSubmenu submenu)
    {
        _submenuPanelRect = GetSubmenuPanelRect(submenu);
        DrawPanel(graphics, _submenuPanelRect);

        int x = _submenuPanelRect.Left + 7;
        int y = _submenuPanelRect.Top + MenuPadding;
        int width = SubMenuWidth - 14;

        if (submenu == ActiveSubmenu.ColorScheme)
        {
            _monochromeRect = new Rectangle(x, y, width, ItemHeight);
            DrawItem(graphics, _monochromeRect, _text.Monochrome, isHovered: _hoveredItem == "monochrome", isChecked: _currentColorScheme == IconColorScheme.Monochrome);
            y += ItemHeight;

            _colorfulRect = new Rectangle(x, y, width, ItemHeight);
            DrawItem(graphics, _colorfulRect, _text.Colorful, isHovered: _hoveredItem == "colorful", isChecked: _currentColorScheme == IconColorScheme.Colorful);
            return;
        }

        _englishRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _englishRect, _text.English, isHovered: _hoveredItem == "english", isChecked: _currentLanguage == "en");
        y += ItemHeight;

        _portugueseRect = new Rectangle(x, y, width, ItemHeight);
        DrawItem(graphics, _portugueseRect, _text.Portuguese, isHovered: _hoveredItem == "portuguese", isChecked: _currentLanguage == "pt-BR");
    }

    private Rectangle GetSubmenuPanelRect(ActiveSubmenu submenu)
    {
        Rectangle parent = submenu == ActiveSubmenu.ColorScheme ? _colorSchemeRect : _languageRect;
        return new Rectangle(ShadowMargin + MenuWidth + MenuGap, parent.Top, SubMenuWidth, SubMenuHeight);
    }

    private void DrawPanel(Graphics graphics, Rectangle rect)
    {
        for (int i = 5; i >= 1; i--)
        {
            int alpha = 3 + (i * 3);
            Rectangle shadowRect = Rectangle.Inflate(rect, i, i);
            shadowRect.Offset(0, 1);
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

        left = Math.Max(workArea.Left, Math.Min(left, workArea.Right - ExpandedWindowWidth));
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

    private enum ActiveSubmenu
    {
        None,
        ColorScheme,
        Language
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
    string ColorScheme,
    string Monochrome,
    string Colorful,
    string Language,
    string English,
    string Portuguese,
    string Exit);

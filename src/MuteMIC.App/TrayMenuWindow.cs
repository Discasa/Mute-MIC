using Forms = System.Windows.Forms;
using System.Threading.Tasks;
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;
using WpfEffects = System.Windows.Media.Effects;
using WpfInput = System.Windows.Input;
using WpfMedia = System.Windows.Media;
using WpfShapes = System.Windows.Shapes;

namespace MuteMIC.App;

internal sealed class TrayMenuWindow : Wpf.Window
{
    private const double ShadowMargin = 18;
    private const double MenuWidth = 232;
    private const double SubMenuWidth = 184;
    private const double MenuGap = 6;
    private const double ItemHeight = 34;
    private const double SeparatorHeight = 9;
    private const double MenuPadding = 8;
    private const double MenuCornerRadius = 8;
    private const double ItemCornerRadius = 4;

    private readonly TrayMenuText _text;
    private readonly Action _showHotkeys;
    private readonly Action<string> _setLanguage;
    private readonly Action _exit;
    private readonly WpfControls.Canvas _root = new();
    private readonly WpfMedia.Brush _menuBackBrush;
    private readonly WpfMedia.Brush _menuBorderBrush;
    private readonly WpfMedia.Brush _hoverBrush;
    private readonly WpfMedia.Brush _separatorBrush;
    private readonly WpfMedia.Brush _textBrush;
    private readonly WpfMedia.Brush _mutedTextBrush;
    private WpfControls.Border? _languageItem;
    private WpfControls.Border? _languageSubmenu;
    private bool _submenuVisible;

    private static double MainMenuHeight => (MenuPadding * 2) + (ItemHeight * 3) + SeparatorHeight;
    private static double SubMenuHeight => (MenuPadding * 2) + (ItemHeight * 2);
    private static double CollapsedWindowWidth => MenuWidth + (ShadowMargin * 2);
    private static double ExpandedWindowWidth => MenuWidth + MenuGap + SubMenuWidth + (ShadowMargin * 2);
    private static double WindowHeight => Math.Max(MainMenuHeight, SubMenuHeight + ItemHeight) + (ShadowMargin * 2);

    public TrayMenuWindow(
        bool lightTheme,
        TrayMenuText text,
        string currentLanguage,
        System.Drawing.Point anchor,
        Action showHotkeys,
        Action<string> setLanguage,
        Action exit)
    {
        _text = text;
        _showHotkeys = showHotkeys;
        _setLanguage = setLanguage;
        _exit = exit;

        _menuBackBrush = Brush(lightTheme ? WpfMedia.Color.FromRgb(249, 249, 249) : WpfMedia.Color.FromRgb(31, 31, 31));
        _menuBorderBrush = Brush(lightTheme ? WpfMedia.Color.FromRgb(222, 222, 222) : WpfMedia.Color.FromRgb(40, 40, 40));
        _hoverBrush = Brush(lightTheme ? WpfMedia.Color.FromRgb(238, 238, 238) : WpfMedia.Color.FromRgb(47, 47, 47));
        _separatorBrush = Brush(lightTheme ? WpfMedia.Color.FromRgb(224, 224, 224) : WpfMedia.Color.FromRgb(72, 72, 72));
        _textBrush = Brush(lightTheme ? WpfMedia.Color.FromRgb(24, 24, 24) : WpfMedia.Colors.White);
        _mutedTextBrush = Brush(lightTheme ? WpfMedia.Color.FromRgb(80, 80, 80) : WpfMedia.Color.FromRgb(210, 210, 210));

        AllowsTransparency = true;
        Background = WpfMedia.Brushes.Transparent;
        Content = _root;
        Focusable = true;
        Height = WindowHeight;
        ResizeMode = Wpf.ResizeMode.NoResize;
        ShowInTaskbar = false;
        SizeToContent = Wpf.SizeToContent.Manual;
        Topmost = true;
        Width = CollapsedWindowWidth;
        WindowStartupLocation = Wpf.WindowStartupLocation.Manual;
        WindowStyle = Wpf.WindowStyle.None;

        BuildMainMenu(currentLanguage);
        PositionNear(anchor);

        Deactivated += async (_, _) =>
        {
            await Task.Delay(180);
            if (IsVisible && !IsMouseOver)
            {
                Close();
            }
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == WpfInput.Key.Escape)
            {
                Close();
            }
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Activate();
    }

    private void BuildMainMenu(string currentLanguage)
    {
        WpfControls.StackPanel items = new();
        items.Children.Add(CreateItem(_text.Hotkeys, () => CloseThen(_showHotkeys)));
        _languageItem = CreateItem(_text.Language, ShowLanguageSubmenu, hasSubmenu: true);
        _languageItem.MouseEnter += (_, _) => ShowLanguageSubmenu();
        items.Children.Add(_languageItem);
        items.Children.Add(CreateSeparator());
        items.Children.Add(CreateItem(_text.Exit, () => CloseThen(_exit)));

        WpfControls.Border menu = CreatePanel(MenuWidth, MainMenuHeight, items);
        WpfControls.Canvas.SetLeft(menu, ShadowMargin);
        WpfControls.Canvas.SetTop(menu, ShadowMargin);
        _root.Children.Add(menu);

        _languageSubmenu = CreateLanguageSubmenu(currentLanguage);
        _languageSubmenu.Visibility = Wpf.Visibility.Collapsed;
        WpfControls.Canvas.SetLeft(_languageSubmenu, ShadowMargin + MenuWidth + MenuGap);
        WpfControls.Canvas.SetTop(_languageSubmenu, ShadowMargin + MenuPadding + ItemHeight);
        _root.Children.Add(_languageSubmenu);
    }

    private WpfControls.Border CreateLanguageSubmenu(string currentLanguage)
    {
        WpfControls.StackPanel items = new();
        items.Children.Add(CreateItem(_text.English, () => CloseThen(() => _setLanguage("en")), isChecked: currentLanguage == "en"));
        items.Children.Add(CreateItem(_text.Portuguese, () => CloseThen(() => _setLanguage("pt-BR")), isChecked: currentLanguage == "pt-BR"));
        return CreatePanel(SubMenuWidth, SubMenuHeight, items);
    }

    private WpfControls.Border CreatePanel(double width, double height, WpfControls.Panel content)
    {
        return new WpfControls.Border
        {
            Width = width,
            Height = height,
            Padding = new Wpf.Thickness(7, MenuPadding, 7, MenuPadding),
            Background = _menuBackBrush,
            BorderBrush = _menuBorderBrush,
            BorderThickness = new Wpf.Thickness(1),
            CornerRadius = new Wpf.CornerRadius(MenuCornerRadius),
            Effect = new WpfEffects.DropShadowEffect
            {
                BlurRadius = 14,
                Direction = 270,
                Opacity = 0.28,
                ShadowDepth = 2,
                Color = WpfMedia.Colors.Black
            },
            Child = content
        };
    }

    private WpfControls.Border CreateItem(string text, Action action, bool hasSubmenu = false, bool isChecked = false)
    {
        WpfControls.Border item = new()
        {
            Height = ItemHeight,
            Background = WpfMedia.Brushes.Transparent,
            CornerRadius = new Wpf.CornerRadius(ItemCornerRadius),
            SnapsToDevicePixels = true
        };

        WpfControls.Grid grid = new();
        grid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition { Width = new Wpf.GridLength(30) });
        grid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition { Width = new Wpf.GridLength(1, Wpf.GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition { Width = new Wpf.GridLength(18) });

        if (isChecked)
        {
            WpfShapes.Path check = new()
            {
                Data = WpfMedia.Geometry.Parse("M 7 17 L 12 22 L 22 11"),
                Stroke = _mutedTextBrush,
                StrokeEndLineCap = WpfMedia.PenLineCap.Round,
                StrokeLineJoin = WpfMedia.PenLineJoin.Round,
                StrokeStartLineCap = WpfMedia.PenLineCap.Round,
                StrokeThickness = 1.6,
                Width = 30,
                Height = ItemHeight
            };
            WpfControls.Grid.SetColumn(check, 0);
            grid.Children.Add(check);
        }

        WpfControls.TextBlock label = new()
        {
            Text = text,
            Foreground = _textBrush,
            FontFamily = new WpfMedia.FontFamily("Segoe UI"),
            FontSize = 12,
            VerticalAlignment = Wpf.VerticalAlignment.Center,
            TextTrimming = Wpf.TextTrimming.CharacterEllipsis
        };
        WpfControls.Grid.SetColumn(label, 1);
        grid.Children.Add(label);

        if (hasSubmenu)
        {
            WpfShapes.Path arrow = new()
            {
                Data = WpfMedia.Geometry.Parse("M 6 4 L 10 8 L 6 12"),
                Stroke = _textBrush,
                StrokeEndLineCap = WpfMedia.PenLineCap.Round,
                StrokeLineJoin = WpfMedia.PenLineJoin.Round,
                StrokeStartLineCap = WpfMedia.PenLineCap.Round,
                StrokeThickness = 1.4,
                Width = 16,
                Height = 16,
                VerticalAlignment = Wpf.VerticalAlignment.Center
            };
            WpfControls.Grid.SetColumn(arrow, 2);
            grid.Children.Add(arrow);
        }

        item.Child = grid;
        item.MouseEnter += (_, _) =>
        {
            item.Background = _hoverBrush;
            if (!ReferenceEquals(item, _languageItem))
            {
                HideLanguageSubmenu();
            }
        };
        item.MouseLeave += (_, _) =>
        {
            if (!ReferenceEquals(item, _languageItem) || !_submenuVisible)
            {
                item.Background = WpfMedia.Brushes.Transparent;
            }
        };
        item.PreviewMouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            action();
        };

        return item;
    }

    private WpfControls.Border CreateSeparator()
    {
        WpfControls.Border line = new()
        {
            Height = 1,
            Margin = new Wpf.Thickness(10, 4, 10, 4),
            Background = _separatorBrush
        };

        return new WpfControls.Border
        {
            Height = SeparatorHeight,
            Child = line
        };
    }

    private void ShowLanguageSubmenu()
    {
        if (_languageSubmenu is null)
        {
            return;
        }

        _submenuVisible = true;
        Width = ExpandedWindowWidth;
        _languageItem!.Background = _hoverBrush;
        _languageSubmenu.Visibility = Wpf.Visibility.Visible;
    }

    private void HideLanguageSubmenu()
    {
        if (_languageSubmenu is null)
        {
            return;
        }

        _submenuVisible = false;
        Width = CollapsedWindowWidth;
        _languageItem!.Background = WpfMedia.Brushes.Transparent;
        _languageSubmenu.Visibility = Wpf.Visibility.Collapsed;
    }

    private void CloseThen(Action action)
    {
        Close();
        action();
    }

    private void PositionNear(System.Drawing.Point anchor)
    {
        Forms.Screen screen = Forms.Screen.FromPoint(anchor);
        System.Drawing.Rectangle workArea = screen.WorkingArea;
        double left = anchor.X - MenuWidth + 16 - ShadowMargin;
        double top = anchor.Y - MainMenuHeight - ShadowMargin - 4;

        left = Math.Max(workArea.Left, Math.Min(left, workArea.Right - CollapsedWindowWidth));
        top = Math.Max(workArea.Top, Math.Min(top, workArea.Bottom - WindowHeight));

        Left = left;
        Top = top;
    }

    private static WpfMedia.SolidColorBrush Brush(WpfMedia.Color color)
    {
        WpfMedia.SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}

internal readonly record struct TrayMenuText(
    string Hotkeys,
    string Language,
    string English,
    string Portuguese,
    string Exit);

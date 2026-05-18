using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace MuteMIC.App;

internal sealed class ModernButton : Button
{
    private bool _hovered;
    private bool _pressed;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(80, 80, 80);
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBackColor { get; set; } = Color.FromArgb(56, 56, 56);
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color PressedBackColor { get; set; } = Color.FromArgb(64, 64, 64);
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 6;

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = RoundedRect(bounds, CornerRadius);
        Color fill = _pressed ? PressedBackColor : _hovered ? HoverBackColor : BackColor;
        using SolidBrush fillBrush = new(fill);
        using Pen borderPen = new(BorderColor);

        e.Graphics.FillPath(fillBrush, path);
        e.Graphics.DrawPath(borderPen, path);

        Color textColor = Enabled ? ForeColor : SystemColors.GrayText;
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            Rectangle focusRect = Rectangle.Inflate(ClientRectangle, -4, -4);
            ControlPaint.DrawFocusRectangle(e.Graphics, focusRect, textColor, fill);
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
}

internal sealed class ModernFieldPanel : Panel
{
    private TextBox? _textBox;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = Color.White;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(96, 96, 96);
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FocusBorderColor { get; set; } = Color.FromArgb(0, 120, 212);
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 4;

    public ModernFieldPanel()
    {
        DoubleBuffered = true;
        MinimumSize = new Size(0, 32);
        Padding = new Padding(10, 0, 10, 0);
    }

    public void Host(TextBox textBox)
    {
        _textBox = textBox;
        textBox.Dock = DockStyle.None;
        textBox.BorderStyle = BorderStyle.None;
        textBox.Margin = Padding.Empty;
        textBox.GotFocus += (_, _) => Invalidate();
        textBox.LostFocus += (_, _) => Invalidate();
        Controls.Add(textBox);
        PositionTextBox();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        PositionTextBox();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = RoundedRect(bounds, CornerRadius);
        using SolidBrush fillBrush = new(FillColor);
        using Pen borderPen = new(_textBox?.Focused == true ? FocusBorderColor : BorderColor);

        e.Graphics.FillPath(fillBrush, path);
        e.Graphics.DrawPath(borderPen, path);
    }

    private void PositionTextBox()
    {
        if (_textBox is null)
        {
            return;
        }

        int x = Padding.Left;
        int width = Math.Max(1, ClientSize.Width - Padding.Horizontal);
        int y = Math.Max(0, (ClientSize.Height - _textBox.PreferredHeight) / 2);
        _textBox.SetBounds(x, y, width, _textBox.PreferredHeight);
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
}

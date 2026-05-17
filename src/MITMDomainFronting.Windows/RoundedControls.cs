using System.Drawing.Drawing2D;

namespace MITMDomainFronting.Windows;

internal sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 14;
    public Color FillColor { get; set; } = Color.FromArgb(30, 30, 45);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundRect(ClientRectangle, Radius);
        using var brush = new SolidBrush(FillColor);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Invalidate();
    }

    internal static GraphicsPath RoundRect(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        var rect = new Rectangle(bounds.Location, new Size(diameter, diameter));

        path.AddArc(rect, 180, 90);
        rect.X = bounds.Right - diameter;
        path.AddArc(rect, 270, 90);
        rect.Y = bounds.Bottom - diameter;
        path.AddArc(rect, 0, 90);
        rect.X = bounds.Left;
        path.AddArc(rect, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class RoundedButton : Button
{
    public int Radius { get; set; } = 22;
    public Color FillColor { get; set; } = Color.FromArgb(106, 91, 255);
    public Color HoverColor { get; set; } = Color.FromArgb(128, 112, 255);
    public Color PressedColor { get; set; } = Color.FromArgb(82, 70, 210);

    private bool _hovered;
    private bool _pressed;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
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
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var fill = _pressed ? PressedColor : _hovered ? HoverColor : FillColor;

        using var path = RoundedPanel.RoundRect(ClientRectangle, Radius);
        using var brush = new SolidBrush(fill);
        pevent.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class StatusCircle : Control
{
    public bool Connected { get; set; }
    public bool Error { get; set; }

    public StatusCircle()
    {
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 19, FontStyle.Bold);
        ForeColor = Color.FromArgb(167, 164, 193);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var margin = 4;
        var rect = new Rectangle(margin, margin, Width - margin * 2 - 1, Height - margin * 2 - 1);
        var border = Error
            ? Color.FromArgb(255, 91, 91)
            : Connected
                ? Color.FromArgb(34, 232, 160)
                : Color.FromArgb(80, 78, 118);

        using var pen = new Pen(border, 3);
        using var fill = new SolidBrush(Color.FromArgb(27, 27, 43));
        e.Graphics.FillEllipse(fill, rect);
        e.Graphics.DrawEllipse(pen, rect);

        var text = Error ? "ERR" : Connected ? "ON" : "OFF";
        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            rect,
            Connected ? Color.FromArgb(34, 232, 160) : Error ? Color.FromArgb(255, 116, 116) : ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}


using System.Drawing.Drawing2D;
using UsdKrwWidget.Models;

namespace UsdKrwWidget.Controls;

internal sealed class SparklinePanel : Panel
{
    private IReadOnlyList<RatePoint> _points = Array.Empty<RatePoint>();

    public SparklinePanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    public void SetPoints(IReadOnlyList<RatePoint> points)
    {
        _points = points;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var border = new Pen(Color.FromArgb(226, 230, 235)))
            e.Graphics.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));

        if (_points.Count < 2)
        {
            using var emptyBrush = new SolidBrush(Color.FromArgb(130, 138, 148));
            e.Graphics.DrawString("No data", Font, emptyBrush, 7, 7);
            return;
        }

        var min = _points.Min(p => p.Rate);
        var max = _points.Max(p => p.Rate);
        var range = Math.Max(0.0001m, max - min);
        var pad = 5f;
        var width = Math.Max(1f, ClientSize.Width - pad * 2);
        var height = Math.Max(1f, ClientSize.Height - pad * 2);

        var pathPoints = new PointF[_points.Count];
        for (var i = 0; i < _points.Count; i++)
        {
            var x = pad + width * i / Math.Max(1, _points.Count - 1);
            var normalized = (float)((_points[i].Rate - min) / range);
            var y = pad + height * (1f - normalized);
            pathPoints[i] = new PointF(x, y);
        }

        var rising = _points[^1].Rate >= _points[0].Rate;
        var lineColor = rising ? Color.FromArgb(24, 148, 88) : Color.FromArgb(210, 69, 69);
        using var pen = new Pen(lineColor, 1.8f);
        e.Graphics.DrawLines(pen, pathPoints);
    }
}

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
        BackColor = Color.FromArgb(22, 24, 28);
    }

    public void SetPoints(IReadOnlyList<RatePoint> points)
    {
        _points = points;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_points.Count < 2)
        {
            using var emptyBrush = new SolidBrush(Color.Gray);
            e.Graphics.DrawString("No data", Font, emptyBrush, 8, 8);
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var min = _points.Min(p => p.Rate);
        var max = _points.Max(p => p.Rate);
        var range = Math.Max(0.0001m, max - min);
        var pad = 6f;
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
        using var pen = new Pen(rising ? Color.FromArgb(70, 200, 120) : Color.FromArgb(235, 95, 95), 2f);
        e.Graphics.DrawLines(pen, pathPoints);
    }
}

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
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.Clear(Color.White);

        DrawGrid(e.Graphics);

        if (_points.Count < 2)
        {
            using var emptyBrush = new SolidBrush(Color.FromArgb(137, 146, 156));
            e.Graphics.DrawString("No data", Font, emptyBrush, 8, 8);
            return;
        }

        var min = _points.Min(p => p.Rate);
        var max = _points.Max(p => p.Rate);
        var range = Math.Max(0.0001m, max - min);
        const float leftPad = 5f;
        const float rightPad = 5f;
        const float topPad = 5f;
        const float bottomPad = 5f;
        var width = Math.Max(1f, ClientSize.Width - leftPad - rightPad);
        var height = Math.Max(1f, ClientSize.Height - topPad - bottomPad);

        var pathPoints = new PointF[_points.Count];
        for (var i = 0; i < _points.Count; i++)
        {
            var x = leftPad + width * i / Math.Max(1, _points.Count - 1);
            var normalized = (float)((_points[i].Rate - min) / range);
            var y = topPad + height * (1f - normalized);
            pathPoints[i] = new PointF(x, y);
        }

        var rising = _points[^1].Rate >= _points[0].Rate;
        var lineColor = rising ? Color.FromArgb(28, 150, 91) : Color.FromArgb(218, 74, 74);

        using (var areaPath = new GraphicsPath())
        {
            areaPath.AddLines(pathPoints);
            areaPath.AddLine(pathPoints[^1].X, pathPoints[^1].Y, pathPoints[^1].X, ClientSize.Height - bottomPad);
            areaPath.AddLine(pathPoints[^1].X, ClientSize.Height - bottomPad, pathPoints[0].X, ClientSize.Height - bottomPad);
            areaPath.CloseFigure();

            using var fill = new LinearGradientBrush(
                new RectangleF(0, 0, ClientSize.Width, ClientSize.Height),
                Color.FromArgb(42, lineColor),
                Color.FromArgb(3, lineColor),
                LinearGradientMode.Vertical);
            e.Graphics.FillPath(fill, areaPath);
        }

        using (var pen = new Pen(lineColor, 1.9f))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            e.Graphics.DrawLines(pen, pathPoints);
        }

        var last = pathPoints[^1];
        using var dotBrush = new SolidBrush(lineColor);
        e.Graphics.FillEllipse(dotBrush, last.X - 2.4f, last.Y - 2.4f, 4.8f, 4.8f);
    }

    private void DrawGrid(Graphics graphics)
    {
        using var gridPen = new Pen(Color.FromArgb(239, 242, 246), 1f);
        for (var i = 1; i <= 2; i++)
        {
            var y = ClientSize.Height * i / 3f;
            graphics.DrawLine(gridPen, 4f, y, Math.Max(4f, ClientSize.Width - 4f), y);
        }
    }
}

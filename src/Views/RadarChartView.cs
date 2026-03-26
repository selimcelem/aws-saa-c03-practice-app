using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using AwsSaaC03Practice.Models;

namespace AwsSaaC03Practice.Views;

public class RadarChartView : SKCanvasView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items), typeof(IList<CategoryScore>), typeof(RadarChartView),
        propertyChanged: (b, _, _) => ((RadarChartView)b).InvalidateSurface());

    public IList<CategoryScore>? Items
    {
        get => (IList<CategoryScore>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public RadarChartView()
    {
        BackgroundColor = Colors.Transparent;
        HorizontalOptions = LayoutOptions.Fill;
        IgnorePixelScaling = true;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        var items = Items;
        if (items is null || items.Count < 3) return;

        var info = e.Info;
        var cx = info.Width / 2f;
        var cy = info.Height / 2f;
        var radius = Math.Min(cx, cy) * 0.62f;
        var n = items.Count;

        using var gridPaint = new SKPaint
        {
            Color = SKColor.Parse("#30363d"),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };

        float labelFontSize = 12;
        float pctFontSize = 10;
        using var labelFont = new SKFont(SKTypeface.Default, labelFontSize);
        using var labelPaint = new SKPaint { Color = SKColor.Parse("#c9d1d9"), IsAntialias = true };
        using var pctFont = new SKFont(SKTypeface.Default, pctFontSize);
        using var pctPaint = new SKPaint { Color = SKColor.Parse("#8b949e"), IsAntialias = true };

        // Draw concentric ring polygons (25%, 50%, 75%, 100%)
        for (int ring = 1; ring <= 4; ring++)
        {
            var r = radius * ring / 4f;
            using var ringPath = new SKPath();
            for (int i = 0; i < n; i++)
            {
                var angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                var px = cx + r * (float)Math.Cos(angle);
                var py = cy + r * (float)Math.Sin(angle);
                if (i == 0) ringPath.MoveTo(px, py);
                else ringPath.LineTo(px, py);
            }
            ringPath.Close();
            canvas.DrawPath(ringPath, gridPaint);
        }

        // Draw spokes, data points, and labels
        var points = new SKPoint[n];
        var labelMargin = 8f;

        for (int i = 0; i < n; i++)
        {
            var angle = -Math.PI / 2 + 2 * Math.PI * i / n;
            var cosA = (float)Math.Cos(angle);
            var sinA = (float)Math.Sin(angle);

            // Spoke
            var ex = cx + radius * cosA;
            var ey = cy + radius * sinA;
            canvas.DrawLine(cx, cy, ex, ey, gridPaint);

            // Data point
            var pct = Math.Clamp((float)(items[i].Percent / 100.0), 0f, 1f);
            points[i] = new SKPoint(cx + radius * pct * cosA, cy + radius * pct * sinA);

            // Label positioning
            var lx = cx + (radius + labelMargin) * cosA;
            var ly = cy + (radius + labelMargin) * sinA;
            var text = items[i].Category;
            var pctText = $"{items[i].Percent:F0}%";
            var textWidth = labelFont.MeasureText(text, out _);
            var pctWidth = pctFont.MeasureText(pctText, out _);

            // Horizontal alignment
            if (cosA < -0.2f) lx -= textWidth;
            else if (cosA >= -0.2f && cosA <= 0.2f) lx -= textWidth / 2f;

            // Vertical adjustment
            if (sinA < -0.2f) ly -= labelFontSize * 0.3f;
            else if (sinA > 0.2f) ly += labelFontSize * 1.2f;
            else ly += labelFontSize * 0.4f;

            canvas.DrawText(text, lx, ly, SKTextAlign.Left, labelFont, labelPaint);

            // Percent value below the label
            var pctLx = cosA < -0.2f ? lx + textWidth - pctWidth
                      : cosA > 0.2f ? lx
                      : lx + (textWidth - pctWidth) / 2f;
            canvas.DrawText(pctText, pctLx, ly + labelFontSize * 1.1f, SKTextAlign.Left, pctFont, pctPaint);
        }

        // Data polygon
        using var fillPaint = new SKPaint
        {
            Color = SKColor.Parse("#58a6ff").WithAlpha(50),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var strokePaint = new SKPaint
        {
            Color = SKColor.Parse("#58a6ff"),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        using var dotPaint = new SKPaint
        {
            Color = SKColor.Parse("#58a6ff"),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var dataPath = new SKPath();
        dataPath.MoveTo(points[0]);
        for (int i = 1; i < n; i++) dataPath.LineTo(points[i]);
        dataPath.Close();

        canvas.DrawPath(dataPath, fillPaint);
        canvas.DrawPath(dataPath, strokePaint);
        foreach (var pt in points) canvas.DrawCircle(pt, 4, dotPaint);
    }
}

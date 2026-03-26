using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using AwsSaaC03Practice.Models;

namespace AwsSaaC03Practice.Views;

public class BarChartView : SKCanvasView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items), typeof(IList<DomainScore>), typeof(BarChartView),
        propertyChanged: (b, _, _) => ((BarChartView)b).InvalidateSurface());

    public IList<DomainScore>? Items
    {
        get => (IList<DomainScore>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public BarChartView()
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
        if (items is null || items.Count == 0) return;

        var info = e.Info;
        var w = info.Width;
        var h = info.Height;
        var n = items.Count;

        // Layout constants
        var yAxisWidth = 40f;
        var xLabelHeight = 36f;
        var topPad = 22f;
        var chartLeft = yAxisWidth;
        var chartRight = w - 10f;
        var chartTop = topPad;
        var chartBottom = h - xLabelHeight;
        var chartW = chartRight - chartLeft;
        var chartH = chartBottom - chartTop;

        if (chartW <= 0 || chartH <= 0) return;

        // Paints
        using var gridPaint = new SKPaint
        {
            Color = SKColor.Parse("#30363d"),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        using var barPaint = new SKPaint
        {
            Color = SKColor.Parse("#58a6ff"),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var yLabelFont = new SKFont(SKTypeface.Default, 11);
        using var yLabelPaint = new SKPaint { Color = SKColor.Parse("#8b949e"), IsAntialias = true };
        using var xLabelFont = new SKFont(SKTypeface.Default, Math.Min(13, chartW / n * 0.3f));
        using var xLabelPaint = new SKPaint { Color = SKColor.Parse("#c9d1d9"), IsAntialias = true };
        using var valueLabelFont = new SKFont(SKTypeface.Default, 11);
        using var valueLabelPaint = new SKPaint { Color = SKColor.Parse("#c9d1d9"), IsAntialias = true };

        // Y-axis gridlines and labels (0%, 25%, 50%, 75%, 100%)
        for (int tick = 0; tick <= 4; tick++)
        {
            var pct = tick * 25;
            var y = chartBottom - chartH * tick / 4f;
            canvas.DrawLine(chartLeft, y, chartRight, y, gridPaint);

            var label = $"{pct}%";
            var labelW = yLabelFont.MeasureText(label, out _);
            canvas.DrawText(label, chartLeft - labelW - 4, y + 4, SKTextAlign.Left, yLabelFont, yLabelPaint);
        }

        // Bars
        var barGap = chartW * 0.15f / (n + 1);
        var barW = (chartW - barGap * (n + 1)) / n;
        barW = Math.Min(barW, 60);
        var totalBarsWidth = barW * n + barGap * (n - 1);
        var startX = chartLeft + (chartW - totalBarsWidth) / 2f;

        // Short labels
        var labels = new string[n];
        for (int i = 0; i < n; i++)
        {
            labels[i] = items[i].Domain
                .Replace("Design ", "")
                .Replace(" Architectures", "")
                .Replace("High-Performing", "High-Perf")
                .Replace("Cost-Optimized", "Cost-Opt");
        }

        for (int i = 0; i < n; i++)
        {
            var pct = (float)Math.Clamp(items[i].Percent, 0, 100);
            var barH = chartH * pct / 100f;
            var x = startX + i * (barW + barGap);
            var barTop = chartBottom - barH;

            // Bar with rounded top corners
            using var barRect = new SKPath();
            var cornerRadius = Math.Min(4, barW / 4);
            barRect.AddRoundRect(new SKRect(x, barTop, x + barW, chartBottom), cornerRadius, cornerRadius);
            canvas.DrawPath(barRect, barPaint);

            // Value label above bar
            var valueText = $"{pct:F0}%";
            var valueW = valueLabelFont.MeasureText(valueText, out _);
            canvas.DrawText(valueText, x + barW / 2 - valueW / 2, barTop - 4, SKTextAlign.Left, valueLabelFont, valueLabelPaint);

            // X-axis label below
            var labelW = xLabelFont.MeasureText(labels[i], out _);
            canvas.DrawText(labels[i], x + barW / 2 - labelW / 2, chartBottom + 18, SKTextAlign.Left, xLabelFont, xLabelPaint);
        }
    }
}

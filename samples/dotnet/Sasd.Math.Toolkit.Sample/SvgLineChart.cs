using System.Globalization;
using System.Text;

namespace Sasd.Math.Toolkit.Sample;

internal readonly record struct DemoPoint(double X, double Y);

internal sealed record DemoSeries(string Name, IReadOnlyList<DemoPoint> Points);

/// <summary>
/// Minimal SVG renderer used only by the demonstration application.
/// </summary>
/// <remarks>
/// This is intentionally not a general charting library. Its purpose is to keep the sample
/// cross-platform and dependency-free while demonstrating numerical results graphically.
/// A future SASD UI or graphics package can replace this renderer without changing the
/// numerical toolkit APIs.
/// </remarks>
internal static class SvgLineChart
{
    private const double Width = 720.0;
    private const double Height = 320.0;
    private const double Left = 58.0;
    private const double Right = 20.0;
    private const double Top = 34.0;
    private const double Bottom = 42.0;

    public static string Render(string title, IReadOnlyList<DemoSeries> series)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(series);
        if (series.Count == 0)
        {
            throw new ArgumentException("At least one series is required.", nameof(series));
        }

        var points = new List<DemoPoint>();
        foreach (var item in series)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(item.Points);
            foreach (var point in item.Points)
            {
                if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                {
                    throw new ArgumentOutOfRangeException(nameof(series), "SVG plot coordinates must be finite.");
                }

                points.Add(point);
            }
        }

        if (points.Count == 0)
        {
            throw new ArgumentException("At least one plot point is required.", nameof(series));
        }

        var xMin = points.Min(point => point.X);
        var xMax = points.Max(point => point.X);
        var yMin = points.Min(point => point.Y);
        var yMax = points.Max(point => point.Y);
        ExpandDegenerateRange(ref xMin, ref xMax);
        ExpandDegenerateRange(ref yMin, ref yMax);
        AddMargin(ref yMin, ref yMax, 0.08);

        var plotWidth = Width - Left - Right;
        var plotHeight = Height - Top - Bottom;

        double MapX(double x) => Left + (((x - xMin) / (xMax - xMin)) * plotWidth);
        double MapY(double y) => Top + (((yMax - y) / (yMax - yMin)) * plotHeight);

        var xAxisY = yMin <= 0.0 && yMax >= 0.0 ? MapY(0.0) : Height - Bottom;
        var yAxisX = xMin <= 0.0 && xMax >= 0.0 ? MapX(0.0) : Left;

        var builder = new StringBuilder(capacity: 8_000);
        builder.AppendLine("<svg viewBox=\"0 0 720 320\" role=\"img\" aria-label=\"Numerical line chart\">");
        builder.Append("  <title>").Append(Xml(title)).AppendLine("</title>");
        builder.Append("  <rect class=\"plot-frame\" x=\"").Append(F(Left)).Append("\" y=\"").Append(F(Top))
            .Append("\" width=\"").Append(F(plotWidth)).Append("\" height=\"").Append(F(plotHeight)).AppendLine("\" />");
        builder.Append("  <line class=\"plot-axis\" x1=\"").Append(F(Left)).Append("\" y1=\"").Append(F(xAxisY))
            .Append("\" x2=\"").Append(F(Width - Right)).Append("\" y2=\"").Append(F(xAxisY)).AppendLine("\" />");
        builder.Append("  <line class=\"plot-axis\" x1=\"").Append(F(yAxisX)).Append("\" y1=\"").Append(F(Top))
            .Append("\" x2=\"").Append(F(yAxisX)).Append("\" y2=\"").Append(F(Height - Bottom)).AppendLine("\" />");

        builder.Append("  <text class=\"plot-label\" x=\"").Append(F(Left)).Append("\" y=\"").Append(F(Height - 14.0)).Append("\">")
            .Append(Xml(Compact(xMin))).AppendLine("</text>");
        builder.Append("  <text class=\"plot-label\" text-anchor=\"end\" x=\"").Append(F(Width - Right)).Append("\" y=\"").Append(F(Height - 14.0)).Append("\">")
            .Append(Xml(Compact(xMax))).AppendLine("</text>");
        builder.Append("  <text class=\"plot-label\" x=\"4\" y=\"").Append(F(Top + 10.0)).Append("\">")
            .Append(Xml(Compact(yMax))).AppendLine("</text>");
        builder.Append("  <text class=\"plot-label\" x=\"4\" y=\"").Append(F(Height - Bottom)).Append("\">")
            .Append(Xml(Compact(yMin))).AppendLine("</text>");

        for (var seriesIndex = 0; seriesIndex < series.Count; seriesIndex++)
        {
            var item = series[seriesIndex];
            if (item.Points.Count == 1)
            {
                var point = item.Points[0];
                builder.Append("  <circle class=\"marker-").Append(seriesIndex % 3).Append("\" cx=\"")
                    .Append(F(MapX(point.X))).Append("\" cy=\"").Append(F(MapY(point.Y))).AppendLine("\" r=\"4\" />");
            }
            else if (item.Points.Count > 1)
            {
                builder.Append("  <polyline class=\"series-").Append(seriesIndex % 3).Append("\" points=\"");
                for (var pointIndex = 0; pointIndex < item.Points.Count; pointIndex++)
                {
                    if (pointIndex > 0)
                    {
                        builder.Append(' ');
                    }

                    var point = item.Points[pointIndex];
                    builder.Append(F(MapX(point.X))).Append(',').Append(F(MapY(point.Y)));
                }

                builder.AppendLine("\" />");
            }

            var legendY = 16.0 + (seriesIndex * 15.0);
            builder.Append("  <line class=\"series-").Append(seriesIndex % 3).Append("\" x1=\"455\" y1=\"")
                .Append(F(legendY)).Append("\" x2=\"475\" y2=\"").Append(F(legendY)).AppendLine("\" />");
            builder.Append("  <text class=\"plot-label\" x=\"482\" y=\"").Append(F(legendY + 4.0)).Append("\">")
                .Append(Xml(item.Name)).AppendLine("</text>");
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static void ExpandDegenerateRange(ref double minimum, ref double maximum)
    {
        if (minimum != maximum)
        {
            return;
        }

        var delta = System.Math.Max(1.0, System.Math.Abs(minimum) * 0.1);
        minimum -= delta;
        maximum += delta;
    }

    private static void AddMargin(ref double minimum, ref double maximum, double fraction)
    {
        var range = maximum - minimum;
        var margin = range * fraction;
        minimum -= margin;
        maximum += margin;
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Compact(double value) => value.ToString("G5", CultureInfo.InvariantCulture);

    private static string Xml(string value) => System.Net.WebUtility.HtmlEncode(value);
}

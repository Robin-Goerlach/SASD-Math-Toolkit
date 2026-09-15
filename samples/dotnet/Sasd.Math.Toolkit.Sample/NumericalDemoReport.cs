using System.Globalization;
using System.Text;
using Sasd.Numerics.Integration;
using Sasd.Numerics.RootFinding;
using Sasd.Numerics.Transforms;

namespace Sasd.Math.Toolkit.Sample;

/// <summary>
/// Creates a deterministic, self-contained HTML/SVG demonstration report for the toolkit.
/// </summary>
/// <remarks>
/// The report intentionally lives in the sample project rather than the numerical class
/// library. Rendering is an application concern; the reusable numerical core remains free
/// of browser, UI and file-format dependencies.
/// </remarks>
public static class NumericalDemoReport
{
    private const int DefaultSampleCount = 160;

    /// <summary>
    /// Builds the complete demonstration report as a self-contained HTML document.
    /// </summary>
    public static string Create()
    {
        var root = RootSolvers.NewtonRaphson(
            x => System.Math.Cos(x) - x,
            x => -System.Math.Sin(x) - 1.0,
            initialGuess: 0.5);

        var integral = NumericalIntegration.AdaptiveSimpson(
            System.Math.Sin,
            0.0,
            System.Math.PI);

        var rootSeries = SampleFunction(
            "cos(x) - x",
            x => System.Math.Cos(x) - x,
            0.0,
            1.2,
            DefaultSampleCount);

        var rootMarker = new DemoSeries(
            "computed root",
            [new DemoPoint(root.Root, 0.0)]);

        var integrationSeries = SampleFunction(
            "sin(x)",
            System.Math.Sin,
            0.0,
            System.Math.PI,
            DefaultSampleCount);

        var fftInput = CreateFftSignal(64);
        var fftSpectrum = FastFourierTransform.ForwardRealCompact(fftInput);
        var fftPoints = new DemoPoint[fftSpectrum.BinCount];
        for (var bin = 0; bin < fftSpectrum.BinCount; bin++)
        {
            fftPoints[bin] = new DemoPoint(bin, fftSpectrum[bin].Magnitude);
        }

        var dominantBin = FindDominantNonDcBin(fftSpectrum);

        double[] convolutionLeft = [1.0, 2.0, 1.0];
        double[] convolutionRight = [1.0, -1.0, 0.5];
        var convolution = FastFourierTransform.ConvolveReal(convolutionLeft, convolutionRight);
        var correlation = FastFourierTransform.CrossCorrelateReal(convolutionLeft, convolutionRight);

        var convolutionPoints = new DemoPoint[convolution.Length];
        for (var index = 0; index < convolution.Length; index++)
        {
            convolutionPoints[index] = new DemoPoint(index, convolution[index]);
        }

        var correlationPoints = new DemoPoint[correlation.Length];
        var firstLag = -(convolutionRight.Length - 1);
        for (var index = 0; index < correlation.Length; index++)
        {
            correlationPoints[index] = new DemoPoint(firstLag + index, correlation[index]);
        }

        var builder = new StringBuilder(capacity: 24_000);
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine("  <title>SASD Math Toolkit — Numerical Demo</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    :root { color-scheme: light dark; font-family: system-ui, sans-serif; }");
        builder.AppendLine("    body { max-width: 1180px; margin: 0 auto; padding: 2rem; line-height: 1.5; }");
        builder.AppendLine("    h1, h2 { line-height: 1.2; }");
        builder.AppendLine("    .intro { max-width: 75ch; }");
        builder.AppendLine("    .metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 1rem; margin: 1.5rem 0 2rem; }");
        builder.AppendLine("    .metric, .panel { border: 1px solid currentColor; border-radius: .6rem; padding: 1rem; }");
        builder.AppendLine("    .metric strong { display: block; font-size: 1.15rem; overflow-wrap: anywhere; }");
        builder.AppendLine("    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(420px, 1fr)); gap: 1rem; }");
        builder.AppendLine("    svg { width: 100%; height: auto; display: block; }");
        builder.AppendLine("    .plot-frame, .plot-axis { fill: none; stroke: currentColor; stroke-width: 1; opacity: .45; }");
        builder.AppendLine("    .plot-axis { opacity: .7; }");
        builder.AppendLine("    .plot-label { fill: currentColor; font-size: 12px; }");
        builder.AppendLine("    .series-0 { fill: none; stroke: #2f6fed; stroke-width: 2; }");
        builder.AppendLine("    .series-1 { fill: none; stroke: #d14a24; stroke-width: 2; }");
        builder.AppendLine("    .series-2 { fill: none; stroke: #238636; stroke-width: 2; }");
        builder.AppendLine("    .marker-0 { fill: #2f6fed; stroke: none; }");
        builder.AppendLine("    .marker-1 { fill: #d14a24; stroke: none; }");
        builder.AppendLine("    .marker-2 { fill: #238636; stroke: none; }");
        builder.AppendLine("    code { font-family: ui-monospace, monospace; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <h1>SASD Math Toolkit — Numerical Demo</h1>");
        builder.AppendLine("  <p class=\"intro\">This report is generated entirely by the .NET sample application. It exercises the real SASD Math Toolkit APIs and renders results as dependency-free inline SVG. It is a modern clean-room demonstration, not a reproduction of the historical Borland graphics programs.</p>");
        builder.AppendLine("  <div class=\"metrics\">");
        AppendMetric(builder, "Newton root", Format(root.Root), $"iterations: {root.Iterations}");
        AppendMetric(builder, "Integral of sin(x), 0..pi", Format(integral), "reference value: 2");
        AppendMetric(builder, "Dominant FFT bin", dominantBin.ToString(CultureInfo.InvariantCulture), "64-sample two-tone signal");
        AppendMetric(builder, "Correlation zero lag", Format(correlation[convolutionRight.Length - 1]), "explicit full-lag convention");
        builder.AppendLine("  </div>");
        builder.AppendLine("  <div class=\"grid\">");
        AppendPanel(builder, "Root finding", "Newton-Raphson applied to cos(x) - x.", SvgLineChart.Render("cos(x) - x and computed root", [rootSeries, rootMarker]));
        AppendPanel(builder, "Numerical integration", "Adaptive Simpson integration of sin(x) on [0, pi].", SvgLineChart.Render("sin(x) on the integration interval", [integrationSeries]));
        AppendPanel(builder, "Compact real FFT", "Magnitude of the non-redundant real FFT spectrum.", SvgLineChart.Render("FFT magnitude by bin", [new DemoSeries("magnitude", fftPoints)]));
        AppendPanel(builder, "Convolution and correlation", "Real-valued helpers use the same complex FFT foundation.", SvgLineChart.Render("Convolution index and correlation lag", [new DemoSeries("convolution", convolutionPoints), new DemoSeries("cross-correlation", correlationPoints)]));
        builder.AppendLine("  </div>");
        builder.AppendLine("  <p><small>Generated by <code>Sasd.Math.Toolkit.Sample</code>. All numeric formatting is culture-independent so the report is reproducible across developer machines and CI agents.</small></p>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static DemoSeries SampleFunction(
        string name,
        Func<double, double> function,
        double start,
        double end,
        int sampleCount)
    {
        var points = new DemoPoint[sampleCount + 1];
        for (var index = 0; index <= sampleCount; index++)
        {
            var fraction = index / (double)sampleCount;
            var x = start + ((end - start) * fraction);
            var y = function(x);
            if (!double.IsFinite(y))
            {
                throw new ArithmeticException($"Demo function '{name}' produced a non-finite value.");
            }

            points[index] = new DemoPoint(x, y);
        }

        return new DemoSeries(name, points);
    }

    private static double[] CreateFftSignal(int sampleCount)
    {
        var samples = new double[sampleCount];
        for (var index = 0; index < sampleCount; index++)
        {
            var phase = 2.0 * System.Math.PI * index / sampleCount;
            samples[index] =
                System.Math.Sin(5.0 * phase) +
                (0.5 * System.Math.Sin(12.0 * phase));
        }

        return samples;
    }

    private static int FindDominantNonDcBin(RealFftSpectrum spectrum)
    {
        if (spectrum.BinCount <= 1)
        {
            return 0;
        }

        var bestBin = 1;
        var bestMagnitude = spectrum[1].Magnitude;
        for (var bin = 2; bin < spectrum.BinCount; bin++)
        {
            var magnitude = spectrum[bin].Magnitude;
            if (magnitude > bestMagnitude)
            {
                bestMagnitude = magnitude;
                bestBin = bin;
            }
        }

        return bestBin;
    }

    private static void AppendMetric(StringBuilder builder, string label, string value, string detail)
    {
        builder.Append("    <div class=\"metric\"><span>")
            .Append(Html(label))
            .Append("</span><strong>")
            .Append(Html(value))
            .Append("</strong><small>")
            .Append(Html(detail))
            .AppendLine("</small></div>");
    }

    private static void AppendPanel(StringBuilder builder, string title, string description, string svg)
    {
        builder.Append("    <section class=\"panel\"><h2>")
            .Append(Html(title))
            .Append("</h2><p>")
            .Append(Html(description))
            .AppendLine("</p>");
        builder.AppendLine(svg);
        builder.AppendLine("    </section>");
    }

    private static string Format(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);
}

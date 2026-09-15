using System.Globalization;
using Sasd.Math.Toolkit.Sample;

namespace Sasd.Math.Toolkit.Tests;

public sealed class DemoApplicationTests
{
    [Fact]
    public void DemoReport_IsSelfContainedAndCoversTheMajorGraphicalExamples()
    {
        var html = NumericalDemoReport.Create();

        Assert.Contains("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<svg", html, StringComparison.Ordinal);
        Assert.Contains("Root finding", html, StringComparison.Ordinal);
        Assert.Contains("Numerical integration", html, StringComparison.Ordinal);
        Assert.Contains("Compact real FFT", html, StringComparison.Ordinal);
        Assert.Contains("Convolution and correlation", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NaN", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Infinity", html, StringComparison.Ordinal);
    }

    [Fact]
    public void DemoReport_IsDeterministic()
    {
        Assert.Equal(NumericalDemoReport.Create(), NumericalDemoReport.Create());
    }

    [Fact]
    public void DemoReport_UsesInvariantNumericFormatting()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            var html = NumericalDemoReport.Create();

            Assert.Contains("0.739", html, StringComparison.Ordinal);
            Assert.DoesNotContain("0,739", html, StringComparison.Ordinal);
            Assert.Contains("viewBox=\"0 0 720 320\"", html, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}

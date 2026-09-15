using Sasd.Numerics.Common;

namespace Sasd.Numerics.Approximation;

/// <summary>
/// Named least-squares models that become linear after a mathematical transformation.
/// </summary>
public static partial class LeastSquares
{
    /// <summary>
    /// Fits the positive-domain power model <c>y = a * x^b</c>.
    /// </summary>
    /// <remarks>
    /// The method applies the standard logarithmic transformation
    /// <c>ln(y) = ln(a) + b*ln(x)</c> and performs ordinary linear least squares in
    /// transformed coordinates. Consequently every x and y sample must be strictly
    /// positive. Residual diagnostics are calculated afterwards in the original y domain.
    /// </remarks>
    public static PowerLawFitResult FitPowerLaw(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));
        RequireAtLeastTwoSamples(x.Count, nameof(x), "fit a power law");

        var logX = new double[x.Count];
        var logY = new double[y.Count];

        for (var i = 0; i < x.Count; i++)
        {
            if (!double.IsFinite(x[i]) || x[i] <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    "Power-law fitting requires finite x values greater than zero.");
            }

            if (!double.IsFinite(y[i]) || y[i] <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(y),
                    "Power-law fitting requires finite y values greater than zero.");
            }

            logX[i] = System.Math.Log(x[i]);
            logY[i] = System.Math.Log(y[i]);
        }

        var (intercept, exponent) = FitStraightLine(
            logX,
            logY,
            nameof(x),
            "At least two distinct x values are required to determine a power-law exponent.");

        var scale = System.Math.Exp(intercept);
        if (!double.IsFinite(scale) || scale <= 0.0 || !double.IsFinite(exponent))
        {
            throw new ArithmeticException("Power-law fitting produced non-finite model parameters.");
        }

        var (residualSumOfSquares, rootMeanSquareError) = ComputeOriginalDomainDiagnostics(
            x,
            y,
            value => scale * System.Math.Pow(value, exponent),
            "power law");

        return new PowerLawFitResult(
            scale,
            exponent,
            x.Count,
            residualSumOfSquares,
            rootMeanSquareError);
    }

    /// <summary>
    /// Fits the exponential model <c>y = a * exp(b*x)</c>.
    /// </summary>
    /// <remarks>
    /// The method linearizes the model as <c>ln(y) = ln(a) + b*x</c>. Therefore all y
    /// observations must be finite and strictly positive, while x may be any finite real
    /// value. As with the power-law helper, the least-squares objective is minimized in
    /// transformed log-y space and the reported residual diagnostics are then calculated
    /// in the original y domain.
    /// </remarks>
    public static ExponentialFitResult FitExponential(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));
        RequireAtLeastTwoSamples(x.Count, nameof(x), "fit an exponential model");

        var logY = new double[y.Count];
        for (var i = 0; i < x.Count; i++)
        {
            if (!double.IsFinite(x[i]))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    "Exponential fitting requires finite x values.");
            }

            if (!double.IsFinite(y[i]) || y[i] <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(y),
                    "Exponential fitting requires finite y values greater than zero.");
            }

            logY[i] = System.Math.Log(y[i]);
        }

        var (intercept, rate) = FitStraightLine(
            x,
            logY,
            nameof(x),
            "At least two distinct x values are required to determine an exponential rate.");

        var scale = System.Math.Exp(intercept);
        if (!double.IsFinite(scale) || scale <= 0.0 || !double.IsFinite(rate))
        {
            throw new ArithmeticException("Exponential fitting produced non-finite model parameters.");
        }

        var (residualSumOfSquares, rootMeanSquareError) = ComputeOriginalDomainDiagnostics(
            x,
            y,
            value => scale * System.Math.Exp(rate * value),
            "exponential model");

        return new ExponentialFitResult(
            scale,
            rate,
            x.Count,
            residualSumOfSquares,
            rootMeanSquareError);
    }

    /// <summary>
    /// Fits a straight line <c>intercept + slope*x</c> through the shared linear-basis engine.
    /// </summary>
    /// <remarks>
    /// Keeping this transformation-specific adapter in one place prevents the named curve
    /// models from each growing their own miniature regression implementation. The upcoming
    /// logarithmic model can reuse the same path after transforming only its x coordinate.
    /// </remarks>
    private static (double Intercept, double Slope) FitStraightLine(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        string abscissaParameterName,
        string distinctAbscissaMessage)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));
        RequireAtLeastTwoSamples(x.Count, abscissaParameterName, "fit a two-parameter model");

        var first = x[0];
        var hasDistinctAbscissa = false;
        for (var i = 1; i < x.Count; i++)
        {
            if (x[i] != first)
            {
                hasDistinctAbscissa = true;
                break;
            }
        }

        if (!hasDistinctAbscissa)
        {
            throw new ArgumentException(distinctAbscissaMessage, abscissaParameterName);
        }

        var coefficients = FitBasis(
            x,
            y,
            [static _ => 1.0, static value => value]);

        return (coefficients[0], coefficients[1]);
    }

    /// <summary>
    /// Calculates residual diagnostics in the original observation domain.
    /// </summary>
    /// <remarks>
    /// Transformed least-squares models minimize residuals in transformed coordinates. We still
    /// expose original-domain RSS and RMSE because they have the same physical units as the input
    /// data and are therefore easier for callers to interpret. These diagnostics must not be
    /// mistaken for the objective function minimized by the transformed fit.
    /// </remarks>
    private static (double ResidualSumOfSquares, double RootMeanSquareError)
        ComputeOriginalDomainDiagnostics(
            IReadOnlyList<double> x,
            IReadOnlyList<double> y,
            Func<double, double> predictor,
            string modelName)
    {
        var residualSumOfSquares = 0.0;
        for (var i = 0; i < x.Count; i++)
        {
            var predicted = predictor(x[i]);
            if (!double.IsFinite(predicted))
            {
                throw new ArithmeticException(
                    $"The fitted {modelName} produced a non-finite prediction for an input sample.");
            }

            var residual = y[i] - predicted;
            residualSumOfSquares += residual * residual;
        }

        if (!double.IsFinite(residualSumOfSquares))
        {
            throw new ArithmeticException(
                $"The {modelName} residual calculation overflowed or became non-finite.");
        }

        return (
            residualSumOfSquares,
            System.Math.Sqrt(residualSumOfSquares / x.Count));
    }

    private static void RequireAtLeastTwoSamples(int sampleCount, string parameterName, string operation)
    {
        if (sampleCount < 2)
        {
            throw new ArgumentException(
                $"At least two data points are required to {operation}.",
                parameterName);
        }
    }
}

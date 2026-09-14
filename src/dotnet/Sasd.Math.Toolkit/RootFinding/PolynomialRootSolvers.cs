using System.Numerics;
using Sasd.Numerics.Common;
using Sasd.Numerics.Polynomials;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Root-finding algorithms specialized for polynomials.
/// </summary>
public static class PolynomialRootSolvers
{
    private const int DeflationFallbackStarts = 12;

    /// <summary>
    /// Applies Newton's method to a polynomial while evaluating value and derivative
    /// together with Horner's scheme.
    /// </summary>
    /// <remarks>
    /// The method accepts complex coefficients and a complex starting value. For a real
    /// polynomial and real start it behaves as the usual Newton-Horner iteration. Use
    /// <see cref="Polynomial.Deflate(Complex)"/> after convergence when sequential
    /// Newton-Horner deflation is desired.
    /// </remarks>
    public static ComplexRootResult NewtonHorner(
        Polynomial polynomial,
        Complex initialGuess,
        RootFindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(polynomial);
        EnsureRootable(polynomial);
        options ??= new RootFindingOptions();
        options.Validate();
        ComplexRootSolvers.EnsureFinite(initialGuess, nameof(initialGuess));

        var x = initialGuess;
        var evaluation = polynomial.EvaluateWithDerivatives(x);

        if (evaluation.Value.Magnitude <= options.Tolerance)
        {
            return new ComplexRootResult(x, evaluation.Value, 0, IterationStatus.Converged);
        }

        for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
        {
            if (ComplexRootSolvers.IsNearlyZero(evaluation.FirstDerivative))
            {
                return new ComplexRootResult(
                    x,
                    evaluation.Value,
                    iteration - 1,
                    IterationStatus.NumericalBreakdown,
                    "Polynomial derivative is too close to zero for a stable Newton-Horner step.");
            }

            var next = x - (evaluation.Value / evaluation.FirstDerivative);
            ComplexRootSolvers.EnsureFinite(next, nameof(polynomial));
            var nextEvaluation = polynomial.EvaluateWithDerivatives(next);

            if (ComplexRootSolvers.HasConverged(next, x, nextEvaluation.Value, options.Tolerance))
            {
                return new ComplexRootResult(next, nextEvaluation.Value, iteration, IterationStatus.Converged);
            }

            x = next;
            evaluation = nextEvaluation;
        }

        return new ComplexRootResult(
            x,
            evaluation.Value,
            options.MaximumIterations,
            IterationStatus.MaximumIterationsReached,
            "Maximum number of iterations reached.");
    }

    /// <summary>
    /// Finds one root of a polynomial with Laguerre's method.
    /// </summary>
    /// <remarks>
    /// Laguerre's method uses the polynomial value and its first two derivatives and is
    /// especially useful as the single-root engine for repeated complex deflation.
    /// </remarks>
    public static ComplexRootResult Laguerre(
        Polynomial polynomial,
        Complex initialGuess,
        RootFindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(polynomial);
        EnsureRootable(polynomial);
        options ??= new RootFindingOptions();
        options.Validate();
        ComplexRootSolvers.EnsureFinite(initialGuess, nameof(initialGuess));

        if (polynomial.Degree == 1)
        {
            var root = -polynomial.Coefficients[1] / polynomial.Coefficients[0];
            var value = polynomial.Evaluate(root);
            return new ComplexRootResult(root, value, 0, IterationStatus.Converged);
        }

        var x = initialGuess;
        var evaluation = polynomial.EvaluateWithDerivatives(x);
        var degree = (double)polynomial.Degree;

        if (evaluation.Value.Magnitude <= options.Tolerance)
        {
            return new ComplexRootResult(x, evaluation.Value, 0, IterationStatus.Converged);
        }

        for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
        {
            var logarithmicDerivative = evaluation.FirstDerivative / evaluation.Value;
            var curvature =
                (logarithmicDerivative * logarithmicDerivative) -
                (evaluation.SecondDerivative / evaluation.Value);

            var radical = Complex.Sqrt(
                (degree - 1.0) *
                ((degree * curvature) - (logarithmicDerivative * logarithmicDerivative)));

            var plus = logarithmicDerivative + radical;
            var minus = logarithmicDerivative - radical;
            var denominator = plus.Magnitude >= minus.Magnitude ? plus : minus;

            if (ComplexRootSolvers.IsNearlyZero(denominator))
            {
                return new ComplexRootResult(
                    x,
                    evaluation.Value,
                    iteration - 1,
                    IterationStatus.NumericalBreakdown,
                    "Laguerre denominator became too small for a stable step.");
            }

            var next = x - (degree / denominator);
            ComplexRootSolvers.EnsureFinite(next, nameof(polynomial));
            var nextEvaluation = polynomial.EvaluateWithDerivatives(next);

            if (ComplexRootSolvers.HasConverged(next, x, nextEvaluation.Value, options.Tolerance))
            {
                return new ComplexRootResult(next, nextEvaluation.Value, iteration, IterationStatus.Converged);
            }

            x = next;
            evaluation = nextEvaluation;
        }

        return new ComplexRootResult(
            x,
            evaluation.Value,
            options.MaximumIterations,
            IterationStatus.MaximumIterationsReached,
            "Maximum number of iterations reached.");
    }

    /// <summary>
    /// Finds all polynomial roots by repeated Laguerre search and synthetic deflation.
    /// </summary>
    /// <remarks>
    /// The solver is deterministic: it starts each deflated polynomial at zero, then uses
    /// a fixed set of points on a Cauchy root-bound circle only when the first start breaks
    /// down. Found roots are polished against the original polynomial before being returned.
    /// This implementation favors transparency and robustness over micro-optimization.
    /// </remarks>
    public static PolynomialRootsResult FindAllRootsLaguerre(
        Polynomial polynomial,
        RootFindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(polynomial);
        EnsureRootable(polynomial);
        options ??= new RootFindingOptions();
        options.Validate();

        var working = polynomial;
        var roots = new List<Complex>(polynomial.Degree);
        var totalIterations = 0;
        var rootOrdinal = 0;

        while (working.Degree > 0)
        {
            ComplexRootResult? rootResult = null;

            foreach (var initialGuess in BuildInitialGuesses(working, rootOrdinal))
            {
                rootResult = Laguerre(working, initialGuess, options);
                totalIterations += rootResult.Iterations;

                if (rootResult.Converged)
                {
                    break;
                }
            }

            if (rootResult is null || !rootResult.Converged)
            {
                return new PolynomialRootsResult(
                    Array.AsReadOnly(roots.ToArray()),
                    totalIterations,
                    rootResult?.Status ?? IterationStatus.NumericalBreakdown,
                    MaximumResidual(polynomial, roots),
                    rootResult?.Message ?? "No usable Laguerre start point was found.");
            }

            var root = SnapTinyImaginaryPart(rootResult.Root, options.Tolerance);
            var deflation = working.Deflate(root);

            roots.Add(root);
            working = deflation.Quotient;
            rootOrdinal++;
        }

        // Deflation accumulates rounding error. A short final polish against the original
        // polynomial makes the returned roots more useful without changing the deflation
        // sequence that already determined which roots were discovered.
        for (var index = 0; index < roots.Count; index++)
        {
            var polished = Laguerre(polynomial, roots[index], options);
            totalIterations += polished.Iterations;

            if (polished.Converged)
            {
                roots[index] = SnapTinyImaginaryPart(polished.Root, options.Tolerance);
            }
        }

        roots.Sort(CompareRoots);
        var residual = MaximumResidual(polynomial, roots);

        return new PolynomialRootsResult(
            Array.AsReadOnly(roots.ToArray()),
            totalIterations,
            IterationStatus.Converged,
            residual);
    }

    private static IEnumerable<Complex> BuildInitialGuesses(Polynomial polynomial, int rootOrdinal)
    {
        yield return Complex.Zero;

        var radius = CauchyRootBound(polynomial);
        for (var attempt = 0; attempt < DeflationFallbackStarts; attempt++)
        {
            // Root ordinal rotates the fallback pattern slightly so repeated deflations do
            // not always probe the same ray first.
            var turns = (attempt + (0.37 * rootOrdinal)) / DeflationFallbackStarts;
            var angle = 2.0 * System.Math.PI * turns;
            yield return Complex.FromPolarCoordinates(radius, angle);
        }
    }

    private static double CauchyRootBound(Polynomial polynomial)
    {
        var leadingMagnitude = polynomial.LeadingCoefficient.Magnitude;
        var maximumRatio = 0.0;

        for (var index = 1; index < polynomial.Coefficients.Count; index++)
        {
            var ratio = polynomial.Coefficients[index].Magnitude / leadingMagnitude;
            if (double.IsFinite(ratio))
            {
                maximumRatio = System.Math.Max(maximumRatio, ratio);
            }
        }

        var radius = 1.0 + maximumRatio;
        return double.IsFinite(radius) && radius > 0.0 ? radius : 1.0;
    }

    private static Complex SnapTinyImaginaryPart(Complex root, double tolerance)
    {
        var scale = System.Math.Max(1.0, System.Math.Abs(root.Real));
        return System.Math.Abs(root.Imaginary) <= 100.0 * tolerance * scale
            ? new Complex(root.Real, 0.0)
            : root;
    }

    private static double MaximumResidual(Polynomial polynomial, IReadOnlyList<Complex> roots)
    {
        var maximum = 0.0;
        for (var index = 0; index < roots.Count; index++)
        {
            maximum = System.Math.Max(maximum, polynomial.Evaluate(roots[index]).Magnitude);
        }

        return maximum;
    }

    private static int CompareRoots(Complex left, Complex right)
    {
        var realComparison = left.Real.CompareTo(right.Real);
        return realComparison != 0 ? realComparison : left.Imaginary.CompareTo(right.Imaginary);
    }

    private static void EnsureRootable(Polynomial polynomial)
    {
        if (polynomial.IsZero || polynomial.Degree < 1)
        {
            throw new ArgumentException("Root finding requires a non-constant polynomial.", nameof(polynomial));
        }
    }
}

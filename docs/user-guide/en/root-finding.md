# Roots of equations

Root finding asks for a value `x` for which

`f(x) = 0`.

The SASD Math Toolkit separates three related situations: scalar real equations, complex-valued equations, and polynomial roots. The public APIs deliberately expose convergence status instead of pretending that every mathematically valid iteration must succeed.

## Choosing a method

| Situation | Recommended starting point | Main trade-off |
|---|---|---|
| A real root is known to lie inside a sign-changing interval | `RootSolvers.Bisection` | Slow but very robust when a bracket exists |
| A good initial guess and derivative are available | `RootSolvers.NewtonRaphson` | Usually fast, but sensitive to the start and small derivatives |
| A derivative is unavailable, but two useful guesses are available | `RootSolvers.Secant` | Faster than bisection in many cases, but not bracket-safe |
| A polynomial root is wanted and a useful start is known | `PolynomialRootSolvers.NewtonHorner` | Efficient polynomial value/derivative evaluation, local convergence |
| A general complex function should be allowed to leave the real axis | `ComplexRootSolvers.Muller` | Three starting values; can converge to complex roots |
| One polynomial root should be found robustly in the complex plane | `PolynomialRootSolvers.Laguerre` | Specialized for polynomials; uses first and second derivatives |
| All polynomial roots are needed | `PolynomialRootSolvers.FindAllRootsLaguerre` | Repeated Laguerre search, deflation and final polishing |

No single root solver is universally best. A bracketed real problem is fundamentally different from finding every complex root of a polynomial.

## Common options and result information

The scalar, Muller, Newton-Horner and Laguerre solvers use `RootFindingOptions`:

```csharp
using Sasd.Numerics.RootFinding;

var options = new RootFindingOptions(
    Tolerance: 1e-12,
    MaximumIterations: 100);
```

`Tolerance` is positive and is used by the current implementation both for the residual and for a relative-scaled change in the iterate. `MaximumIterations` is a safety limit, not a promise that convergence will occur before that point.

Scalar methods return `RootResult`. The most useful properties are:

- `Root`: the best root estimate returned by the algorithm;
- `FunctionValue`: `f(Root)` for that estimate;
- `Residual`: `abs(FunctionValue)`;
- `Iterations`: completed iteration count;
- `Status`: why the algorithm stopped;
- `Converged`: shorthand for `Status == IterationStatus.Converged`;
- `Message`: optional diagnostic text.

Complex single-root methods return `ComplexRootResult`, whose `Residual` is `|f(root)|`. `FindAllRootsLaguerre` returns `PolynomialRootsResult` with the roots found, total iteration count and the largest residual measured against the original polynomial.

Programming-contract errors such as `NaN` input or an invalid interval are exceptions. Expected numerical outcomes such as a missing bisection bracket, a nearly zero Newton derivative, or reaching the iteration limit are represented by the result status.

## Bisection: the bracketed real method

Bisection is the first choice when a continuous function is known to change sign over an interval. For

`f(x) = cos(x) - x`

the interval `[0, 1]` brackets the root:

```csharp
using Sasd.Numerics.RootFinding;

var result = RootSolvers.Bisection(
    x => Math.Cos(x) - x,
    left: 0.0,
    right: 1.0);

if (!result.Converged)
{
    Console.WriteLine($"Bisection stopped with {result.Status}: {result.Message}");
}
else
{
    Console.WriteLine($"root = {result.Root:G17}");
    Console.WriteLine($"residual = {result.Residual:E3}");
}
```

Bisection repeatedly halves the bracket and keeps the half whose endpoints still have opposite signs. This gives predictable progress and makes it much less dependent on a clever starting guess than Newton or secant iteration.

If the endpoint values have the same sign, the toolkit returns `IterationStatus.NotBracketed`; it does not invent a root or silently switch algorithms. A same-sign interval does not prove that no root exists inside it, but it does mean that ordinary bisection has no valid sign-change bracket.

## Newton-Raphson: fast when the local model is good

Newton-Raphson uses the tangent line

`x(next) = x - f(x) / f'(x)`.

For the same equation:

```csharp
var result = RootSolvers.NewtonRaphson(
    x => Math.Cos(x) - x,
    x => -Math.Sin(x) - 1.0,
    initialGuess: 0.5);
```

Newton iteration is often much faster than bisection near a simple root, but it relies on local derivative information. A poor initial guess can move the sequence away from the desired root, and a derivative close to zero makes the Newton step unstable. The SASD implementation reports that condition as `IterationStatus.NumericalBreakdown` rather than dividing by an almost-zero value.

Use Newton-Raphson when the derivative is reliable and a plausible initial guess is available. If guaranteed bracket retention matters more than speed, prefer bisection or build a later safeguarded hybrid method on top of the existing primitives.

## Secant method: derivative-free local iteration

The secant method approximates the derivative from two recent function values:

```csharp
var result = RootSolvers.Secant(
    x => Math.Cos(x) - x,
    firstGuess: 0.0,
    secondGuess: 1.0);
```

It needs no derivative callback and commonly converges faster than bisection. Unlike bisection, however, the two guesses do not define a permanently protected bracket. If the two function values become almost equal, the secant slope becomes numerically unusable and the result reports `NumericalBreakdown`.

## Polynomial coefficients and Horner evaluation

Polynomial APIs use `Sasd.Numerics.Polynomials.Polynomial`. Coefficients are supplied in **descending power order**. For example:

```csharp
using Sasd.Numerics.Polynomials;

// x^3 - 2x - 5
var polynomial = new Polynomial([1.0, 0.0, -2.0, -5.0]);
```

This ordering is important. `[1, 0, -2, -5]` does not mean constant-first coefficients.

`PolynomialRootSolvers.NewtonHorner` combines Newton iteration with extended Horner evaluation, so the polynomial value and derivative are obtained efficiently in one pass:

```csharp
using System.Numerics;
using Sasd.Numerics.RootFinding;

var result = PolynomialRootSolvers.NewtonHorner(
    polynomial,
    initialGuess: new Complex(2.0, 0.0));
```

After a root has been found, `Polynomial.Deflate(root)` can remove the corresponding linear factor. The deflation result includes both the quotient polynomial and the remainder. A large remainder is a warning that the supplied value was not an accurate root.

## Muller: allowing a real start to become complex

Muller's method uses three recent points to fit a quadratic local model. Because the quadratic discriminant is evaluated as a complex number, a sequence that begins on the real axis may naturally move into the complex plane.

For `f(z) = z^2 + 1`:

```csharp
using System.Numerics;

static Complex Function(Complex z) => (z * z) + Complex.One;

var result = ComplexRootSolvers.Muller(
    Function,
    Complex.Zero,
    Complex.One,
    new Complex(2.0, 0.0));
```

A converged result should be close to either `+i` or `-i`. Which root is reached is determined by the numerical path; a single-root solver does not promise a particular member of a multi-root set unless the starting configuration makes that behavior sufficiently constrained.

## Laguerre for polynomial roots

Laguerre's method is specialized for polynomials and uses the value plus the first two derivatives. It is useful when roots may be complex:

```csharp
var quartic = new Polynomial([1.0, 0.0, 0.0, 0.0, 1.0]); // x^4 + 1
var oneRoot = PolynomialRootSolvers.Laguerre(quartic, Complex.One);
```

When all roots are required, use the higher-level operation rather than manually writing a deflation loop:

```csharp
var polynomial = new Polynomial([1.0, 0.0, 0.0, 0.0, -1.0]); // x^4 - 1
var allRoots = PolynomialRootSolvers.FindAllRootsLaguerre(polynomial);

if (allRoots.Converged)
{
    foreach (var root in allRoots.Roots)
    {
        Console.WriteLine(root);
    }

    Console.WriteLine($"maximum residual = {allRoots.MaximumResidual:E3}");
}
```

The current all-roots implementation is deterministic. It uses repeated Laguerre searches, synthetic deflation, fallback start points based on a Cauchy root bound, and a final polishing pass against the original polynomial. Polishing is important because rounding errors from successive deflations can otherwise accumulate.

## Interpreting convergence carefully

A small numerical step and a small residual are related but not identical ideas. The current root solvers accept convergence when either the function residual is within the requested tolerance or the change between successive iterates is sufficiently small relative to scale. For important calculations, inspect `Residual` as well as `Converged`.

A converged root is also not automatically the root you intended. Functions can have multiple roots, repeated roots can slow local methods, and starting guesses influence Newton, secant, Muller and Laguerre iterations. Bisection provides stronger localization because the root remains inside a sign-changing interval, but only for a valid real bracket.

## Practical workflow

For a new problem, first decide whether it is a general real equation, a complex equation, or a polynomial. If a real sign-changing interval is available, begin with bisection as a reference result. Use Newton or secant when speed matters and suitable starting information exists. For polynomials, prefer the polynomial-specific methods because they reuse Horner evaluation and provide deflation/all-roots support. Always record the termination status, residual and tolerances when numerical results are important to later analysis.

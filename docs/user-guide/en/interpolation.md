# Interpolation

Interpolation constructs a function that passes through known data points. It is useful when values are known only at discrete locations but an estimate between them is required.

SASD Math Toolkit V1 provides four closely related choices:

- direct Lagrange polynomial interpolation,
- Newton divided-difference interpolation,
- natural cubic splines,
- clamped cubic splines.

All are interpolation methods: at the supplied knots they reproduce the supplied values, subject only to floating-point round-off. They are not regression methods and do not smooth measurement noise.

## Choosing a method

Use **Lagrange** for a small data set and one or a few evaluations when the direct formula is convenient. Use **Newton divided differences** when the same interpolation polynomial will conceptually be reused or when its coefficient form is useful. Use a **cubic spline** when many knots are present and a piecewise-smooth curve is preferable to one high-degree global polynomial.

Natural and clamped splines differ only in their endpoint conditions. A natural spline assumes zero second derivative at both ends. A clamped spline accepts known first derivatives at both ends.

High-degree global polynomial interpolation can oscillate strongly, especially near the interval boundaries and for unfortunate knot distributions. More data points do not automatically make a global interpolation polynomial better.

## Lagrange interpolation

Suppose the points are `(0,1)`, `(1,4)` and `(2,9)`. They lie on `(x+1)^2`.

```csharp
using Sasd.Numerics.Interpolation;

double[] x = [0.0, 1.0, 2.0];
double[] y = [1.0, 4.0, 9.0];

var value = InterpolationAlgorithms.Lagrange(x, y, 1.5);
Console.WriteLine(value); // approximately 6.25
```

The x-values must be distinct, but they do not have to be sorted for polynomial interpolation. Inputs and the evaluation point must be finite.

`Lagrange` evaluates the basis formula directly. This keeps the implementation easy to inspect but repeats the O(n^2) basis work for every call. V1 deliberately favors this clear reference implementation over premature optimization.

## Newton divided differences

The same interpolation polynomial can be represented as

`a0 + a1(x-x0) + a2(x-x0)(x-x1) + ...`

The coefficients can be obtained separately:

```csharp
var coefficients = InterpolationAlgorithms.NewtonDividedDifferenceCoefficients(x, y);
```

For the example above they are approximately `[1, 3, 1]` in the Newton basis defined by the supplied x-order.

For a one-off evaluation there is a convenience method:

```csharp
var value = InterpolationAlgorithms.NewtonDividedDifference(x, y, 1.5);
```

The returned coefficient array is independent of the input arrays. Changing the input after coefficient construction does not change the coefficients.

## Natural cubic spline

A cubic spline uses a separate cubic polynomial on every interval while enforcing continuity of the function and its first two derivatives at interior knots.

```csharp
double[] x = [0.0, 1.0, 2.0, 3.0];
double[] y = [0.0, 1.0, 0.0, 1.0];

var spline = InterpolationAlgorithms.NaturalCubicSpline(x, y);

var value = spline.Evaluate(1.5);
var slope = spline.FirstDerivative(1.5);
var curvature = spline.SecondDerivative(1.5);
```

A natural spline adds the endpoint conditions

`S''(x0) = 0` and `S''(xn) = 0`.

This is a neutral boundary assumption, not a statement that the underlying real function truly has zero curvature there.

## Clamped cubic spline

If endpoint slopes are known, a clamped spline can incorporate them:

```csharp
static double F(double x) => x * x * x;
static double Df(double x) => 3.0 * x * x;

double[] x = [0.0, 1.0, 2.0, 3.0];
var y = x.Select(F).ToArray();

var spline = InterpolationAlgorithms.ClampedCubicSpline(
    x,
    y,
    leftDerivative: Df(x[0]),
    rightDerivative: Df(x[^1]));
```

With exact endpoint derivatives, a clamped cubic spline reproduces a cubic polynomial exactly apart from rounding error. In measured data, endpoint derivatives should only be supplied when they are meaningfully known; inaccurate slope constraints can distort the boundary region.

## Spline domain and immutability

Spline knots must be strictly increasing. The spline exposes:

```csharp
spline.Knots
spline.IntervalStart
spline.IntervalEnd
spline.SegmentCount
```

`Knots` is read-only, and the spline owns its internal coefficient data. The object can therefore be safely reused after construction without being changed by later modifications of input arrays.

Spline evaluation deliberately does **not** extrapolate. Calls outside `[IntervalStart, IntervalEnd]`, and calls with NaN or infinity, throw `ArgumentOutOfRangeException`. If extrapolation is required, it should be an explicit modeling decision rather than an accidental side effect of interpolation.

Polynomial `Lagrange` and `NewtonDividedDifference` do permit evaluation outside the supplied x-range because a global polynomial is mathematically defined there. That does not make polynomial extrapolation reliable.

## Numerical failure and invalid data

Duplicate polynomial abscissas are invalid because the interpolation polynomial would require division by zero. Spline knots must additionally be sorted and strictly increasing. Non-finite input data are rejected.

Finite input can still be so extremely scaled that intermediate floating-point arithmetic overflows or a spline system becomes numerically unusable. In those cases the reference implementation raises `ArithmeticException` rather than silently returning NaN or infinity.

For difficult data sets, useful first checks are to inspect scales, shift or normalize x-values when mathematically appropriate, and avoid unnecessarily high-degree global polynomials.

## Interpolation is not approximation

Interpolation forces the resulting curve through every supplied value. Least-squares approximation, described in the later handbook chapter, intentionally allows residuals in order to fit a model to noisy or overdetermined data. Choose based on the meaning of the data, not merely on which method produces a visually smooth curve.

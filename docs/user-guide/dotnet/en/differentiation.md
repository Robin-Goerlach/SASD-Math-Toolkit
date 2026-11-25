# Numerical differentiation

Numerical differentiation estimates a derivative when an analytic derivative is unavailable or inconvenient. SASD Math Toolkit provides three workflows: differentiate a callable function, differentiate sampled table data, or differentiate a cubic spline fitted to table data.

## Differentiating a function

```csharp
using Sasd.Numerics.Differentiation;

var first = NumericalDifferentiation.FirstDerivative(Math.Sin, 0.3);
var second = NumericalDifferentiation.SecondDerivative(Math.Sin, 0.3);
```

The default routines use centered differences plus Richardson refinement. Explicit five-point methods are available when that stencil is preferred.

## Differentiating table data

Suppose a measurement series contains increasing x-values and corresponding y-values:

```csharp
var x = new[] { 0.0, 0.5, 1.1, 2.0, 3.0 };
var y = x.Select(value => value * value).ToArray();

var first = TabularDifferentiation.FirstDerivativeThreePoint(x, y, 2);
var second = TabularDifferentiation.SecondDerivativeFivePoint(x, y, 2);
```

The final argument is the index of the table entry at which the derivative is required. The x-values may be non-uniformly spaced but must be finite and strictly increasing.

Available methods are:

- first derivative: two, three or five points;
- second derivative: three or five points.

Near the first or last table entry the library automatically shifts the local stencil instead of reading beyond the table. For smooth data, more points often reduce truncation error. For noisy measurements, however, differentiation can amplify noise, and a larger stencil is not automatically better.

## Differentiating a spline

A spline is useful when the data should first be represented by a smooth piecewise cubic curve. For repeated evaluations, build the spline once:

```csharp
using Sasd.Numerics.Interpolation;

var spline = InterpolationAlgorithms.NaturalCubicSpline(x, y);
var slope = spline.FirstDerivative(1.4);
var curvature = spline.SecondDerivative(1.4);
```

For a one-shot calculation, convenience helpers are available:

```csharp
var slope = SplineDifferentiation.NaturalFirstDerivative(x, y, 1.4);
```

If endpoint derivatives are known, use the clamped variant:

```csharp
var slope = SplineDifferentiation.ClampedFirstDerivative(
    x,
    y,
    leftDerivative: 0.0,
    rightDerivative: 6.0,
    point: 1.4);
```

Spline evaluation is restricted to the interpolation interval. SASD Math Toolkit does not silently extrapolate beyond the supplied data.

## Choosing a method

Use direct function differentiation when a function can be evaluated freely near the target point. Use tabular differentiation when the original data points themselves are the object of interest. Use spline differentiation when a smooth interpolating curve is a meaningful model for the data and derivatives are needed at arbitrary positions inside the interval.

No method can recover information that is absent from the data. In particular, derivative estimates from noisy measurements should be interpreted with more caution than derivatives of smooth mathematical functions.

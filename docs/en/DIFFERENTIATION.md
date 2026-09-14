# Numerical differentiation

The C# implementation now covers the complete historical V1 differentiation area with modern APIs for functions, tabular data and cubic-spline interpolants.

## Function differentiation

`NumericalDifferentiation` provides first- and second-derivative estimates for callable functions. The default methods use centered differences with Richardson refinement; explicit five-point variants are also available.

These routines are appropriate when a function can be evaluated at nearby points. They are not the best choice for measured or already sampled data because repeatedly evaluating the source function is then impossible or meaningless.

## Tabular differentiation

`TabularDifferentiation` provides the historical two/three/five-point family:

- `FirstDerivativeTwoPoint`
- `FirstDerivativeThreePoint`
- `FirstDerivativeFivePoint`
- `SecondDerivativeThreePoint`
- `SecondDerivativeFivePoint`

The public API accepts ordered `x` and `y` values plus the index at which the derivative is required. At interior entries the implementation chooses a local stencil around the requested point. Near the table boundaries the stencil is shifted and becomes one-sided automatically.

Unlike many textbook formulas, the SASD implementation does not require equal spacing. It derives the differentiation weights from the derivative of the local Lagrange interpolating polynomial. On equally spaced data these weights reduce to the familiar finite-difference formulas. Supporting non-uniform grids makes the API more useful for measured and scientific data without changing the historical method family.

Input requirements are deliberately explicit:

- `x` and `y` must have the same length;
- all coordinates must be finite;
- `x` must be strictly increasing;
- the requested index must exist;
- enough points must be present for the selected stencil.

The implementation favors a small and auditable polynomial-weight construction over a more optimized coefficient generator. With at most five stencil points this is computationally insignificant and keeps V1 easy to review.

## Spline differentiation

`CubicSpline` exposes both `FirstDerivative(point)` and `SecondDerivative(point)`. This is the preferred API when many derivatives of the same interpolant are needed.

`SplineDifferentiation` adds one-shot helpers that construct a natural or clamped spline and immediately evaluate a derivative:

- `NaturalFirstDerivative`
- `NaturalSecondDerivative`
- `ClampedFirstDerivative`
- `ClampedSecondDerivative`

A clamped spline is useful when endpoint derivatives are known. A natural spline imposes zero second derivative at both ends. Evaluation is restricted to the interpolation interval; extrapolation is intentionally not performed silently.

## Numerical interpretation

Differentiation amplifies noise. Increasing the stencil size can improve truncation error for smooth data, but it does not automatically improve noisy measurements. Spline differentiation can produce a smoother derivative curve, but that curve is the derivative of the chosen interpolant, not a guarantee about the unknown underlying physical process.

The V1 routines therefore expose the numerical estimate without attaching statistical confidence claims. Noise models, smoothing, uncertainty propagation and robust local regression belong to later statistics/data-analysis layers.

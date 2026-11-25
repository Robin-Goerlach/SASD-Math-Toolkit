# Interpolation design and numerical contracts

## Scope

The V1 interpolation domain contains direct Lagrange evaluation, Newton divided differences, natural cubic splines and clamped cubic splines. The implementation is independent new code; historical material is used only to identify functional coverage.

## API split

`InterpolationAlgorithms` contains construction and stateless polynomial helpers. `CubicSpline` is an immutable reusable result object that owns the knot and coefficient data needed for repeated value and derivative evaluation.

This split is intentional: building a spline requires a tridiagonal solve, while evaluating an already-built spline should only locate one segment and evaluate its cubic polynomial.

## Polynomial interpolation

`Lagrange` evaluates the direct Lagrange basis. It is the clearest reference form and requires no persistent interpolant object, at the cost of repeated O(n^2) work.

`NewtonDividedDifferenceCoefficients` computes the in-place triangular divided-difference table in a private copy of y and returns the Newton coefficients. `NewtonDividedDifference` is a convenience call that constructs those coefficients and evaluates the nested Newton form immediately.

Polynomial x-values must be finite and unique, but need not be sorted. The supplied order defines the Newton basis.

## Cubic splines

Natural and clamped construction use one shared `BuildCubicSpline` implementation. Only the first and last equations differ; the interior tridiagonal elimination and backward substitution are identical. Keeping that numerical core in one place reduces the risk that two spline variants diverge over time.

Natural boundary conditions set the endpoint second derivatives to zero. Clamped boundary conditions use caller-supplied endpoint first derivatives.

Spline knots must be finite and strictly increasing. `CubicSpline` clones its coefficient buffers and exposes knots through a read-only view. Evaluation outside the knot interval is rejected rather than silently extrapolated.

## Numerical failure policy

Programming-contract errors such as duplicate x-values, unordered spline knots, mismatched lengths or non-finite inputs are reported as argument exceptions.

Finite inputs can still overflow during subtraction, divided-difference construction, tridiagonal elimination or polynomial evaluation. Such numerical breakdowns are reported as `ArithmeticException`; the API does not intentionally return NaN or infinity as a successful interpolation result.

This follows the broader V1 convention: malformed input is different from arithmetic failure on otherwise structurally valid data.

## Performance position

The V1 code is a readable dependency-free reference implementation. It deliberately does not introduce barycentric Lagrange weights, reusable Newton-interpolant objects, vectorized batch evaluation or specialized spline work buffers yet. Those are valid future optimizations once real workloads justify them and can be added without changing the fundamental mathematical contracts.

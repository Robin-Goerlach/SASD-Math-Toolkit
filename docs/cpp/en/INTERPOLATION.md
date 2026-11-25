# Interpolation and cubic splines

CPP-M3 introduces `sasd::numerics::interpolation` as an independent C++20
implementation of the shared interpolation contract.

## Public API

- `InterpolationAlgorithms::linear` for a line through two distinct points;
- direct Lagrange polynomial evaluation;
- Newton divided-difference coefficient construction and one-shot evaluation;
- immutable `NewtonInterpolant` for repeated scalar or batch evaluation;
- immutable natural and clamped `CubicSpline` values and first/second derivatives.

Polynomial abscissas must be distinct but need not be sorted. Spline knots must
be strictly increasing. All coordinates, endpoint derivatives and evaluation
points must be finite. Spline evaluation is restricted to the closed knot
interval; polynomial and linear extrapolation remain mathematically permitted.

## Ownership and numerical checks

Reusable interpolants and splines copy their completed state. Later caller
changes to input vectors therefore cannot invalidate an object. Exact duplicate
nodes are rejected, and non-finite intermediate results are reported as
numerical errors.

Natural and clamped splines share one tridiagonal construction path. The
interior elimination is not duplicated, which prevents the two boundary modes
from drifting apart. The public spline exposes read-only knots and segment
metadata, but no mutable coefficient storage.

## Parallel batch evaluation

Each target point in `evaluate_many` is independent. Newton and spline batch
methods therefore honor `ExecutionOptions` and preserve input order. Spline
construction and divided-difference construction remain sequential because
their recurrences are dependent.

The library does not define one hardware-independent profitable crossover.
`automatic` uses the caller-adjustable `minimum_work_per_thread`; applications
may also select `sequential`, force `parallel`, or cap `maximum_threads`.
Forcing parallel execution requests workers but does not promise a speedup.

The language-neutral behavior is defined in
[`spec/interpolation.md`](../../../spec/interpolation.md), with analytic shared
cases under `conformance/interpolation/`.

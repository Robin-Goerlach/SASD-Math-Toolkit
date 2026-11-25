# Interpolation and splines

Interpolation constructs a function from known sample points. CPP-M3 provides
simple line interpolation, polynomial methods and piecewise cubic splines. The
choice matters: a high-degree polynomial is global, whereas changing one
spline interval primarily affects neighboring segments.

## Linear interpolation

```cpp
#include <sasd/numerics/interpolation/interpolation_algorithms.hpp>

using sasd::numerics::interpolation::InterpolationAlgorithms;

const double value = InterpolationAlgorithms::linear(
    2.0, 10.0,
    6.0, 18.0,
    4.0); // 14.0
```

The two abscissas must differ. Evaluation outside their interval is allowed and
is linear extrapolation; the caller remains responsible for whether that model
is meaningful.

## Lagrange and Newton polynomials

```cpp
#include <sasd/numerics/interpolation/interpolation_algorithms.hpp>

#include <array>

constexpr std::array x{0.0, 1.0, 2.0};
constexpr std::array y{1.0, 4.0, 9.0};

const double lagrange = InterpolationAlgorithms::lagrange(x, y, 1.5);
const double newton =
    InterpolationAlgorithms::newton_divided_difference(x, y, 1.5);
// Both are 6.25 for y=(x+1)^2.
```

Lagrange is direct and transparent for one evaluation. Newton divided
differences are preferable when the same data set is evaluated repeatedly.

```cpp
#include <sasd/numerics/interpolation/newton_interpolant.hpp>

using sasd::numerics::interpolation::NewtonInterpolant;

const NewtonInterpolant interpolant{x, y};
const double first = interpolant.evaluate(1.5);
const double second = interpolant.evaluate(2.5);
```

The interpolant copies nodes and coefficients. Mutating the original arrays
later does not change it.

## Natural and clamped cubic splines

```cpp
#include <sasd/numerics/interpolation/interpolation_algorithms.hpp>

constexpr std::array knots{0.0, 1.0, 2.0, 3.0};
constexpr std::array values{0.0, 1.0, 0.0, 1.0};

const auto natural =
    InterpolationAlgorithms::natural_cubic_spline(knots, values);

const double value = natural.evaluate(1.5);
const double slope = natural.first_derivative(1.5);
const double curvature = natural.second_derivative(1.5);
```

A natural spline has zero second derivative at both endpoints. A clamped
spline instead uses known endpoint slopes:

```cpp
const auto clamped = InterpolationAlgorithms::clamped_cubic_spline(
    knots, values, 1.0, 2.0);
```

Splines reject extrapolation. Use `interval_start()`, `interval_end()` and
`knots()` to inspect the valid closed interval.

## Batch evaluation and CPU policy

Batch target points are independent, so they can use the shared execution
policy:

```cpp
#include <sasd/numerics/execution/execution_options.hpp>

#include <vector>

using sasd::numerics::execution::ExecutionOptions;
using sasd::numerics::execution::ExecutionPolicy;

const std::vector<double> targets{0.0, 0.25, 0.5, 1.0, 1.5, 2.0};
ExecutionOptions execution{
    ExecutionPolicy::automatic,
    4,    // at most four workers
    1024  // at least this many target points per worker
};

const auto results = interpolant.evaluate_many(targets, execution);
```

There is no batch size that is optimal for every CPU and application. Worker
creation, the polynomial degree, cache behavior and surrounding application
parallelism all influence the crossover. Adjust `minimum_work_per_thread` for
your workload, select `sequential` when the application already distributes
larger jobs, or use `parallel` when explicit worker execution is required.

Spline construction and Newton coefficient construction remain sequential:
their recurrences depend on earlier steps. Parallelism applies only to the
independent target points and preserves their order.

## Error behavior

- all coordinates and evaluation points must be finite;
- coordinate arrays must have equal length;
- polynomial nodes must be distinct;
- spline knots must be strictly increasing;
- splines require at least two knots and reject points outside their interval;
- non-finite intermediate results are reported instead of returned.

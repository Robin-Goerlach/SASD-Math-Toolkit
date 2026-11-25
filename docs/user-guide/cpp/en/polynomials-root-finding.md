# Polynomials and root finding

## Choosing a method

| Problem | First choice | Main trade-off |
| --- | --- | --- |
| Real root in a known sign-changing interval | Bisection | Robust but linearly convergent |
| Reliable derivative and useful start | Newton-Raphson | Fast locally; derivative may become unusable |
| No derivative, two useful starts | Secant | Derivative-free but does not preserve a bracket |
| General complex function | Muller | Can leave the real axis; needs three starts |
| One polynomial root | Newton-Horner or Laguerre | Newton is local; Laguerre uses two derivatives |
| Every polynomial root | All-roots Laguerre | Deflation requires final polishing |

No solver can guarantee that an arbitrary starting value reaches the particular
root an application intended. Status and residual must be recorded with the
returned approximation.

## Scalar real roots

```cpp
#include <sasd/numerics/root_finding/root_solvers.hpp>

#include <cmath>
#include <iostream>

int main() {
    const auto function = [](double x) { return std::cos(x) - x; };

    const auto result =
        sasd::numerics::root_finding::RootSolvers::bisection(function, 0.0, 1.0);

    if (result.converged()) {
        std::cout << result.root() << " residual=" << result.residual() << '\n';
    }
}
```

Newton-Raphson additionally accepts a derivative callback. The secant method
accepts two starting estimates. All three use `RootFindingOptions`:

```cpp
sasd::numerics::root_finding::RootFindingOptions options;
options.tolerance = 1.0e-12;
options.maximum_iterations = 100;
```

`not_bracketed`, `numerical_breakdown` and
`maximum_iterations_reached` are returned statuses, not exceptions. Invalid
intervals, non-finite input and non-finite callbacks violate the API contract
and are rejected with exceptions.

## Polynomial representation

Coefficients are written in descending power order:

```cpp
#include <sasd/numerics/polynomials/polynomial.hpp>

// x^3 - 2x - 5
const sasd::numerics::polynomials::Polynomial polynomial{1.0, 0.0, -2.0, -5.0};

const auto value = polynomial.evaluate(2.0);
const auto evaluation = polynomial.evaluate_with_derivatives(2.0);
const auto derivative = polynomial.derivative();
```

The object owns its coefficients and exposes only a read-only span. `deflate`
performs synthetic division by `(x-root)` and returns both quotient and
remainder. A large remainder means that the supplied value was not an accurate
root.

## Complex and polynomial roots

```cpp
#include <sasd/numerics/root_finding/polynomial_root_solvers.hpp>

#include <complex>

using sasd::numerics::root_finding::PolynomialRootSolvers;

const auto one_root =
    PolynomialRootSolvers::newton_horner(polynomial, std::complex<double>{2.0, 0.0});

const sasd::numerics::polynomials::Polynomial quartic{1.0, 0.0, 0.0, 0.0, -1.0};
const auto all_roots = PolynomialRootSolvers::find_all_roots_laguerre(quartic);
```

The all-roots solver uses deterministic fallback starts, synthetic deflation
and a polishing pass against the original polynomial. Its
`maximum_residual()` is therefore an independently useful final diagnostic.

Muller's method accepts a function from `std::complex<double>` to
`std::complex<double>`. The complex square root in its quadratic model allows
real starting points to converge to `+i` or `-i` for `z^2+1`.

## Parallel execution

The next point of Newton, secant, Muller or Laguerre depends on the preceding
point. Creating worker threads inside one recurrence would add overhead without
independent work. CPP-M2 therefore keeps individual searches sequential. An
application may safely distribute independent equations or parameter sets at a
higher level; a future batch API can formalize this using `ExecutionOptions`.

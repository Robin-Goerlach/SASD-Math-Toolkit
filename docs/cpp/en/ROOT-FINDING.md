# Polynomials and root finding

CPP-M2 introduces the immutable `sasd::numerics::polynomials::Polynomial` and
the `sasd::numerics::root_finding` solver family.

## Public algorithms

- scalar bisection, Newton-Raphson and secant iteration;
- complex Muller iteration;
- polynomial Newton-Horner and Laguerre iteration;
- deterministic all-roots Laguerre search with synthetic deflation and final
  polishing against the original polynomial.

Coefficients are stored in descending power order. `std::complex<double>` is
used for polynomial coefficients and complex roots; real coefficient
constructors remain available for ordinary real polynomials.

## Diagnostics and errors

`RootResult`, `ComplexRootResult` and `PolynomialRootsResult` keep termination
status, approximation, iteration count, residual and optional message
separate. Missing brackets, numerical breakdown and iteration exhaustion are
expected numerical outcomes. Empty callbacks, malformed options and non-finite
caller input are contract errors. Non-finite callback values and overflowed
iterates are rejected before being supplied to caller code again.

The convergence test accepts a sufficiently small residual or a relative-
scaled iterate change. Applications should still inspect the returned residual
and confirm that the root reached is the root relevant to their problem.

## Numerical safeguards

- `std::midpoint` protects bisection from endpoint-addition overflow;
- Newton and secant steps refuse numerically singular denominators;
- Muller and Laguerre select the larger-magnitude quadratic denominator;
- only exact leading polynomial zeros are discarded;
- deflated roots are polished against the original polynomial;
- all-roots results report the largest original-polynomial residual.

One root iteration is inherently sequential. CPP-M2 therefore does not attach
threads to dependent recurrences. Parallel CPU policy remains available for
independent large workloads, and later batch-root APIs may use it explicitly.

The language-neutral behavior is defined in
[`spec/root-finding.md`](../../../spec/root-finding.md), with shared scalar
cases under `conformance/root-finding/`.

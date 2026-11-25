# SASD Math Toolkit C++ documentation

This directory documents the independent modern C++ edition of SASD Math
Toolkit. It is complete enough to understand and build the C++ tree without
consulting the C#/.NET manual. The .NET implementation remains the mathematical
reference while C++ is under development; it is not a source-code template.

## Current status

CPP-M3 advances the library to version `0.4.0`:

- C++20 and CMake project structure;
- public namespace root `sasd::numerics`;
- iteration status, result and numerical-default types;
- explicit sequential, automatic and parallel CPU execution policies;
- CTest tests, an executable sample and installed-package smoke test;
- CMake install/export package `SasdMathToolkit`;
- Doxygen-compatible public comments and an optional API target;
- descriptive statistics with stable mean, median and population/sample variance;
- deterministic multi-core reductions for sufficiently large mean/variance inputs;
- immutable complex-capable polynomials with Horner evaluation and deflation;
- scalar, complex and polynomial root solvers with explicit diagnostics;
- linear, Lagrange and Newton interpolation plus immutable natural and clamped cubic splines;
- caller-controlled parallel batch evaluation for independent interpolation targets.

No numerical algorithm catalog is claimed yet. C++ reaches 1.0 only after the
defined feature and cross-language conformance gates are complete.

## Documents

- [Architecture](ARCHITECTURE.md)
- [Building, testing and installation](BUILDING.md)
- [Parallel execution](PARALLELISM.md)
- [Descriptive statistics](STATISTICS.md)
- [Polynomials and root finding](ROOT-FINDING.md)
- [Interpolation and cubic splines](INTERPOLATION.md)
- [English C++ user handbook](../../user-guide/cpp/en/README.md)
- [German C++ documentation](../de/README.md)

## Separation rule

C++ source, tests, samples, build logic and manuals live only in their C++
trees. Language-neutral behavior belongs in `spec/`, and shared reference data
belongs in `conformance/`. Fortran and JavaScript production work starts only
after the C++ edition is complete enough to provide a mature second-language
contract.

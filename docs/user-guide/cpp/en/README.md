# SASD Math Toolkit C++ handbook

This is the independent English user and developer handbook for the modern C++
edition of SASD Math Toolkit. It documents C++ APIs, CMake, ownership, error
handling and execution behavior directly; it does not require the C#/.NET
handbook.

## Edition status

Handbook version `0.4` accompanies CPP-M3. The stable .NET
edition already provides the broad numerical reference catalog, but the C++
edition contains its build/package foundation, common result vocabulary, CPU
execution policy, descriptive statistics, polynomials, root finding,
interpolation and cubic splines. Later chapters are added together with the
corresponding tested C++ algorithms.

## Contents

1. [Getting started](getting-started.md)
2. [Diagnostics and parallel execution](parallel-execution.md)
3. [Descriptive statistics](descriptive-statistics.md)
4. [Polynomials and root finding](polynomials-root-finding.md)
5. [Interpolation and splines](interpolation-splines.md)

Future chapters will cover calculus, dense and sparse linear algebra,
eigenvalue methods, ODE/BVP methods, approximation and transforms.
Every example in this handbook will compile as C++20.

## Guiding principle

A numerical value alone is not a trustworthy result. Method, input contract,
tolerance, termination status and an independent quality diagnostic belong
together. Parallel execution must preserve this contract and must be justified
by measurements rather than assumed from the number of threads.

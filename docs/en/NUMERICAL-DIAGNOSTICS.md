# Numerical diagnostics and termination contracts

This document defines the cross-cutting diagnostic conventions used by the C#/.NET reference implementation. It complements the domain-specific algorithm notes and is intended to become part of the language-neutral behavioral contract as additional implementations are added.

## 1. Three layers of failure reporting

The library distinguishes three categories deliberately.

### 1.1 Input-contract failures

Invalid dimensions, null arguments, non-finite required inputs, non-positive tolerances and other caller contract violations use ordinary .NET exceptions, normally `ArgumentException` or `ArgumentOutOfRangeException`.

The core C# implementation therefore does not normally turn malformed public input into `IterationStatus.InvalidInput`. That enum member remains available as shared vocabulary for result-oriented adapters or future language implementations, but it is not a reason to weaken validation at the C# API boundary.

### 1.2 Expected numerical termination

When the call is valid but an iterative/adaptive process cannot satisfy its requested criterion, the algorithm should return a result status where practical. Examples include:

- `IterationStatus.MaximumIterationsReached`;
- `IterationStatus.NumericalBreakdown`;
- `IterationStatus.NotBracketed`;
- `AdaptiveIntegrationStatus.MaximumDepthReached`;
- `AdaptiveIntegrationStatus.NumericalResolutionReached`;
- `AdaptiveOdeStatus.MaximumStepAttemptsReached`;
- `AdaptiveOdeStatus.MinimumStepSizeReached`.

This makes normal numerical control flow inspectable without exceptions.

### 1.3 Direct arithmetic failure

A direct operation that cannot produce a meaningful finite value and has no useful iterative result envelope may throw `ArithmeticException`. Examples include a numerically singular direct solve or an overflowed direct computation.

## 2. Generic iterative result semantics

`IterativeResult<T>` separates five concepts:

- `Value`: computed value or method-specific best candidate;
- `Iterations`: number of completed iterations;
- `Status`: termination reason;
- `Residual`: optional problem-specific defect measure;
- `Message`: optional human-readable diagnostic context.

`Converged` is exactly `Status == IterationStatus.Converged`.

`HasFiniteResidual` is exactly `double.IsFinite(Residual)` and deliberately does not imply convergence. A result can reach its iteration limit and still expose a useful finite residual. A numerical breakdown can occur before a meaningful residual exists.

The default `Residual = double.NaN` means that the generic result does not expose a finite residual for that outcome. It is not an invitation to accept non-finite values as normal algorithm input.

## 3. Residual is domain-specific

The word “residual” always needs a mathematical definition. V1 currently uses, among others:

- root finding: `|f(x)|`;
- linear systems: `||A*x-b||∞`;
- eigenpairs: `||A*v-lambda*v||2`;
- iterative algorithms: a method-specific residual documented by that API.

Residuals from different domains have different dimensions, scales and interpretations. They must not be compared merely because they occupy a property named `Residual`.

A residual is also not automatically the forward error in the unknown. Conditioning determines how equation defect translates into solution error.

## 4. Error estimates are not exact errors

Adaptive integration and RKF-style ODE methods derive error indicators from differences between numerical approximations. These estimates guide refinement but are not comparisons with an exact solution.

Documentation must therefore use words such as `EstimatedError`, local error estimate or refinement indicator rather than imply knowledge of an exact global error.

## 5. Tolerance ownership

There is no mutable global convergence tolerance. Each algorithm owns and documents its criterion.

`NumericConstants.DefaultTolerance` is a convenience default, not a universal accuracy guarantee. `NumericConstants.NearlyZero` is a small absolute breakdown guard, not machine epsilon and not a general-purpose approximate-equality rule.

Where a threshold represents a material numerical policy decision, public APIs should expose it explicitly. The LU pivot tolerance and inverse-power pivot tolerance are examples.

Future M3 comparison/tolerance helpers may add reusable scaled comparison abstractions, but they must not retroactively obscure the simple V1 algorithm contracts.

## 6. Status values are not interchangeable across domains

The toolkit does not force every numerical method into one oversized status enum. `IterationStatus` covers common iterative behavior; adaptive quadrature and adaptive ODE integration retain domain-specific statuses because depth exhaustion and minimum-step exhaustion have different meanings and recovery actions.

The architectural requirement is consistent *semantics*, not a single enum type.

## 7. Finite-value policy

Public numerical inputs are expected to be finite unless explicitly documented otherwise. Callback results are checked by stable V1 algorithms where propagation of `NaN` or infinity would destroy diagnostic value.

A non-finite value created by arithmetic is treated as a numerical failure, not as a normal converged result.

## 8. Diagnostic independence

Where practical, important result types provide a problem-facing diagnostic that can be recomputed independently of the stopping loop. Examples are:

- `RootResult.Residual`;
- `LinearSystemSolvers.ResidualInfinityNorm`;
- `EigenSolvers.EigenpairResidualNorm`.

This is preferred to exposing only an internal “change between iterations”, because a tiny update can occur even when the mathematical equation is not yet satisfied.

Some iterative algorithms intentionally require both an update criterion and a problem residual. `GaussSeidel` is an example: convergence requires both a sufficiently small component update and a sufficiently small linear-system residual.

## 9. Conditioning belongs in interpretation, not hidden status magic

V1 result statuses report what the algorithm observed. They do not attempt to infer a universal condition number or silently relabel every sensitive problem as failure.

Documentation must distinguish:

- algorithmic convergence;
- backward defect/residual;
- forward error;
- problem conditioning;
- model/discretization error.

Later modern linear-algebra/statistics modules may expose explicit condition estimates where that is mathematically justified.

## 10. Determinism and reproducibility

Reference algorithms should be deterministic for the same finite inputs and options unless randomness is explicitly part of the future API. Diagnostics should contain enough information for tests and applications to understand why an algorithm stopped.

For reproducible research or validation workflows, applications should record method, options, status, iteration/refinement counts, residual/error indicator and toolkit version/commit alongside the final numerical value.

## 11. Cross-language direction

When C++, Java, JavaScript/TypeScript and Fortran implementations are added, the language-neutral `spec/` layer should preserve the semantic distinctions in this document while allowing idiomatic exception/error mechanisms per language.

Cross-language conformance should compare mathematical behavior, termination categories and diagnostics—not require identical source structure or identical exception class names.

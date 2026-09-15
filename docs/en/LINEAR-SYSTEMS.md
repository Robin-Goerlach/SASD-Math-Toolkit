# Dense matrices and linear systems

## Purpose

The V1 linear-algebra layer provides a dependency-free reference implementation for the historical numerical-methods scope while establishing reusable abstractions for later SASD projects.

The central separation is:

```text
DenseMatrix
   |
   +-- basic storage / multiplication
   |
LinearSystemSolvers
   +-- one-shot Gaussian elimination
   +-- determinant
   +-- residual diagnostics
   +-- Gauss-Seidel iteration
   |
LuFactorization
   +-- reusable P*A=L*U decomposition
   +-- repeated vector/matrix solves
   +-- determinant / inverse reuse
```

This keeps data representation, one-shot algorithms and reusable decomposition state distinct.

## DenseMatrix invariant

`DenseMatrix` is mutable in its entries but immutable in its dimensions. Every stored entry must be finite. Constructors and the indexer reject NaN and infinity.

The finite-value invariant is intentional. It prevents a bad input value from travelling through several algorithms before surfacing as an unrelated convergence problem.

The type remains deliberately small: no sparse storage, views, slicing expression trees or operator-heavy abstraction layer are introduced for V1.

## Arithmetic overflow

Finite input values can still overflow during multiplication or elimination. Reference operations therefore test important computed products/accumulations and throw `ArithmeticException` rather than silently storing infinity.

This is diagnostic hardening, not arbitrary-precision arithmetic. Scaling a problem remains the caller's responsibility when ordinary IEEE-754 `double` range is insufficient.

## Gaussian elimination

`LinearSystemSolvers.SolveGaussian` performs elimination on copies of the coefficient matrix and right-hand side, followed by back substitution.

Partial pivoting is enabled by default. At pivot column `k`, the algorithm chooses the row with the largest `abs(A[i,k])` among the remaining rows. The optional unpivoted path is preserved for historical/educational comparison.

A pivot at or below the positive absolute `pivotTolerance` causes an `ArithmeticException`. This covers singular matrices and matrices treated as numerically singular under the chosen threshold.

## LU factorization

`LuFactorization` uses the convention

```text
P * A = L * U
```

and stores `L` and `U` in one compact matrix. `Permutation`, `LowerTriangular` and `UpperTriangular` expose defensive/independent data for diagnostics. `PivotTolerance` records the threshold used to create the factorization.

Repeated solves should reuse the factorization. This is an architectural distinction, not merely a speed trick: decomposition is setup state, triangular substitution is the repeatable operation.

## Determinants

`LinearSystemSolvers.Determinant` retains a direct elimination implementation because a numerically singular determinant naturally returns zero instead of requiring creation of a factorization object that rejects the matrix.

When an LU factorization already exists, `LuFactorization.Determinant` is the preferred path and multiplies the diagonal entries of `U` together with permutation parity.

Both paths detect non-finite arithmetic overflow.

## Inverse

`LinearSystemSolvers.Inverse` delegates to one LU factorization and solves all columns of the identity matrix. It also accepts the same pivot tolerance.

The API exists because an inverse is a legitimate matrix object required by some algorithms and by the historical compatibility scope. It is not the recommended way to solve `A*x=b`; direct solving avoids constructing an unnecessary matrix and generally has better numerical behavior.

## Residual contract

`ResidualInfinityNorm` computes

```text
max_i |(A*x-b)_i|
```

and validates matrix/vector dimensions and finite vector values. Non-finite arithmetic during residual construction is reported as `ArithmeticException`.

A residual is a backward diagnostic. It must not be confused with a condition estimate or a bound on forward error.

## Gauss-Seidel

Gauss-Seidel returns `IterativeResult<double[]>`. Convergence requires both:

1. maximum component update `<= tolerance`, and
2. residual infinity norm `<= tolerance`.

The method returns `NumericalBreakdown` for zero/near-zero diagonal entries and for non-finite arithmetic caused by divergence/overflow. It returns `MaximumIterationsReached` when the iteration budget is exhausted.

This reflects the toolkit-wide policy: invalid API input throws; expected outcomes of a valid iterative process are represented by status.

## Conditioning

V1 does not claim to estimate the condition number. Partial pivoting and a pivot threshold improve robustness but do not turn an ill-conditioned problem into a well-conditioned one.

A later SASD linear-algebra milestone may add scaled pivoting, condition estimation, iterative refinement and optional high-performance backends. Those features should be layered on the current abstractions instead of complicating the V1 reference implementation prematurely.

## Testing strategy

Regression tests cover:

- finite matrix invariants and index bounds;
- multiplication dimension checks and overflow detection;
- determinant, numerical zero and determinant overflow;
- Gaussian solve with and without required pivoting;
- preservation of caller-owned inputs;
- LU reconstruction, repeated solves, defensive permutation state, determinant and inverse;
- Gauss-Seidel convergence and breakdown status;
- residual validation and analytical reference solutions.

The same matrix foundation is also exercised indirectly by eigenvalue, least-squares and other higher-level algorithms.

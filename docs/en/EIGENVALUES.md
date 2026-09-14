# Eigenvalues and eigenvectors

The C# reference implementation now covers the complete historical V1 eigenvalue/eigenvector catalog while keeping the APIs useful beyond compatibility work.

## Available algorithms

### Power method

`EigenSolvers.PowerMethod` approximates the eigenpair whose eigenvalue has the largest magnitude. It returns an `IterativeResult<Eigenpair>` containing convergence status, iteration count and residual norm.

### Inverse power method

`EigenSolvers.InversePowerMethod` targets the eigenvalue nearest zero in the current unshifted API. The matrix is LU-factorized once and the same factorization is reused for every iteration.

A shifted inverse iteration is intentionally left for a later extension because it deserves an explicit API rather than an implicit hidden shift convention.

### Wielandt deflation

`WielandtDeflation.Create` implements a classical rank-one Wielandt transformation. Given a known right eigenpair

`A v = lambda v`,

it constructs

`B = A - v x^T`

with `x^T v = lambda`. The selected row is based on the largest absolute component of `v`, reducing the risk of dividing by a small component. The removed eigenvalue is replaced by zero while the remaining eigenvalues are retained.

`EigenSolvers.WielandtSecondEigenpair` combines this transformation with two power iterations: first find the dominant eigenpair, deflate it, find the dominant eigenpair of the deflated matrix, and restore that eigenvector to the coordinates of the original matrix.

This is primarily a historical/reference algorithm. Repeated or nearly repeated dominant eigenvalues make the restoration ill-conditioned; the implementation reports a numerical breakdown rather than hiding that limitation.

### Cyclic Jacobi method

`EigenSolvers.CyclicJacobi` computes the complete eigensystem of a **real symmetric** matrix. Each sweep applies plane rotations to every off-diagonal pair. The accumulated orthogonal rotations form the eigenvector matrix.

The result is a `SymmetricEigendecomposition`:

- eigenvalues are sorted in descending numerical order;
- eigenvectors are stored as columns;
- `GetEigenpair(index)` returns a defensive eigenpair copy;
- public arrays/matrices are copied so a finished decomposition cannot be mutated accidentally.

Convergence is measured by the off-diagonal Frobenius norm relative to the Frobenius norm of the input matrix. Symmetry is checked with a separate relative tolerance.

## Design notes

These routines deliberately use readable textbook-style loops rather than SIMD, blocked matrix kernels or external linear algebra packages. They are reference implementations for small and medium dense problems and for cross-language conformance work.

Future high-performance adapters may use LAPACK/BLAS behind separate abstractions without changing the behavioral contracts of these reference algorithms.

For modern production eigensolvers, additional work will eventually be useful: shifted inverse iteration, QR-based general eigensolvers, specialized symmetric tridiagonal reduction, and sparse Krylov methods. Those are extensions beyond the historical V1 compatibility target.

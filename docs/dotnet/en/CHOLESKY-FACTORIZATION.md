# Cholesky factorization

`CholeskyFactorization` is the second modern dense-linear-algebra building block after Householder QR. It specializes real symmetric positive-definite matrices using `A = L * L^T`.

The factorization is reusable for vector and multi-right-hand-side solves. It exposes a defensive copy of `L`, ordinary and logarithmic determinants, and explicit relative tolerances for symmetry and numerical positive-definiteness.

`LogDeterminant()` is particularly useful for statistical and likelihood work because it avoids forming a determinant that may overflow or underflow before its logarithm is taken.

Use Cholesky for SPD systems, pivoted LU for general square systems, Householder QR for square/tall least squares, and the planned SVD for rank-deficient or underdetermined problems.

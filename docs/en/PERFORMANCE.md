# Performance policy and V1 optimization round

## Purpose

The V1 optimization round improves hot paths without turning the dependency-free reference implementation into low-level HPC code. Correctness, finite-value diagnostics and stable public APIs remain requirements; within those constraints, avoidable allocations, repeated callback evaluations and poor row-major memory access should not remain simply for aesthetic reasons.

## Optimizations applied

### Dense matrices

`DenseMatrix` keeps its public checked indexer, but trusted algorithms in the same assembly can use validated row spans. Matrix-vector multiplication now addresses the row-major backing array directly after one input-validation pass. Matrix-matrix multiplication uses a row-inner-column loop order, so rows of the right matrix and result are traversed contiguously. This improves cache locality without unsafe code, parallel scheduling or manual SIMD.

### LU factorization and solves

LU elimination and substitutions use row spans instead of repeated checked two-dimensional indexing. A scalar solve performs forward and backward substitution in one working vector. Multi-right-hand-side solving is genuinely batched: it applies the permutation and triangular substitutions to all RHS columns together instead of allocating temporary vectors and invoking the scalar solver for every column. Matrix inversion benefits automatically because it is implemented as a batched solve against the identity matrix.

### Least squares

General basis functions are evaluated once per sample and cached in a compact design matrix. The normal matrix is accumulated only in its upper triangle and mirrored afterwards. Polynomial fitting generates powers by recurrence instead of allocating power delegates and repeatedly calling `Math.Pow`. The numerical model is unchanged: V1 still uses normal equations with pivoted dense solving; QR/SVD remains the planned direction for difficult conditioning, not a performance shortcut hidden in this round.

### FFT, convolution and correlation

Public FFT calls still copy caller-owned inputs. Internally owned arrays are transformed in place. Real FFT paths no longer pass through LINQ plus a second complex-input copy. Compact real spectra can take ownership of a newly allocated internal bin array while remaining immutable publicly. Convolution/correlation now transform their zero-padded buffers directly, multiply spectra in place and inverse-transform the same buffer; real overloads populate padded complex buffers directly rather than creating temporary exact-size complex sequences first.

## Deliberately deferred

This round does not add unsafe pointers, manual SIMD, `ArrayPool` lifetime complexity, parallel loops, cache-blocked matrix kernels, a specialized real-only FFT, native BLAS/LAPACK, or timing thresholds in CI. Those choices need representative benchmarks and workload evidence. The current changes target clear algorithmic/allocation overhead with low semantic risk.

## Regression strategy

CI remains correctness-oriented. Optimization tests verify mathematical equivalence, input immutability and observable work reduction such as one basis callback invocation per sample. Wall-clock assertions are intentionally excluded because shared CI timing is noisy. A dedicated benchmark project can be added after V1 when representative SASD workloads are available.

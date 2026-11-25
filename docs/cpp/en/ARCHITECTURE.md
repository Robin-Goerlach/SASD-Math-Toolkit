# C++ architecture

## 1. Role of the C++ edition

The C++ edition is a portable, dependency-light reference implementation of the
same mathematical contracts as SASD Math Toolkit C#/.NET. Equivalent behavior
does not require identical internal code. C++ APIs use RAII, value semantics,
standard-library types and explicit ownership.

## 2. Physical boundaries

```text
src/cpp/                 public headers, implementation and CMake package
tests/cpp/               C++-only executable tests and install consumer
samples/cpp/             C++-only examples
docs/cpp/                C++ technical documentation
docs/user-guide/cpp/     C++ Markdown handbook sources
docs/user-guide/LaTeX/cpp/ C++ typesetting project
```

The C++ build does not compile .NET, Fortran or JavaScript sources. Conversely,
the existing .NET solution does not include C++ files.

## 3. Namespace and headers

The root namespace is `sasd::numerics`. Mathematical domains become nested
namespaces such as `common`, `execution`, `root_finding`, `linear_algebra` and
`transforms`. Installed headers follow the same hierarchy below
`include/sasd/numerics/`.

Public declarations receive Doxygen-compatible comments that explain contracts,
tolerances, ownership, termination semantics and relevant complexity. Comments
inside implementations explain mathematical or numerical reasons rather than
restating the code.

## 4. Error and result semantics

Invalid public input is rejected with ordinary C++ exceptions appropriate to
the contract. Expected numerical outcomes such as an iteration limit or a
breakdown use status-bearing results. `IterativeResult<T>` deliberately keeps
the approximation, status, iteration count, residual and optional message
separate.

The presence of a finite residual does not imply convergence, and residuals
from unrelated problems are not directly comparable.

## 5. Ownership

RAII is mandatory. Value types and `std::vector` are preferred. Views such as
`std::span` may be used when lifetime rules are explicit. Public results must
not retain hidden references to temporary caller storage. Manual `new` and
`delete` are not part of normal numerical implementation.

## 6. Parallel execution boundary

Parallelism is explicit through `execution::ExecutionOptions`; there is no
mutable process-wide thread setting. `automatic` is the default and only uses
multiple cores when a method has independent work and enough of it to amortize
thread overhead. `sequential` supports reproducible diagnosis and avoids nested
parallelism. `parallel` is available for controlled benchmarking or callers
that know their workload.

Internal work is partitioned into deterministic contiguous chunks. Algorithms
must still define whether floating-point reduction order can change and must
validate numerical quality independently. See [Parallel execution](PARALLELISM.md).

## 7. Dependencies and backends

The reference library depends on the C++ standard library and the platform
thread facility exposed by CMake's `Threads` package. Optional BLAS/LAPACK,
SIMD, OpenMP or GPU backends may be added only behind stable SASD contracts and
after benchmarks justify them. The portable implementation remains available.

# Diagnostics and parallel execution

## Status, value and residual

`IterativeResult<T>` retains five independent pieces of information:

- the calculated value or best available approximation;
- completed iteration count;
- termination status;
- a problem-specific residual when available;
- an optional diagnostic message.

For example, reaching an iteration limit can still leave a useful approximation
and finite residual. A numerical breakdown may leave no meaningful residual at
all. Callers should not turn either situation silently into success.

## Selecting CPU behavior

Algorithms that support parallel work accept
`sasd::numerics::execution::ExecutionOptions`:

```cpp
#include <sasd/numerics/execution/execution_options.hpp>

using sasd::numerics::execution::ExecutionOptions;
using sasd::numerics::execution::ExecutionPolicy;

ExecutionOptions automatic;  // default: use useful available cores

ExecutionOptions serial;
serial.policy = ExecutionPolicy::sequential;

ExecutionOptions bounded_parallel;
bounded_parallel.policy = ExecutionPolicy::parallel;
bounded_parallel.maximum_threads = 4;
```

The options do not promise that every algorithm creates threads. A scalar
recurrence or small matrix remains serial. A large independent batch can use
multiple workers in automatic mode.

## Reproducibility

Contiguous work partitioning is deterministic, but parallel floating-point
reductions can change operation order and therefore the last few bits. The
library will document such cases and test mathematical tolerances, residuals or
reconstruction errors. Select `sequential` when investigating order-sensitive
behavior or when the application already performs outer-level parallelism.

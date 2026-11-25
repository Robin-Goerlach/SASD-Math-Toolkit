# Parallel execution

## Goal

The C++ edition should use available CPU cores when that improves a meaningful
workload, while retaining a readable and portable reference implementation.
Thread count alone is not a performance claim; each parallelized algorithm must
eventually be benchmarked against its serial path.

## Public policy

`sasd::numerics::execution::ExecutionOptions` contains:

- `policy`: `automatic`, `sequential` or `parallel`;
- `maximum_threads`: an upper bound; zero means the detected hardware limit;
- `minimum_work_per_thread`: the automatic-mode granularity threshold.

The defaults are local values passed to supporting algorithms. There is no
mutable global pool size or tolerance-like process state.

## Suitable work

Likely parallel candidates include independent matrix rows/columns, batch
function evaluation, independent transforms and large element-wise operations.
Sequential recurrences such as one scalar Newton iteration do not become faster
merely by creating threads.

Reductions require extra care. Changing summation order can change rounding.
An algorithm must document this, use a stable formulation where appropriate and
test a mathematical quality metric rather than requiring accidental bitwise
identity.

## Oversubscription

Applications may already parallelize simulations, requests or datasets. They
can select `sequential` inside the library or cap `maximum_threads` to prevent
nested worker sets from competing for the same cores.

## Internal worker behavior

The CPP-M0 internal primitive partitions indices into stable contiguous chunks,
bounds workers by both hardware and work size, joins all workers and rethrows
the first worker exception on the calling thread. It does not expose a general
thread-pool API and therefore leaves room for a measured persistent-pool or
backend design later.

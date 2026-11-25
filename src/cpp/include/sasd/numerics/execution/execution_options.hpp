#pragma once

#include <cstddef>

namespace sasd::numerics::execution {

/**
 * @brief Selects how an algorithm may distribute independent work.
 *
 * Parallel execution is intentionally a policy rather than hidden mutable
 * global state. This keeps tests reproducible and allows applications to avoid
 * oversubscription when they already manage parallel work at a higher level.
 */
enum class ExecutionPolicy {
    /** Always execute the operation on the calling thread. */
    sequential,

    /** Use multiple workers only when the workload is large enough. */
    automatic,

    /** Use multiple workers whenever at least two work items exist. */
    parallel
};

/**
 * @brief Controls portable CPU parallelism in algorithms that support it.
 *
 * Not every numerical method can profitably or safely be parallelized. An
 * algorithm documents whether it honors these options. Scalar recurrences,
 * for example, normally remain sequential, while independent rows, samples or
 * transform batches can be partitioned into deterministic contiguous chunks.
 */
struct ExecutionOptions final {
    /** Default to adaptive use of the machine's available CPU cores. */
    ExecutionPolicy policy = ExecutionPolicy::automatic;

    /**
     * Maximum number of workers. Zero means use `hardware_concurrency()`.
     * The effective count is always bounded by the number of work items.
     */
    std::size_t maximum_threads = 0;

    /**
     * Minimum number of work items assigned per worker in automatic mode.
     * Values below one are treated as one.
     */
    std::size_t minimum_work_per_thread = 4096;
};

/**
 * @brief Resolves the worker count for a workload without creating threads.
 *
 * @param work_items Number of independent work items.
 * @param options Requested execution behavior.
 * @param detected_hardware_threads Optional hardware count for deterministic
 *        tests. Zero requests `std::thread::hardware_concurrency()`.
 * @return A count of at least one and at most `work_items` for non-empty work.
 */
[[nodiscard]] std::size_t
effective_thread_count(std::size_t work_items, const ExecutionOptions& options = {},
                       std::size_t detected_hardware_threads = 0) noexcept;

} // namespace sasd::numerics::execution

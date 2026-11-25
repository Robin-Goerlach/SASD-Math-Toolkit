#pragma once

#include "sasd/numerics/execution/execution_options.hpp"

#include <atomic>
#include <cstddef>
#include <exception>
#include <mutex>
#include <thread>
#include <utility>
#include <vector>

namespace sasd::numerics::detail {

/**
 * Executes independent indices in deterministic contiguous chunks.
 *
 * This is an internal implementation primitive, not part of the installed
 * public API. The caller must ensure that invoking `function(index)` for
 * different indices is thread-safe. Exceptions are captured in worker threads,
 * all workers are joined, and the first exception is rethrown on the caller.
 */
template <typename Function>
void parallel_for(const std::size_t work_items, const execution::ExecutionOptions& options,
                  Function&& function) {
    if (work_items == 0) {
        return;
    }

    const std::size_t worker_count = execution::effective_thread_count(work_items, options);

    if (worker_count == 1) {
        for (std::size_t index = 0; index < work_items; ++index) {
            function(index);
        }
        return;
    }

    std::atomic_bool failed{false};
    std::exception_ptr first_exception;
    std::mutex exception_mutex;

    const std::size_t base_chunk_size = work_items / worker_count;
    const std::size_t remainder = work_items % worker_count;

    {
        std::vector<std::jthread> workers;
        workers.reserve(worker_count);

        std::size_t begin = 0;
        for (std::size_t worker = 0; worker < worker_count; ++worker) {
            const std::size_t chunk_size = base_chunk_size + (worker < remainder ? 1 : 0);
            const std::size_t end = begin + chunk_size;

            workers.emplace_back([&, begin, end] {
                try {
                    for (std::size_t index = begin;
                         index < end && !failed.load(std::memory_order_relaxed); ++index) {
                        function(index);
                    }
                } catch (...) {
                    {
                        std::scoped_lock lock{exception_mutex};
                        if (first_exception == nullptr) {
                            first_exception = std::current_exception();
                        }
                    }
                    failed.store(true, std::memory_order_relaxed);
                }
            });

            begin = end;
        }
        // std::jthread joins during destruction before we inspect/rethrow the
        // captured exception outside this scope.
    }

    if (first_exception != nullptr) {
        std::rethrow_exception(first_exception);
    }
}

} // namespace sasd::numerics::detail

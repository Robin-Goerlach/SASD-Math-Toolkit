#include "sasd/numerics/execution/execution_options.hpp"

#include <algorithm>
#include <thread>

namespace sasd::numerics::execution {

std::size_t effective_thread_count(const std::size_t work_items, const ExecutionOptions& options,
                                   const std::size_t detected_hardware_threads) noexcept {
    if (work_items < 2 || options.policy == ExecutionPolicy::sequential) {
        return 1;
    }

    std::size_t hardware_threads = detected_hardware_threads;
    if (hardware_threads == 0) {
        hardware_threads = static_cast<std::size_t>(std::thread::hardware_concurrency());
    }

    // The standard permits hardware_concurrency() to return zero when the
    // platform cannot determine the value. Falling back to one is the only
    // portable and non-oversubscribing response.
    hardware_threads = std::max<std::size_t>(hardware_threads, 1);

    // maximum_threads is a cap, not a request to oversubscribe the hardware.
    // Applications that run several library calls concurrently can use a
    // smaller cap to reserve cores for their outer-level work.
    std::size_t available = hardware_threads;
    if (options.maximum_threads != 0) {
        available = std::min(available, options.maximum_threads);
    }
    available = std::min(available, work_items);

    if (options.policy == ExecutionPolicy::parallel) {
        return available;
    }

    const std::size_t minimum_work = std::max<std::size_t>(options.minimum_work_per_thread, 1);

    // Division plus remainder avoids the overflow possible in the common
    // `(work_items + minimum_work - 1) / minimum_work` formulation.
    const std::size_t useful_workers =
        (work_items / minimum_work) + (work_items % minimum_work != 0 ? 1 : 0);

    return std::max<std::size_t>(1, std::min(available, useful_workers));
}

} // namespace sasd::numerics::execution

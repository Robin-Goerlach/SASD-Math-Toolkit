#include "detail/parallel_for.hpp"
#include "sasd/numerics/common/iteration_status.hpp"
#include "sasd/numerics/common/iterative_result.hpp"
#include "sasd/numerics/common/numeric_constants.hpp"
#include "sasd/numerics/execution/execution_options.hpp"

#include <atomic>
#include <cmath>
#include <iostream>
#include <limits>
#include <mutex>
#include <set>
#include <stdexcept>
#include <string>
#include <string_view>
#include <thread>

namespace {

int failures = 0;
int checks = 0;

void expect(const bool condition, const std::string_view description) {
    ++checks;
    if (!condition) {
        std::cerr << "FAILED: " << description << '\n';
        ++failures;
    }
}

void test_status_vocabulary() {
    using sasd::numerics::common::IterationStatus;
    using sasd::numerics::common::to_string;

    expect(to_string(IterationStatus::converged) == "converged", "converged status name");
    expect(to_string(IterationStatus::maximum_iterations_reached) == "maximum_iterations_reached",
           "iteration-limit status name");
    expect(to_string(IterationStatus::numerical_breakdown) == "numerical_breakdown",
           "breakdown status name");
    expect(to_string(IterationStatus::not_bracketed) == "not_bracketed", "bracketing status name");

    const auto unknown = static_cast<IterationStatus>(255);
    expect(to_string(unknown) == "unknown", "unknown status remains safe to log");
}

void test_result_diagnostics_are_independent() {
    using sasd::numerics::common::IterationStatus;
    using sasd::numerics::common::IterativeResult;

    const IterativeResult<double> converged{1.0, 4, IterationStatus::converged, 1.0e-14};
    const IterativeResult<double> limited{0.99, 10, IterationStatus::maximum_iterations_reached,
                                          2.0e-5, std::string{"Iteration limit reached."}};
    const IterativeResult<double> breakdown{std::numeric_limits<double>::quiet_NaN(), 2,
                                            IterationStatus::numerical_breakdown};

    expect(converged.converged(), "converged status is reported");
    expect(converged.has_finite_residual(), "converged result has finite residual");
    expect(converged.iterations() == 4, "iteration count is retained");

    // A best-so-far approximation can have a useful residual even though the
    // documented stopping criterion was not satisfied.
    expect(!limited.converged(), "iteration-limited result is not converged");
    expect(limited.has_finite_residual(), "non-converged result can retain a residual");
    expect(std::abs(limited.residual() - 2.0e-5) < 1.0e-18, "residual value is retained");
    expect(limited.message().has_value(), "optional diagnostic message is retained");

    // A breakdown may occur before an algorithm can calculate a meaningful
    // problem-facing residual. NaN explicitly represents that absence.
    expect(!breakdown.converged(), "breakdown is not converged");
    expect(!breakdown.has_finite_residual(), "breakdown can lack a residual");
    expect(std::isnan(breakdown.residual()), "unavailable residual is NaN");
    expect(!breakdown.message().has_value(), "message remains optional");
}

void test_numeric_defaults() {
    namespace constants = sasd::numerics::common::numeric_constants;

    static_assert(constants::default_tolerance == 1.0e-12);
    static_assert(constants::nearly_zero == 1.0e-15);
    static_assert(constants::default_maximum_iterations == 100);

    expect(constants::nearly_zero < constants::default_tolerance,
           "guard threshold is smaller than the ordinary default tolerance");
}

void test_execution_policy() {
    using sasd::numerics::execution::effective_thread_count;
    using sasd::numerics::execution::ExecutionOptions;
    using sasd::numerics::execution::ExecutionPolicy;

    const ExecutionOptions sequential{ExecutionPolicy::sequential, 0, 1};
    expect(effective_thread_count(10'000, sequential, 16) == 1,
           "sequential policy uses one worker");

    const ExecutionOptions automatic{ExecutionPolicy::automatic, 8, 1'000};
    expect(effective_thread_count(500, automatic, 16) == 1,
           "small automatic workload stays serial");
    expect(effective_thread_count(4'500, automatic, 16) == 5,
           "automatic workload uses useful workers");

    const ExecutionOptions parallel{ExecutionPolicy::parallel, 4, 999'999};
    expect(effective_thread_count(10'000, parallel, 16) == 4,
           "parallel policy honors thread limit");
    expect(effective_thread_count(2, parallel, 16) == 2, "worker count never exceeds work items");

    const ExecutionOptions excessive_cap{ExecutionPolicy::parallel, 64, 1};
    expect(effective_thread_count(10'000, excessive_cap, 8) == 8,
           "thread cap does not oversubscribe hardware");
}

void test_parallel_for_uses_multiple_workers() {
    using sasd::numerics::execution::ExecutionOptions;
    using sasd::numerics::execution::ExecutionPolicy;

    constexpr std::size_t item_count = 10'000;
    const ExecutionOptions options{ExecutionPolicy::parallel, 4, 1};
    std::atomic<std::size_t> completed{0};
    std::set<std::thread::id> worker_ids;
    std::mutex worker_ids_mutex;

    sasd::numerics::detail::parallel_for(item_count, options, [&](const std::size_t) {
        completed.fetch_add(1, std::memory_order_relaxed);
        const std::scoped_lock lock{worker_ids_mutex};
        worker_ids.insert(std::this_thread::get_id());
    });

    expect(completed.load(std::memory_order_relaxed) == item_count,
           "parallel loop completes every item");
    expect(worker_ids.size() == 4, "forced parallel loop uses the requested workers");

    bool exception_rethrown = false;
    try {
        sasd::numerics::detail::parallel_for(32, options, [](const std::size_t index) {
            if (index == 7) {
                throw std::runtime_error{"worker failure"};
            }
        });
    } catch (const std::runtime_error&) {
        exception_rethrown = true;
    }
    expect(exception_rethrown, "worker exception is rethrown on the caller");
}

} // namespace

int main() {
    test_status_vocabulary();
    test_result_diagnostics_are_independent();
    test_numeric_defaults();
    test_execution_policy();
    test_parallel_for_uses_multiple_workers();

    if (failures != 0) {
        std::cerr << failures << " C++ foundation test(s) failed.\n";
        return 1;
    }

    std::cout << "All " << checks << " C++ foundation checks passed.\n";
    return 0;
}

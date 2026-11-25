#include "sasd/numerics/common/iteration_status.hpp"
#include "sasd/numerics/common/iterative_result.hpp"
#include "sasd/numerics/execution/execution_options.hpp"

#include <iostream>
#include <string>

int main() {
    using sasd::numerics::common::IterationStatus;
    using sasd::numerics::common::IterativeResult;
    using sasd::numerics::common::to_string;
    using sasd::numerics::execution::effective_thread_count;
    using sasd::numerics::execution::ExecutionOptions;

    // This value is illustrative: later numerical algorithms will construct
    // the same result envelope with their own documented residual definition.
    const IterativeResult<double> result{0.7390851332151607, 7, IterationStatus::converged, 2.2e-16,
                                         std::string{"Illustrative fixed-point result."}};

    std::cout << "SASD Math Toolkit C++ diagnostics sample\n"
              << "status: " << to_string(result.status()) << '\n'
              << "value: " << result.value() << '\n'
              << "iterations: " << result.iterations() << '\n'
              << "residual available: " << std::boolalpha << result.has_finite_residual() << '\n'
              << "automatic workers for 100000 independent items: "
              << effective_thread_count(100'000, ExecutionOptions{}) << '\n';

    return result.converged() ? 0 : 1;
}

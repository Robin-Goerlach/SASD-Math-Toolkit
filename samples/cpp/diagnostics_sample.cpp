#include "sasd/numerics/common/iteration_status.hpp"
#include "sasd/numerics/execution/execution_options.hpp"
#include "sasd/numerics/root_finding/root_solvers.hpp"

#include <cmath>
#include <iostream>

int main() {
    using sasd::numerics::common::to_string;
    using sasd::numerics::execution::effective_thread_count;
    using sasd::numerics::execution::ExecutionOptions;
    using sasd::numerics::root_finding::RootSolvers;

    // Bisection is intentionally used as the sample's robust reference path:
    // cos(x)-x changes sign between zero and one.
    const auto result =
        RootSolvers::bisection([](const double x) { return std::cos(x) - x; }, 0.0, 1.0);

    std::cout << "SASD Math Toolkit C++ diagnostics sample\n"
              << "status: " << to_string(result.status()) << '\n'
              << "root: " << result.root() << '\n'
              << "iterations: " << result.iterations() << '\n'
              << "root residual: " << result.residual() << '\n'
              << "automatic workers for 100000 independent items: "
              << effective_thread_count(100'000, ExecutionOptions{}) << '\n';

    return result.converged() ? 0 : 1;
}

#include "sasd/numerics/common/iteration_status.hpp"
#include "sasd/numerics/polynomials/polynomial.hpp"
#include "sasd/numerics/root_finding/polynomial_root_solvers.hpp"
#include "sasd/numerics/statistics/descriptive_statistics.hpp"

#include <array>
#include <iostream>

int main() {
    using sasd::numerics::common::IterationStatus;
    using sasd::numerics::common::to_string;

    constexpr std::array values{1.0, 2.0, 3.0};
    const double average = sasd::numerics::statistics::mean(values);
    const sasd::numerics::polynomials::Polynomial polynomial{1.0, 0.0, -1.0};
    const auto root =
        sasd::numerics::root_finding::PolynomialRootSolvers::laguerre(polynomial, {0.5, 0.0});

    std::cout << "Installed SASD Math Toolkit C++ status: " << to_string(IterationStatus::converged)
              << '\n';
    return average == 2.0 && root.converged() && root.residual() <= 1.0e-12 ? 0 : 1;
}

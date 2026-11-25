#pragma once

#include "sasd/numerics/root_finding/root_finding_options.hpp"
#include "sasd/numerics/root_finding/root_result.hpp"

#include <complex>
#include <functional>

namespace sasd::numerics::root_finding {

/** Root solvers operating directly in the complex plane. */
class ComplexRootSolvers final {
  public:
    /**
     * Finds a complex root with Muller's quadratic interpolation method.
     *
     * Complex square roots allow a sequence beginning on the real axis to
     * move naturally toward a non-real root.
     */
    [[nodiscard]] static ComplexRootResult
    muller(const std::function<std::complex<double>(std::complex<double>)>& function,
           std::complex<double> first_guess, std::complex<double> second_guess,
           std::complex<double> third_guess, const RootFindingOptions& options = {});

  private:
    ComplexRootSolvers() = delete;
};

} // namespace sasd::numerics::root_finding

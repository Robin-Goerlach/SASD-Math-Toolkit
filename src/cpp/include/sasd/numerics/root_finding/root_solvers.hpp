#pragma once

#include "sasd/numerics/root_finding/root_finding_options.hpp"
#include "sasd/numerics/root_finding/root_result.hpp"

#include <functional>

namespace sasd::numerics::root_finding {

/** Scalar real root-finding algorithms with explicit termination status. */
class RootSolvers final {
  public:
    /** Finds a root in a finite sign-changing interval. */
    [[nodiscard]] static RootResult bisection(const std::function<double(double)>& function,
                                              double left, double right,
                                              const RootFindingOptions& options = {});

    /** Applies Newton-Raphson from one finite initial estimate. */
    [[nodiscard]] static RootResult newton_raphson(const std::function<double(double)>& function,
                                                   const std::function<double(double)>& derivative,
                                                   double initial_guess,
                                                   const RootFindingOptions& options = {});

    /** Applies the derivative-free secant iteration from two finite estimates. */
    [[nodiscard]] static RootResult secant(const std::function<double(double)>& function,
                                           double first_guess, double second_guess,
                                           const RootFindingOptions& options = {});

  private:
    RootSolvers() = delete;
};

} // namespace sasd::numerics::root_finding

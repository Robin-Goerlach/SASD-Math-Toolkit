#pragma once

#include "sasd/numerics/execution/execution_options.hpp"

#include <span>
#include <vector>

namespace sasd::numerics::interpolation {

/** Immutable Newton-form interpolation polynomial for repeated evaluation. */
class NewtonInterpolant final {
  public:
    /** Copies finite distinct nodes and builds the Newton coefficients. */
    NewtonInterpolant(std::span<const double> abscissas, std::span<const double> ordinates);

    /** Original node order defining the Newton basis. */
    [[nodiscard]] std::span<const double> abscissas() const noexcept;

    /** Divided-difference coefficients in increasing Newton order. */
    [[nodiscard]] std::span<const double> coefficients() const noexcept;

    /** Evaluates the nested Newton form at one finite point. */
    [[nodiscard]] double evaluate(double point) const;

    /** Evaluates mutually independent points using the selected execution policy. */
    [[nodiscard]] std::vector<double>
    evaluate_many(std::span<const double> points,
                  const execution::ExecutionOptions& options = {}) const;

  private:
    std::vector<double> abscissas_;
    std::vector<double> coefficients_;
};

} // namespace sasd::numerics::interpolation

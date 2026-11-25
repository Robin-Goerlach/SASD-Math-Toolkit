#pragma once

#include "sasd/numerics/execution/execution_options.hpp"
#include "sasd/numerics/statistics/variance_mode.hpp"

#include <span>

namespace sasd::numerics::statistics {

/**
 * Calculates the arithmetic mean of finite values.
 *
 * Inputs are normalized by their largest magnitude before compensated
 * accumulation. This avoids the avoidable overflow of a naive sum for inputs
 * such as {max, max}.
 * Independent deterministic chunks may be evaluated on multiple CPU cores.
 *
 * @throws std::invalid_argument if values is empty or contains NaN/infinity.
 * @throws std::overflow_error if the final result cannot be represented.
 */
[[nodiscard]] double mean(std::span<const double> values,
                          const execution::ExecutionOptions& execution_options = {});

/**
 * Calculates the median of finite values without modifying the input.
 *
 * @throws std::invalid_argument if values is empty or contains NaN/infinity.
 */
[[nodiscard]] double median(std::span<const double> values);

/**
 * Calculates population or unbiased sample variance for finite values.
 *
 * A scaled sum-of-squares formulation prevents avoidable intermediate
 * overflow. If the mathematical variance itself is outside the range of a
 * double, the function reports that fact instead of returning infinity.
 * Independent deterministic chunks may be evaluated on multiple CPU cores.
 *
 * @throws std::invalid_argument for empty/non-finite input, or fewer than two
 *         values in sample mode.
 * @throws std::overflow_error if a finite variance cannot be represented.
 */
[[nodiscard]] double variance(std::span<const double> values,
                              VarianceMode mode = VarianceMode::population,
                              const execution::ExecutionOptions& execution_options = {});

} // namespace sasd::numerics::statistics

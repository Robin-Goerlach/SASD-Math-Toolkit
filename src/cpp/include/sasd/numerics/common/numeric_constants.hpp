#pragma once

#include <cstddef>

namespace sasd::numerics::common {

/**
 * @brief Shared defaults for the portable C++ reference implementation.
 *
 * These values are defaults and defensive guard thresholds, not universal
 * statements about numerical accuracy. Public algorithms should expose an
 * explicit tolerance whenever the problem scale requires caller control.
 */
namespace numeric_constants {

/** General-purpose default used only where the algorithm documents it. */
inline constexpr double default_tolerance = 1.0e-12;

/**
 * Absolute guard used for numerically negligible divisors or pivots.
 *
 * This is neither machine epsilon nor a general approximate-equality rule.
 */
inline constexpr double nearly_zero = 1.0e-15;

/** Default safety limit for algorithms using the shared iteration model. */
inline constexpr std::size_t default_maximum_iterations = 100;

} // namespace numeric_constants
} // namespace sasd::numerics::common

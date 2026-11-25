#pragma once

#include <cstdint>
#include <string_view>

namespace sasd::numerics::common {

/**
 * @brief Describes why an iterative numerical algorithm terminated.
 *
 * The status is deliberately independent from the returned value and from a
 * residual. A method can provide a useful best-so-far approximation without
 * satisfying its convergence criterion, and a numerical breakdown may occur
 * before a meaningful residual can be calculated.
 */
enum class IterationStatus : std::uint8_t {
    /** The documented convergence criterion was satisfied. */
    converged = 0,

    /** The configured iteration limit was reached without convergence. */
    maximum_iterations_reached = 1,

    /** The input did not satisfy the algorithm's public contract. */
    invalid_input = 2,

    /** Iteration stopped because a required numerical invariant was lost. */
    numerical_breakdown = 3,

    /** A valid interval did not bracket the sign change required by a solver. */
    not_bracketed = 4
};

/**
 * @brief Returns the stable diagnostic name of an iteration status.
 *
 * The returned text is intended for logs and diagnostics. It is not localized
 * and should not be used as a serialized replacement for the enum value.
 */
[[nodiscard]] std::string_view to_string(IterationStatus status) noexcept;

} // namespace sasd::numerics::common

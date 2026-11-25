#pragma once

#include "sasd/numerics/common/iteration_status.hpp"

#include <cmath>
#include <cstddef>
#include <limits>
#include <optional>
#include <string>
#include <utility>

namespace sasd::numerics::common {

/**
 * @brief Result envelope shared by iterative numerical algorithms.
 *
 * @tparam T Type of the calculated value or best available approximation.
 *
 * Status, value and residual answer different questions and therefore remain
 * separate. In particular, a finite residual does not imply convergence, and
 * residuals from unrelated problem classes are not directly comparable.
 */
template <typename T> class IterativeResult final {
  public:
    /**
     * @param value Calculated value or best available approximation.
     * @param iterations Number of completed iterations.
     * @param status Reason why iteration terminated.
     * @param residual Problem-specific residual, or NaN when unavailable.
     * @param message Optional diagnostic detail for non-routine termination.
     */
    explicit IterativeResult(T value, std::size_t iterations, IterationStatus status,
                             double residual = std::numeric_limits<double>::quiet_NaN(),
                             std::optional<std::string> message = std::nullopt)
        : value_(std::move(value)), iterations_(iterations), status_(status), residual_(residual),
          message_(std::move(message)) {}

    /** @return The calculated value or best available approximation. */
    [[nodiscard]] const T& value() const& noexcept { return value_; }

    /** @return The calculated value, moved from this result object. */
    [[nodiscard]] T&& value() && noexcept { return std::move(value_); }

    /** @return The number of completed iterations. */
    [[nodiscard]] std::size_t iterations() const noexcept { return iterations_; }

    /** @return The reason why iteration terminated. */
    [[nodiscard]] IterationStatus status() const noexcept { return status_; }

    /** @return The problem-specific residual, or NaN when unavailable. */
    [[nodiscard]] double residual() const noexcept { return residual_; }

    /** @return Optional diagnostic detail supplied by the algorithm. */
    [[nodiscard]] const std::optional<std::string>& message() const noexcept { return message_; }

    /** @return True only when the algorithm reported convergence. */
    [[nodiscard]] bool converged() const noexcept { return status_ == IterationStatus::converged; }

    /**
     * @return True when the residual contains a finite diagnostic value.
     *
     * This property describes residual availability only. It is intentionally
     * not another convergence flag.
     */
    [[nodiscard]] bool has_finite_residual() const noexcept { return std::isfinite(residual_); }

  private:
    T value_;
    std::size_t iterations_;
    IterationStatus status_;
    double residual_;
    std::optional<std::string> message_;
};

} // namespace sasd::numerics::common

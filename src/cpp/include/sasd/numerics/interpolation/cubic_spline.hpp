#pragma once

#include "sasd/numerics/execution/execution_options.hpp"

#include <cstddef>
#include <span>
#include <vector>

namespace sasd::numerics::interpolation {

class InterpolationAlgorithms;

/** Immutable piecewise cubic spline on a closed knot interval. */
class CubicSpline final {
  public:
    [[nodiscard]] double interval_start() const noexcept;
    [[nodiscard]] double interval_end() const noexcept;
    [[nodiscard]] std::size_t segment_count() const noexcept;
    [[nodiscard]] std::span<const double> knots() const noexcept;

    /** Evaluates the spline without extrapolating beyond its knots. */
    [[nodiscard]] double evaluate(double point) const;

    /** Evaluates the first derivative of the active cubic segment. */
    [[nodiscard]] double first_derivative(double point) const;

    /** Evaluates the second derivative of the active cubic segment. */
    [[nodiscard]] double second_derivative(double point) const;

    /**
     * Evaluates independent points, honoring the caller's execution policy.
     * The automatic threshold is intentionally caller-adjustable because the
     * useful crossover depends on hardware, workload and surrounding code.
     */
    [[nodiscard]] std::vector<double>
    evaluate_many(std::span<const double> points,
                  const execution::ExecutionOptions& options = {}) const;

    /** Evaluates first derivatives at independent points in input order. */
    [[nodiscard]] std::vector<double>
    first_derivative_many(std::span<const double> points,
                          const execution::ExecutionOptions& options = {}) const;

    /** Evaluates second derivatives at independent points in input order. */
    [[nodiscard]] std::vector<double>
    second_derivative_many(std::span<const double> points,
                           const execution::ExecutionOptions& options = {}) const;

  private:
    friend class InterpolationAlgorithms;

    CubicSpline(std::vector<double> knots, std::vector<double> constant, std::vector<double> linear,
                std::vector<double> quadratic, std::vector<double> cubic);

    [[nodiscard]] std::size_t find_segment(double point) const;
    void require_inside_interval(double point) const;

    std::vector<double> knots_;
    std::vector<double> constant_;
    std::vector<double> linear_;
    std::vector<double> quadratic_;
    std::vector<double> cubic_;
};

} // namespace sasd::numerics::interpolation

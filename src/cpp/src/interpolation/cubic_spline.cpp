#include "sasd/numerics/interpolation/cubic_spline.hpp"

#include "detail/parallel_for.hpp"
#include "interpolation/interpolation_helpers.hpp"

#include <algorithm>
#include <stdexcept>
#include <utility>

namespace sasd::numerics::interpolation {

CubicSpline::CubicSpline(std::vector<double> knots, std::vector<double> constant,
                         std::vector<double> linear, std::vector<double> quadratic,
                         std::vector<double> cubic)
    : knots_(std::move(knots)), constant_(std::move(constant)), linear_(std::move(linear)),
      quadratic_(std::move(quadratic)), cubic_(std::move(cubic)) {}

double CubicSpline::interval_start() const noexcept { return knots_.front(); }

double CubicSpline::interval_end() const noexcept { return knots_.back(); }

std::size_t CubicSpline::segment_count() const noexcept { return knots_.size() - 1; }

std::span<const double> CubicSpline::knots() const noexcept { return knots_; }

void CubicSpline::require_inside_interval(const double point) const {
    if (!std::isfinite(point) || point < interval_start() || point > interval_end()) {
        throw std::out_of_range{"Spline evaluation requires a finite point inside its interval."};
    }
}

std::size_t CubicSpline::find_segment(const double point) const {
    if (point == interval_end()) {
        return segment_count() - 1;
    }
    const auto upper = std::upper_bound(knots_.begin(), knots_.end(), point);
    return static_cast<std::size_t>(std::distance(knots_.begin(), upper) - 1);
}

double CubicSpline::evaluate(const double point) const {
    require_inside_interval(point);
    const std::size_t segment = find_segment(point);
    const double offset = point - knots_[segment];
    // Nested evaluation avoids repeated powers and keeps the segment formula readable.
    const double value =
        constant_[segment] +
        offset * (linear_[segment] + offset * (quadratic_[segment] + offset * cubic_[segment]));
    detail::require_finite_computation(value, "Spline evaluation produced a non-finite value.");
    return value;
}

double CubicSpline::first_derivative(const double point) const {
    require_inside_interval(point);
    const std::size_t segment = find_segment(point);
    const double offset = point - knots_[segment];
    const double value =
        linear_[segment] + offset * (2.0 * quadratic_[segment] + offset * 3.0 * cubic_[segment]);
    detail::require_finite_computation(value,
                                       "Spline first derivative produced a non-finite value.");
    return value;
}

double CubicSpline::second_derivative(const double point) const {
    require_inside_interval(point);
    const std::size_t segment = find_segment(point);
    const double offset = point - knots_[segment];
    const double value = (2.0 * quadratic_[segment]) + (6.0 * cubic_[segment] * offset);
    detail::require_finite_computation(value,
                                       "Spline second derivative produced a non-finite value.");
    return value;
}

std::vector<double> CubicSpline::evaluate_many(const std::span<const double> points,
                                               const execution::ExecutionOptions& options) const {
    std::vector<double> values(points.size());
    sasd::numerics::detail::parallel_for(points.size(), options, [&](const std::size_t index) {
        values[index] = evaluate(points[index]);
    });
    return values;
}

std::vector<double>
CubicSpline::first_derivative_many(const std::span<const double> points,
                                   const execution::ExecutionOptions& options) const {
    std::vector<double> values(points.size());
    sasd::numerics::detail::parallel_for(points.size(), options, [&](const std::size_t index) {
        values[index] = first_derivative(points[index]);
    });
    return values;
}

std::vector<double>
CubicSpline::second_derivative_many(const std::span<const double> points,
                                    const execution::ExecutionOptions& options) const {
    std::vector<double> values(points.size());
    sasd::numerics::detail::parallel_for(points.size(), options, [&](const std::size_t index) {
        values[index] = second_derivative(points[index]);
    });
    return values;
}

} // namespace sasd::numerics::interpolation

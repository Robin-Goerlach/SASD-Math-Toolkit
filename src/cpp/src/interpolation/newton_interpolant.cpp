#include "sasd/numerics/interpolation/newton_interpolant.hpp"

#include "detail/parallel_for.hpp"
#include "interpolation/interpolation_helpers.hpp"
#include "sasd/numerics/interpolation/interpolation_algorithms.hpp"

#include <cstddef>

namespace sasd::numerics::interpolation {

NewtonInterpolant::NewtonInterpolant(const std::span<const double> abscissas,
                                     const std::span<const double> ordinates)
    : abscissas_(abscissas.begin(), abscissas.end()),
      coefficients_(
          InterpolationAlgorithms::newton_divided_difference_coefficients(abscissas, ordinates)) {}

std::span<const double> NewtonInterpolant::abscissas() const noexcept { return abscissas_; }

std::span<const double> NewtonInterpolant::coefficients() const noexcept { return coefficients_; }

double NewtonInterpolant::evaluate(const double point) const {
    detail::require_finite(point, "Newton interpolation point must be finite.");

    double result = coefficients_.back();
    for (std::size_t index = coefficients_.size() - 1; index-- > 0;) {
        const double offset = detail::finite_difference(
            point, abscissas_[index], "Newton interpolation offset is not representable.");
        result = coefficients_[index] + (offset * result);
        detail::require_finite_computation(result,
                                           "Newton interpolation produced a non-finite value.");
    }
    return result;
}

std::vector<double>
NewtonInterpolant::evaluate_many(const std::span<const double> points,
                                 const execution::ExecutionOptions& options) const {
    std::vector<double> values(points.size());
    sasd::numerics::detail::parallel_for(points.size(), options, [&](const std::size_t index) {
        values[index] = evaluate(points[index]);
    });
    return values;
}

} // namespace sasd::numerics::interpolation

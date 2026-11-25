#include "sasd/numerics/interpolation/interpolation_algorithms.hpp"

#include "interpolation/interpolation_helpers.hpp"
#include "sasd/numerics/common/numeric_constants.hpp"
#include "sasd/numerics/interpolation/newton_interpolant.hpp"

#include <cmath>
#include <cstddef>
#include <stdexcept>
#include <utility>
#include <vector>

namespace sasd::numerics::interpolation {
namespace {

void require_usable_pivot(const double pivot) {
    if (!std::isfinite(pivot) || std::abs(pivot) <= common::numeric_constants::nearly_zero) {
        throw std::overflow_error{
            "Cubic-spline construction encountered a numerically singular pivot."};
    }
}

} // namespace

CubicSpline InterpolationAlgorithms::build_cubic_spline(const std::span<const double> abscissas,
                                                        const std::span<const double> ordinates,
                                                        const bool clamped,
                                                        const double left_derivative,
                                                        const double right_derivative) {
    detail::validate_points(abscissas, ordinates, 2);
    detail::require_strictly_increasing(abscissas);

    const std::size_t segments = abscissas.size() - 1;
    std::vector<double> a{ordinates.begin(), ordinates.end()};
    std::vector<double> b(segments);
    std::vector<double> c(segments + 1);
    std::vector<double> d(segments);
    std::vector<double> h(segments);
    std::vector<double> alpha(segments + 1);
    std::vector<double> diagonal(segments + 1);
    std::vector<double> multiplier(segments + 1);
    std::vector<double> solution(segments + 1);

    for (std::size_t index = 0; index < segments; ++index) {
        h[index] = detail::finite_difference(abscissas[index + 1], abscissas[index],
                                             "Spline knot spacing is not representable.");
        if (h[index] <= 0.0) {
            throw std::overflow_error{"Spline knot spacing must remain positive."};
        }
    }

    // The interior right-hand side encodes the change between neighboring
    // secant slopes. Natural and clamped boundaries only change the endpoints.
    for (std::size_t index = 1; index < segments; ++index) {
        alpha[index] = (3.0 / h[index] * (a[index + 1] - a[index])) -
                       (3.0 / h[index - 1] * (a[index] - a[index - 1]));
        detail::require_finite_computation(alpha[index],
                                           "Spline right-hand side produced a non-finite value.");
    }

    if (clamped) {
        alpha[0] = (3.0 * (a[1] - a[0]) / h[0]) - (3.0 * left_derivative);
        alpha[segments] =
            (3.0 * right_derivative) - (3.0 * (a[segments] - a[segments - 1]) / h[segments - 1]);
        detail::require_finite_computation(
            alpha[0], "Left spline boundary equation produced a non-finite value.");
        detail::require_finite_computation(
            alpha[segments], "Right spline boundary equation produced a non-finite value.");

        diagonal[0] = 2.0 * h[0];
        require_usable_pivot(diagonal[0]);
        multiplier[0] = 0.5;
        solution[0] = alpha[0] / diagonal[0];
    } else {
        // Natural boundary condition: the endpoint curvature is zero.
        diagonal[0] = 1.0;
    }

    // This is the forward elimination of the tridiagonal spline system. Each
    // row depends on the previous row, so splitting this recurrence into
    // workers would add synchronization rather than independent work.
    for (std::size_t index = 1; index < segments; ++index) {
        diagonal[index] = (2.0 * (abscissas[index + 1] - abscissas[index - 1])) -
                          (h[index - 1] * multiplier[index - 1]);
        require_usable_pivot(diagonal[index]);
        multiplier[index] = h[index] / diagonal[index];
        solution[index] = (alpha[index] - (h[index - 1] * solution[index - 1])) / diagonal[index];
        detail::require_finite_computation(multiplier[index],
                                           "Spline elimination multiplier is not representable.");
        detail::require_finite_computation(solution[index],
                                           "Spline elimination result is not representable.");
    }

    if (clamped) {
        diagonal[segments] = h[segments - 1] * (2.0 - multiplier[segments - 1]);
        require_usable_pivot(diagonal[segments]);
        solution[segments] =
            (alpha[segments] - (h[segments - 1] * solution[segments - 1])) / diagonal[segments];
        detail::require_finite_computation(solution[segments],
                                           "Right spline boundary result is not representable.");
        c[segments] = solution[segments];
    }

    // Back substitution yields the quadratic coefficients, from which the
    // linear and cubic coefficients follow directly for every segment.
    for (std::size_t index = segments; index-- > 0;) {
        c[index] = solution[index] - (multiplier[index] * c[index + 1]);
        b[index] = ((a[index + 1] - a[index]) / h[index]) -
                   (h[index] * (c[index + 1] + (2.0 * c[index])) / 3.0);
        d[index] = (c[index + 1] - c[index]) / (3.0 * h[index]);
        detail::require_finite_computation(c[index], "Spline coefficient is not representable.");
        detail::require_finite_computation(b[index], "Spline coefficient is not representable.");
        detail::require_finite_computation(d[index], "Spline coefficient is not representable.");
    }

    a.resize(segments);
    c.resize(segments);
    return {std::vector<double>{abscissas.begin(), abscissas.end()}, std::move(a), std::move(b),
            std::move(c), std::move(d)};
}

double InterpolationAlgorithms::linear(const double first_x, const double first_y,
                                       const double second_x, const double second_y,
                                       const double point) {
    detail::require_finite(first_x, "First linear-interpolation abscissa must be finite.");
    detail::require_finite(first_y, "First linear-interpolation ordinate must be finite.");
    detail::require_finite(second_x, "Second linear-interpolation abscissa must be finite.");
    detail::require_finite(second_y, "Second linear-interpolation ordinate must be finite.");
    detail::require_finite(point, "Linear-interpolation point must be finite.");
    if (first_x == second_x) {
        throw std::invalid_argument{"Linear interpolation requires distinct abscissas."};
    }

    const double width = detail::finite_difference(
        second_x, first_x, "Linear-interpolation interval is not representable.");
    const double offset = detail::finite_difference(
        point, first_x, "Linear-interpolation offset is not representable.");
    const double value = std::lerp(first_y, second_y, offset / width);
    detail::require_finite_computation(value, "Linear interpolation produced a non-finite value.");
    return value;
}

double InterpolationAlgorithms::lagrange(const std::span<const double> abscissas,
                                         const std::span<const double> ordinates,
                                         const double point) {
    detail::validate_points(abscissas, ordinates, 1);
    detail::require_distinct(abscissas);
    detail::require_finite(point, "Lagrange interpolation point must be finite.");

    double result = 0.0;
    for (std::size_t basis_index = 0; basis_index < abscissas.size(); ++basis_index) {
        double basis = 1.0;
        for (std::size_t index = 0; index < abscissas.size(); ++index) {
            if (index == basis_index) {
                continue;
            }
            const double numerator = detail::finite_difference(
                point, abscissas[index], "Lagrange numerator is not representable.");
            const double denominator =
                detail::finite_difference(abscissas[basis_index], abscissas[index],
                                          "Lagrange denominator is not representable.");
            basis *= numerator / denominator;
            detail::require_finite_computation(basis,
                                               "Lagrange basis produced a non-finite value.");
        }
        result += ordinates[basis_index] * basis;
        detail::require_finite_computation(result,
                                           "Lagrange interpolation produced a non-finite value.");
    }
    return result;
}

std::vector<double> InterpolationAlgorithms::newton_divided_difference_coefficients(
    const std::span<const double> abscissas, const std::span<const double> ordinates) {
    detail::validate_points(abscissas, ordinates, 1);
    detail::require_distinct(abscissas);

    std::vector<double> coefficients{ordinates.begin(), ordinates.end()};
    for (std::size_t order = 1; order < coefficients.size(); ++order) {
        // Descending indices preserve the preceding divided-difference order
        // until it has been consumed by the current order.
        for (std::size_t index = coefficients.size(); index-- > order;) {
            const double numerator = coefficients[index] - coefficients[index - 1];
            detail::require_finite_computation(
                numerator, "Newton divided-difference numerator is not representable.");
            const double denominator = detail::finite_difference(
                abscissas[index], abscissas[index - order],
                "Newton divided-difference denominator is not representable.");
            coefficients[index] = numerator / denominator;
            detail::require_finite_computation(coefficients[index],
                                               "Newton coefficient is not representable.");
        }
    }
    return coefficients;
}

double InterpolationAlgorithms::newton_divided_difference(const std::span<const double> abscissas,
                                                          const std::span<const double> ordinates,
                                                          const double point) {
    return NewtonInterpolant{abscissas, ordinates}.evaluate(point);
}

CubicSpline InterpolationAlgorithms::natural_cubic_spline(const std::span<const double> abscissas,
                                                          const std::span<const double> ordinates) {
    return build_cubic_spline(abscissas, ordinates, false, 0.0, 0.0);
}

CubicSpline InterpolationAlgorithms::clamped_cubic_spline(const std::span<const double> abscissas,
                                                          const std::span<const double> ordinates,
                                                          const double left_derivative,
                                                          const double right_derivative) {
    detail::require_finite(left_derivative, "Left spline derivative must be finite.");
    detail::require_finite(right_derivative, "Right spline derivative must be finite.");
    return build_cubic_spline(abscissas, ordinates, true, left_derivative, right_derivative);
}

} // namespace sasd::numerics::interpolation

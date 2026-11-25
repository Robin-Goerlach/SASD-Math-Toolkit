#include "sasd/numerics/root_finding/polynomial_root_solvers.hpp"

#include "root_finding/root_helpers.hpp"

#include <algorithm>
#include <cmath>
#include <complex>
#include <numbers>
#include <optional>
#include <stdexcept>
#include <string>
#include <vector>

namespace sasd::numerics::root_finding {
namespace {

constexpr std::size_t deflation_fallback_starts = 12;

void require_rootable(const polynomials::Polynomial& polynomial) {
    if (polynomial.is_zero() || polynomial.degree() < 1) {
        throw std::invalid_argument{"Root finding requires a non-constant polynomial."};
    }
}

[[nodiscard]] std::complex<double> snap_tiny_imaginary_part(const std::complex<double> root,
                                                            const double tolerance) noexcept {
    const double scale = std::max(1.0, std::abs(root.real()));
    return std::abs(root.imag()) <= 100.0 * tolerance * scale
               ? std::complex<double>{root.real(), 0.0}
               : root;
}

[[nodiscard]] double cauchy_root_bound(const polynomials::Polynomial& polynomial) {
    const double leading_magnitude = std::abs(polynomial.leading_coefficient());
    double maximum_ratio = 0.0;
    const auto coefficients = polynomial.coefficients();
    for (std::size_t index = 1; index < coefficients.size(); ++index) {
        const double ratio = std::abs(coefficients[index]) / leading_magnitude;
        if (std::isfinite(ratio)) {
            maximum_ratio = std::max(maximum_ratio, ratio);
        }
    }
    const double radius = 1.0 + maximum_ratio;
    return std::isfinite(radius) && radius > 0.0 ? radius : 1.0;
}

[[nodiscard]] std::vector<std::complex<double>>
build_initial_guesses(const polynomials::Polynomial& polynomial, const std::size_t root_ordinal) {
    std::vector<std::complex<double>> guesses;
    guesses.reserve(deflation_fallback_starts + 1);
    guesses.emplace_back(0.0, 0.0);

    const double radius = cauchy_root_bound(polynomial);
    for (std::size_t attempt = 0; attempt < deflation_fallback_starts; ++attempt) {
        const double turns =
            (static_cast<double>(attempt) + (0.37 * static_cast<double>(root_ordinal))) /
            static_cast<double>(deflation_fallback_starts);
        guesses.push_back(std::polar(radius, 2.0 * std::numbers::pi * turns));
    }
    return guesses;
}

[[nodiscard]] double maximum_residual(const polynomials::Polynomial& polynomial,
                                      const std::vector<std::complex<double>>& roots) {
    double maximum = 0.0;
    for (const auto root : roots) {
        maximum = std::max(maximum, std::abs(polynomial.evaluate(root)));
    }
    return maximum;
}

} // namespace

ComplexRootResult PolynomialRootSolvers::newton_horner(const polynomials::Polynomial& polynomial,
                                                       const std::complex<double> initial_guess,
                                                       const RootFindingOptions& options) {
    require_rootable(polynomial);
    options.validate();
    if (!detail::is_finite(initial_guess)) {
        throw std::invalid_argument{"Newton-Horner initial guess must be finite."};
    }

    auto x = initial_guess;
    auto evaluation = polynomial.evaluate_with_derivatives(x);
    if (std::abs(evaluation.value) <= options.tolerance) {
        return {x, evaluation.value, 0, common::IterationStatus::converged};
    }

    for (std::size_t completed = 0; completed < options.maximum_iterations; ++completed) {
        const std::size_t iteration = completed + 1;
        if (detail::is_nearly_zero(evaluation.first_derivative)) {
            return {x, evaluation.value, iteration - 1,
                    common::IterationStatus::numerical_breakdown,
                    std::string{
                        "Polynomial derivative is too close to zero for a stable Newton step."}};
        }

        const auto next = x - (evaluation.value / evaluation.first_derivative);
        detail::require_finite_complex(next, "Newton-Horner produced a non-finite iterate.");
        const auto next_evaluation = polynomial.evaluate_with_derivatives(next);
        if (detail::has_converged(next, x, next_evaluation.value, options.tolerance)) {
            return {next, next_evaluation.value, iteration, common::IterationStatus::converged};
        }
        x = next;
        evaluation = next_evaluation;
    }

    return {x, evaluation.value, options.maximum_iterations,
            common::IterationStatus::maximum_iterations_reached,
            std::string{"Maximum number of iterations reached."}};
}

ComplexRootResult PolynomialRootSolvers::laguerre(const polynomials::Polynomial& polynomial,
                                                  const std::complex<double> initial_guess,
                                                  const RootFindingOptions& options) {
    require_rootable(polynomial);
    options.validate();
    if (!detail::is_finite(initial_guess)) {
        throw std::invalid_argument{"Laguerre initial guess must be finite."};
    }

    if (polynomial.degree() == 1) {
        const auto coefficients = polynomial.coefficients();
        const auto root = -coefficients[1] / coefficients[0];
        detail::require_finite_complex(root, "Linear polynomial root is not representable.");
        const auto value = polynomial.evaluate(root);
        return {root, value, 0, common::IterationStatus::converged};
    }

    auto x = initial_guess;
    auto evaluation = polynomial.evaluate_with_derivatives(x);
    const double degree = static_cast<double>(polynomial.degree());
    if (std::abs(evaluation.value) <= options.tolerance) {
        return {x, evaluation.value, 0, common::IterationStatus::converged};
    }

    for (std::size_t completed = 0; completed < options.maximum_iterations; ++completed) {
        const std::size_t iteration = completed + 1;
        const auto logarithmic_derivative = evaluation.first_derivative / evaluation.value;
        const auto curvature = (logarithmic_derivative * logarithmic_derivative) -
                               (evaluation.second_derivative / evaluation.value);
        const auto radical =
            std::sqrt((degree - 1.0) *
                      ((degree * curvature) - (logarithmic_derivative * logarithmic_derivative)));
        detail::require_finite_complex(radical, "Laguerre radical is not representable.");

        const auto plus = logarithmic_derivative + radical;
        const auto minus = logarithmic_derivative - radical;
        const auto denominator = std::abs(plus) >= std::abs(minus) ? plus : minus;
        if (detail::is_nearly_zero(denominator)) {
            return {x, evaluation.value, iteration - 1,
                    common::IterationStatus::numerical_breakdown,
                    std::string{"Laguerre denominator became too small for a stable step."}};
        }

        const auto next = x - (degree / denominator);
        detail::require_finite_complex(next, "Laguerre produced a non-finite iterate.");
        const auto next_evaluation = polynomial.evaluate_with_derivatives(next);
        if (detail::has_converged(next, x, next_evaluation.value, options.tolerance)) {
            return {next, next_evaluation.value, iteration, common::IterationStatus::converged};
        }
        x = next;
        evaluation = next_evaluation;
    }

    return {x, evaluation.value, options.maximum_iterations,
            common::IterationStatus::maximum_iterations_reached,
            std::string{"Maximum number of iterations reached."}};
}

PolynomialRootsResult
PolynomialRootSolvers::find_all_roots_laguerre(const polynomials::Polynomial& polynomial,
                                               const RootFindingOptions& options) {
    require_rootable(polynomial);
    options.validate();

    auto working = polynomial;
    std::vector<std::complex<double>> roots;
    roots.reserve(polynomial.degree());
    std::size_t total_iterations = 0;
    std::size_t root_ordinal = 0;

    while (working.degree() > 0) {
        std::optional<ComplexRootResult> root_result;
        for (const auto initial_guess : build_initial_guesses(working, root_ordinal)) {
            root_result = laguerre(working, initial_guess, options);
            total_iterations += root_result->iterations();
            if (root_result->converged()) {
                break;
            }
        }

        if (!root_result.has_value() || !root_result->converged()) {
            const double residual = maximum_residual(polynomial, roots);
            return {std::move(roots), total_iterations,
                    root_result.has_value() ? root_result->status()
                                            : common::IterationStatus::numerical_breakdown,
                    residual,
                    root_result.has_value()
                        ? root_result->message()
                        : std::optional<std::string>{"No usable Laguerre start point was found."}};
        }

        const auto root = snap_tiny_imaginary_part(root_result->root(), options.tolerance);
        const auto deflation = working.deflate(root);
        roots.push_back(root);
        working = deflation.quotient;
        ++root_ordinal;
    }

    // Deflation accumulates rounding error. Polish every root against the
    // original polynomial before reporting the final residual.
    for (auto& root : roots) {
        const auto polished = laguerre(polynomial, root, options);
        total_iterations += polished.iterations();
        if (polished.converged()) {
            root = snap_tiny_imaginary_part(polished.root(), options.tolerance);
        }
    }

    std::sort(roots.begin(), roots.end(), [](const auto left, const auto right) {
        return left.real() != right.real() ? left.real() < right.real()
                                           : left.imag() < right.imag();
    });
    const double residual = maximum_residual(polynomial, roots);
    return {std::move(roots), total_iterations, common::IterationStatus::converged, residual};
}

} // namespace sasd::numerics::root_finding

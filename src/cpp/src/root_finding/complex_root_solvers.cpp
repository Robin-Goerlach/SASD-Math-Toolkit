#include "sasd/numerics/root_finding/complex_root_solvers.hpp"

#include "root_finding/root_helpers.hpp"

#include <complex>
#include <stdexcept>
#include <string>
#include <utility>

namespace sasd::numerics::root_finding {
namespace {

using ComplexFunction = std::function<std::complex<double>(std::complex<double>)>;

void require_function(const ComplexFunction& function) {
    if (!function) {
        throw std::invalid_argument{"Muller iteration requires a function."};
    }
}

[[nodiscard]] std::complex<double> evaluate(const ComplexFunction& function,
                                            const std::complex<double> x) {
    const auto value = function(x);
    detail::require_finite_callback(value);
    return value;
}

[[nodiscard]] ComplexRootResult breakdown(const std::complex<double> root,
                                          const std::complex<double> function_value,
                                          const std::size_t iterations, std::string message) {
    return {root, function_value, iterations, common::IterationStatus::numerical_breakdown,
            std::move(message)};
}

} // namespace

ComplexRootResult ComplexRootSolvers::muller(const ComplexFunction& function,
                                             const std::complex<double> first_guess,
                                             const std::complex<double> second_guess,
                                             const std::complex<double> third_guess,
                                             const RootFindingOptions& options) {
    require_function(function);
    options.validate();
    if (!detail::is_finite(first_guess) || !detail::is_finite(second_guess) ||
        !detail::is_finite(third_guess)) {
        throw std::invalid_argument{"Muller starting points must be finite."};
    }

    auto x0 = first_guess;
    auto x1 = second_guess;
    auto x2 = third_guess;
    auto f0 = evaluate(function, x0);
    auto f1 = evaluate(function, x1);
    auto f2 = evaluate(function, x2);

    if (std::abs(f0) <= options.tolerance) {
        return {x0, f0, 0, common::IterationStatus::converged};
    }
    if (std::abs(f1) <= options.tolerance) {
        return {x1, f1, 0, common::IterationStatus::converged};
    }
    if (std::abs(f2) <= options.tolerance) {
        return {x2, f2, 0, common::IterationStatus::converged};
    }

    for (std::size_t completed = 0; completed < options.maximum_iterations; ++completed) {
        const std::size_t iteration = completed + 1;
        const auto h1 = x1 - x0;
        const auto h2 = x2 - x1;
        detail::require_finite_complex(h1, "Muller point difference is not representable.");
        detail::require_finite_complex(h2, "Muller point difference is not representable.");
        if (detail::is_nearly_zero(h1) || detail::is_nearly_zero(h2) ||
            detail::is_nearly_zero(h1 + h2)) {
            return breakdown(x2, f2, iteration - 1,
                             "Muller iteration requires three sufficiently distinct points.");
        }

        const auto delta1 = (f1 - f0) / h1;
        const auto delta2 = (f2 - f1) / h2;
        const auto quadratic = (delta2 - delta1) / (h1 + h2);
        const auto linear = delta2 + (h2 * quadratic);
        const auto discriminant = std::sqrt((linear * linear) - (4.0 * f2 * quadratic));
        detail::require_finite_complex(discriminant, "Muller discriminant is not representable.");

        // The larger denominator reduces cancellation in the quadratic step.
        const auto plus = linear + discriminant;
        const auto minus = linear - discriminant;
        const auto denominator = std::abs(plus) >= std::abs(minus) ? plus : minus;
        if (detail::is_nearly_zero(denominator)) {
            return breakdown(x2, f2, iteration - 1,
                             "Muller quadratic step became numerically singular.");
        }

        const auto step = (-2.0 * f2) / denominator;
        const auto next = x2 + step;
        detail::require_finite_complex(next, "Muller iteration produced a non-finite iterate.");
        const auto next_value = evaluate(function, next);
        if (detail::has_converged(next, x2, next_value, options.tolerance)) {
            return {next, next_value, iteration, common::IterationStatus::converged};
        }

        x0 = x1;
        f0 = f1;
        x1 = x2;
        f1 = f2;
        x2 = next;
        f2 = next_value;
    }

    return {x2, f2, options.maximum_iterations, common::IterationStatus::maximum_iterations_reached,
            std::string{"Maximum number of iterations reached."}};
}

} // namespace sasd::numerics::root_finding

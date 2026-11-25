#include "sasd/numerics/root_finding/root_solvers.hpp"

#include "root_finding/root_helpers.hpp"
#include "sasd/numerics/common/numeric_constants.hpp"

#include <cmath>
#include <limits>
#include <numeric>
#include <stdexcept>
#include <string>

namespace sasd::numerics::root_finding {
namespace {

void require_function(const std::function<double(double)>& function, const char* description) {
    if (!function) {
        throw std::invalid_argument{description};
    }
}

[[nodiscard]] double evaluate(const std::function<double(double)>& function, const double x) {
    const double value = function(x);
    detail::require_finite_callback(value);
    return value;
}

} // namespace

RootResult RootSolvers::bisection(const std::function<double(double)>& function, double left,
                                  double right, const RootFindingOptions& options) {
    require_function(function, "Bisection requires a function.");
    options.validate();
    detail::require_finite_scalar(left, "The left endpoint must be finite.");
    detail::require_finite_scalar(right, "The right endpoint must be finite.");
    if (left >= right) {
        throw std::invalid_argument{"The left endpoint must be smaller than the right endpoint."};
    }

    double f_left = evaluate(function, left);
    const double f_right = evaluate(function, right);
    if (std::abs(f_left) <= options.tolerance) {
        return {left, f_left, 0, common::IterationStatus::converged};
    }
    if (std::abs(f_right) <= options.tolerance) {
        return {right, f_right, 0, common::IterationStatus::converged};
    }
    if (std::signbit(f_left) == std::signbit(f_right)) {
        const double unavailable = std::numeric_limits<double>::quiet_NaN();
        return {unavailable, unavailable, 0, common::IterationStatus::not_bracketed,
                std::string{"Function values at the interval endpoints must have opposite signs."}};
    }

    double previous = left;
    double midpoint = left;
    double f_midpoint = f_left;
    for (std::size_t completed = 0; completed < options.maximum_iterations; ++completed) {
        const std::size_t iteration = completed + 1;
        // std::midpoint avoids overflow for wide finite intervals.
        midpoint = std::midpoint(left, right);
        f_midpoint = evaluate(function, midpoint);
        if (detail::has_converged(midpoint, previous, f_midpoint, options.tolerance)) {
            return {midpoint, f_midpoint, iteration, common::IterationStatus::converged};
        }

        if (std::signbit(f_left) != std::signbit(f_midpoint)) {
            right = midpoint;
        } else {
            left = midpoint;
            f_left = f_midpoint;
        }
        previous = midpoint;
    }

    return {midpoint, f_midpoint, options.maximum_iterations,
            common::IterationStatus::maximum_iterations_reached,
            std::string{"Maximum number of iterations reached."}};
}

RootResult RootSolvers::newton_raphson(const std::function<double(double)>& function,
                                       const std::function<double(double)>& derivative,
                                       const double initial_guess,
                                       const RootFindingOptions& options) {
    require_function(function, "Newton-Raphson requires a function.");
    require_function(derivative, "Newton-Raphson requires a derivative.");
    options.validate();
    detail::require_finite_scalar(initial_guess, "The initial guess must be finite.");

    double x = initial_guess;
    double function_value = evaluate(function, x);
    if (std::abs(function_value) <= options.tolerance) {
        return {x, function_value, 0, common::IterationStatus::converged};
    }

    for (std::size_t completed = 0; completed < options.maximum_iterations; ++completed) {
        const std::size_t iteration = completed + 1;
        const double derivative_value = evaluate(derivative, x);
        if (std::abs(derivative_value) <= common::numeric_constants::nearly_zero) {
            return {x, function_value, iteration - 1, common::IterationStatus::numerical_breakdown,
                    std::string{"Derivative is too close to zero for a stable Newton step."}};
        }

        const double next = x - (function_value / derivative_value);
        if (!std::isfinite(next)) {
            throw std::overflow_error{"Newton-Raphson produced a non-finite iterate."};
        }
        const double next_value = evaluate(function, next);
        if (detail::has_converged(next, x, next_value, options.tolerance)) {
            return {next, next_value, iteration, common::IterationStatus::converged};
        }
        x = next;
        function_value = next_value;
    }

    return {x, function_value, options.maximum_iterations,
            common::IterationStatus::maximum_iterations_reached,
            std::string{"Maximum number of iterations reached."}};
}

RootResult RootSolvers::secant(const std::function<double(double)>& function, double first_guess,
                               double second_guess, const RootFindingOptions& options) {
    require_function(function, "Secant iteration requires a function.");
    options.validate();
    detail::require_finite_scalar(first_guess, "The first guess must be finite.");
    detail::require_finite_scalar(second_guess, "The second guess must be finite.");

    double x0 = first_guess;
    double x1 = second_guess;
    double f0 = evaluate(function, x0);
    double f1 = evaluate(function, x1);
    if (std::abs(f0) <= options.tolerance) {
        return {x0, f0, 0, common::IterationStatus::converged};
    }
    if (std::abs(f1) <= options.tolerance) {
        return {x1, f1, 0, common::IterationStatus::converged};
    }

    for (std::size_t completed = 0; completed < options.maximum_iterations; ++completed) {
        const std::size_t iteration = completed + 1;
        const double denominator = f1 - f0;
        if (!std::isfinite(denominator)) {
            throw std::overflow_error{"Secant slope difference is not representable."};
        }
        if (std::abs(denominator) <= common::numeric_constants::nearly_zero) {
            return {x1, f1, iteration - 1, common::IterationStatus::numerical_breakdown,
                    std::string{"Secant slope is too close to zero."}};
        }

        const double next = x1 - (f1 * (x1 - x0) / denominator);
        if (!std::isfinite(next)) {
            throw std::overflow_error{"Secant iteration produced a non-finite iterate."};
        }
        const double next_value = evaluate(function, next);
        if (detail::has_converged(next, x1, next_value, options.tolerance)) {
            return {next, next_value, iteration, common::IterationStatus::converged};
        }

        x0 = x1;
        f0 = f1;
        x1 = next;
        f1 = next_value;
    }

    return {x1, f1, options.maximum_iterations, common::IterationStatus::maximum_iterations_reached,
            std::string{"Maximum number of iterations reached."}};
}

} // namespace sasd::numerics::root_finding

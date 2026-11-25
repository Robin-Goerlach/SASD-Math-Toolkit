#include "sasd/numerics/execution/execution_options.hpp"
#include "sasd/numerics/interpolation/interpolation_algorithms.hpp"
#include "sasd/numerics/interpolation/newton_interpolant.hpp"

#include <cmath>
#include <fstream>
#include <iostream>
#include <limits>
#include <sstream>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>

namespace {

using sasd::numerics::execution::ExecutionOptions;
using sasd::numerics::execution::ExecutionPolicy;
using sasd::numerics::interpolation::CubicSpline;
using sasd::numerics::interpolation::InterpolationAlgorithms;
using sasd::numerics::interpolation::NewtonInterpolant;

int failures = 0;
int checks = 0;

void expect(const bool condition, const std::string_view description) {
    ++checks;
    if (!condition) {
        std::cerr << "FAILED: " << description << '\n';
        ++failures;
    }
}

void expect_near(const double expected, const double actual, const double tolerance,
                 const std::string_view description) {
    expect(std::abs(expected - actual) <= tolerance, description);
}

template <typename Exception, typename Function>
void expect_throws(Function&& function, const std::string_view description) {
    bool observed = false;
    try {
        function();
    } catch (const Exception&) {
        observed = true;
    }
    expect(observed, description);
}

[[nodiscard]] std::vector<std::string> split(const std::string& value, const char delimiter) {
    std::stringstream input{value};
    std::vector<std::string> fields;
    std::string field;
    while (std::getline(input, field, delimiter)) {
        fields.push_back(field);
    }
    return fields;
}

[[nodiscard]] std::vector<double> parse_values(const std::string& field) {
    std::vector<double> values;
    for (const auto& token : split(field, ';')) {
        values.push_back(std::stod(token));
    }
    return values;
}

void test_conformance_cases() {
    const std::string path =
        std::string{SASD_MATH_TOOLKIT_CONFORMANCE_DIR} + "/interpolation/reference.csv";
    std::ifstream input{path};
    expect(input.good(), "interpolation conformance file is available");

    std::string line;
    std::getline(input, line); // header
    std::size_t cases = 0;
    while (std::getline(input, line)) {
        const auto fields = split(line, ',');
        if (fields.size() != 11) {
            throw std::runtime_error{"Malformed interpolation conformance row."};
        }

        const std::string& method = fields[1];
        const auto x = parse_values(fields[2]);
        const auto y = parse_values(fields[3]);
        const double point = std::stod(fields[4]);
        const double expected = std::stod(fields[7]);
        const double tolerance = std::stod(fields[10]);
        double actual = 0.0;

        if (method == "linear") {
            actual = InterpolationAlgorithms::linear(x[0], y[0], x[1], y[1], point);
        } else if (method == "lagrange") {
            actual = InterpolationAlgorithms::lagrange(x, y, point);
        } else if (method == "newton") {
            actual = InterpolationAlgorithms::newton_divided_difference(x, y, point);
        } else {
            const CubicSpline spline = method == "natural_spline"
                                           ? InterpolationAlgorithms::natural_cubic_spline(x, y)
                                           : InterpolationAlgorithms::clamped_cubic_spline(
                                                 x, y, std::stod(fields[5]), std::stod(fields[6]));
            actual = spline.evaluate(point);
            expect_near(std::stod(fields[8]), spline.first_derivative(point), tolerance,
                        "conformance spline first derivative matches reference");
            expect_near(std::stod(fields[9]), spline.second_derivative(point), tolerance,
                        "conformance spline second derivative matches reference");
        }

        expect_near(expected, actual, tolerance,
                    "conformance interpolation value matches reference");
        ++cases;
    }
    expect(cases == 5, "all interpolation conformance rows were executed");
}

void test_linear_and_polynomial_interpolation() {
    expect_near(14.0, InterpolationAlgorithms::linear(2.0, 10.0, 6.0, 18.0, 4.0), 1.0e-14,
                "linear interpolation finds the midpoint value");
    expect_near(6.0, InterpolationAlgorithms::linear(6.0, 18.0, 2.0, 10.0, 0.0), 1.0e-14,
                "linear interpolation permits reversed endpoints and extrapolation");
    expect_near(0.0,
                InterpolationAlgorithms::linear(0.0, std::numeric_limits<double>::max(), 1.0,
                                                -std::numeric_limits<double>::max(), 0.5),
                0.0, "linear interpolation avoids avoidable ordinate overflow");

    const std::vector<double> x{0.0, 1.0, 2.0};
    const std::vector<double> y{1.0, 4.0, 9.0};
    expect_near(6.25, InterpolationAlgorithms::lagrange(x, y, 1.5), 1.0e-12,
                "Lagrange reconstructs a quadratic");
    expect_near(6.25, InterpolationAlgorithms::newton_divided_difference(x, y, 1.5), 1.0e-12,
                "Newton reconstructs a quadratic");
    expect_near(7.0,
                InterpolationAlgorithms::lagrange(std::vector<double>{3.0},
                                                  std::vector<double>{7.0}, -100.0),
                0.0, "one-point interpolation is a constant");

    const auto coefficients = InterpolationAlgorithms::newton_divided_difference_coefficients(x, y);
    expect(coefficients == std::vector<double>({1.0, 3.0, 1.0}),
           "Newton coefficients match the expected Newton form");

    const std::vector<double> unordered_x{2.0, 0.0, 1.0};
    const std::vector<double> unordered_y{9.0, 1.0, 4.0};
    expect_near(6.25, InterpolationAlgorithms::lagrange(unordered_x, unordered_y, 1.5), 1.0e-12,
                "polynomial interpolation accepts unordered distinct nodes");
}

void test_newton_interpolant_and_batches() {
    std::vector<double> x{0.0, 1.0, 2.0};
    std::vector<double> y{1.0, 4.0, 9.0};
    const NewtonInterpolant interpolant{x, y};
    x[0] = 100.0;
    y[0] = 100.0;

    expect(interpolant.abscissas()[0] == 0.0,
           "Newton interpolant owns its abscissas independently");
    expect(interpolant.coefficients()[0] == 1.0,
           "Newton interpolant owns its coefficients independently");
    expect_near(12.25, interpolant.evaluate(2.5), 1.0e-12,
                "reusable Newton interpolant evaluates repeatedly");

    const std::vector<double> points{-1.0, 0.0, 0.5, 1.5, 2.0, 3.0};
    const ExecutionOptions sequential{ExecutionPolicy::sequential, 0, 1};
    const ExecutionOptions parallel{ExecutionPolicy::parallel, 3, 1};
    const ExecutionOptions automatic_parallel{ExecutionPolicy::automatic, 2, 1};
    const auto serial_values = interpolant.evaluate_many(points, sequential);
    const auto parallel_values = interpolant.evaluate_many(points, parallel);
    const auto automatic_values = interpolant.evaluate_many(points, automatic_parallel);
    expect(serial_values == parallel_values,
           "parallel Newton batch preserves deterministic output order");
    expect(serial_values == automatic_values,
           "automatic Newton batch follows the caller-selected threshold");
    expect_near(16.0, parallel_values.back(), 1.0e-12,
                "parallel Newton batch evaluates every point");
    expect(interpolant.evaluate_many({}, parallel).empty(), "empty Newton batch is supported");

    const std::vector<double> invalid_points{0.0, std::numeric_limits<double>::quiet_NaN(), 1.0};
    expect_throws<std::invalid_argument>(
        [&] { (void)interpolant.evaluate_many(invalid_points, parallel); },
        "parallel batch propagates worker validation failures");
}

void test_cubic_splines() {
    std::vector<double> x{0.0, 1.0, 2.0, 3.0};
    std::vector<double> y{0.0, 1.0, 0.0, 1.0};
    const CubicSpline natural = InterpolationAlgorithms::natural_cubic_spline(x, y);
    x[0] = -100.0;
    y[0] = -100.0;

    expect(natural.interval_start() == 0.0 && natural.interval_end() == 3.0,
           "spline owns immutable interval metadata");
    expect(natural.segment_count() == 3 && natural.knots().size() == 4,
           "spline exposes read-only knot and segment metadata");
    expect_near(0.0, natural.second_derivative(0.0), 1.0e-12,
                "natural spline has zero left endpoint curvature");
    expect_near(0.0, natural.second_derivative(3.0), 1.0e-12,
                "natural spline has zero right endpoint curvature");
    expect_near(1.0, natural.evaluate(1.0), 1.0e-12,
                "natural spline passes through an interior knot");
    const std::vector<double> expected_natural_values{0.0, 1.0, 0.0, 1.0};
    for (std::size_t index = 0; index < expected_natural_values.size(); ++index) {
        expect_near(expected_natural_values[index], natural.evaluate(natural.knots()[index]),
                    1.0e-12, "natural spline passes through every supplied knot");
    }

    const auto function = [](const double value) {
        return (value * value * value) - (2.0 * value * value) + value + 1.0;
    };
    const auto first = [](const double value) {
        return (3.0 * value * value) - (4.0 * value) + 1.0;
    };
    const auto second = [](const double value) { return (6.0 * value) - 4.0; };
    const std::vector<double> cubic_x{0.0, 1.0, 2.0, 3.0};
    std::vector<double> cubic_y;
    for (const double point : cubic_x) {
        cubic_y.push_back(function(point));
    }
    const CubicSpline clamped = InterpolationAlgorithms::clamped_cubic_spline(
        cubic_x, cubic_y, first(cubic_x.front()), first(cubic_x.back()));
    for (const double point : std::vector<double>{0.0, 0.25, 1.5, 2.75, 3.0}) {
        expect_near(function(point), clamped.evaluate(point), 1.0e-11,
                    "clamped spline reproduces an exact cubic value");
        expect_near(first(point), clamped.first_derivative(point), 1.0e-11,
                    "clamped spline reproduces an exact cubic first derivative");
        expect_near(second(point), clamped.second_derivative(point), 1.0e-11,
                    "clamped spline reproduces an exact cubic second derivative");
    }

    const CubicSpline two_point = InterpolationAlgorithms::natural_cubic_spline(
        std::vector<double>{2.0, 6.0}, std::vector<double>{10.0, 18.0});
    expect_near(14.0, two_point.evaluate(4.0), 1.0e-14,
                "two-knot natural spline reduces to a line");
}

void test_spline_batches_and_failures() {
    const CubicSpline spline = InterpolationAlgorithms::clamped_cubic_spline(
        std::vector<double>{0.0, 1.0, 2.0, 3.0}, std::vector<double>{1.0, 1.0, 3.0, 13.0}, 1.0,
        16.0);
    const std::vector<double> points{0.0, 0.25, 0.5, 1.5, 2.5, 3.0};
    const ExecutionOptions sequential{ExecutionPolicy::sequential, 0, 1};
    const ExecutionOptions parallel{ExecutionPolicy::parallel, 4, 1};
    expect(spline.evaluate_many(points, sequential) == spline.evaluate_many(points, parallel),
           "parallel spline values match sequential values exactly");
    expect(spline.first_derivative_many(points, sequential) ==
               spline.first_derivative_many(points, parallel),
           "parallel spline first derivatives preserve output order");
    expect(spline.second_derivative_many(points, sequential) ==
               spline.second_derivative_many(points, parallel),
           "parallel spline second derivatives preserve output order");

    expect_throws<std::invalid_argument>(
        [] { (void)InterpolationAlgorithms::linear(0.0, 1.0, 0.0, 2.0, 0.5); },
        "linear interpolation rejects duplicate abscissas");
    expect_throws<std::invalid_argument>(
        [] {
            (void)InterpolationAlgorithms::lagrange(std::vector<double>{0.0, 1.0, 1.0},
                                                    std::vector<double>{1.0, 2.0, 3.0}, 0.5);
        },
        "polynomial interpolation rejects duplicate abscissas");
    expect_throws<std::invalid_argument>(
        [] {
            (void)InterpolationAlgorithms::lagrange(std::vector<double>{0.0, 1.0},
                                                    std::vector<double>{1.0}, 0.5);
        },
        "interpolation rejects mismatched coordinate lengths");
    expect_throws<std::invalid_argument>(
        [] {
            (void)InterpolationAlgorithms::natural_cubic_spline(std::vector<double>{0.0, 2.0, 1.0},
                                                                std::vector<double>{0.0, 1.0, 2.0});
        },
        "spline construction rejects unordered knots");
    expect_throws<std::invalid_argument>(
        [] {
            (void)InterpolationAlgorithms::natural_cubic_spline(
                std::vector<double>{0.0, 1.0},
                std::vector<double>{0.0, std::numeric_limits<double>::infinity()});
        },
        "spline construction rejects non-finite ordinates");
    expect_throws<std::invalid_argument>(
        [] {
            (void)InterpolationAlgorithms::clamped_cubic_spline(
                std::vector<double>{0.0, 1.0}, std::vector<double>{0.0, 1.0},
                std::numeric_limits<double>::quiet_NaN(), 1.0);
        },
        "clamped spline rejects non-finite endpoint derivatives");
    expect_throws<std::out_of_range>([&] { (void)spline.evaluate(-0.01); },
                                     "spline rejects left extrapolation");
    expect_throws<std::out_of_range>([&] { (void)spline.first_derivative(3.01); },
                                     "spline rejects right extrapolation");
    expect_throws<std::out_of_range>(
        [&] { (void)spline.second_derivative(std::numeric_limits<double>::quiet_NaN()); },
        "spline rejects non-finite evaluation points");
}

} // namespace

int main() {
    test_conformance_cases();
    test_linear_and_polynomial_interpolation();
    test_newton_interpolant_and_batches();
    test_cubic_splines();
    test_spline_batches_and_failures();

    if (failures != 0) {
        std::cerr << failures << " C++ interpolation test(s) failed.\n";
        return 1;
    }
    std::cout << "All " << checks << " C++ interpolation checks passed.\n";
    return 0;
}

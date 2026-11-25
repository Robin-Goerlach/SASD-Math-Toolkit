#include "sasd/numerics/execution/execution_options.hpp"
#include "sasd/numerics/statistics/descriptive_statistics.hpp"
#include "sasd/numerics/statistics/variance_mode.hpp"

#include <array>
#include <cmath>
#include <iostream>
#include <limits>
#include <stdexcept>
#include <string_view>
#include <vector>

namespace {

int failures = 0;
int checks = 0;

void expect(const bool condition, const std::string_view description) {
    ++checks;
    if (!condition) {
        std::cerr << "FAILED: " << description << '\n';
        ++failures;
    }
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

void test_reference_values() {
    using sasd::numerics::statistics::mean;
    using sasd::numerics::statistics::median;
    using sasd::numerics::statistics::variance;
    using sasd::numerics::statistics::VarianceMode;

    constexpr std::array values{1.0, 2.0, 3.0, 4.0, 5.0};
    expect(mean(values) == 3.0, "mean matches an analytic reference");
    expect(median(values) == 3.0, "odd median matches an analytic reference");
    expect(std::abs(variance(values) - 2.0) < 1.0e-15, "population variance uses denominator N");
    expect(std::abs(variance(values, VarianceMode::sample) - 2.5) < 1.0e-15,
           "sample variance uses denominator N-1");

    constexpr std::array even_values{8.0, 1.0, 4.0, 2.0};
    expect(median(even_values) == 3.0, "even median sorts a defensive copy");
    expect(even_values[0] == 8.0, "median does not modify caller-owned input");
}

void test_numerical_boundaries() {
    using sasd::numerics::statistics::mean;
    using sasd::numerics::statistics::median;
    using sasd::numerics::statistics::variance;
    using sasd::numerics::statistics::VarianceMode;

    constexpr double maximum = std::numeric_limits<double>::max();
    constexpr std::array repeated_maximum{maximum, maximum};
    expect(mean(repeated_maximum) == maximum,
           "scaled mean avoids overflow for repeated maximum values");
    expect(median(repeated_maximum) == maximum,
           "midpoint median avoids overflow for repeated maximum values");
    expect(variance(repeated_maximum) == 0.0,
           "variance of a large constant sequence is exactly zero");

    const std::vector<double> many_maxima(10'000, maximum);
    expect(mean(many_maxima) == maximum,
           "parallel scaled mean remains finite for many maximum values");

    constexpr std::array symmetric_extremes{-maximum, maximum};
    expect(mean(symmetric_extremes) == 0.0, "mean retains cancellation across finite extremes");
    expect_throws<std::overflow_error>([&] { (void)variance(symmetric_extremes); },
                                       "unrepresentable variance reports overflow");

    constexpr std::array singleton{42.0};
    expect_throws<std::invalid_argument>([&] { (void)variance(singleton, VarianceMode::sample); },
                                         "sample variance rejects a singleton");
}

void test_validation() {
    using sasd::numerics::statistics::mean;
    using sasd::numerics::statistics::median;
    using sasd::numerics::statistics::variance;
    using sasd::numerics::statistics::VarianceMode;

    const std::vector<double> empty;
    expect_throws<std::invalid_argument>([&] { (void)mean(empty); }, "mean rejects empty input");
    expect_throws<std::invalid_argument>([&] { (void)median(empty); },
                                         "median rejects empty input");
    expect_throws<std::invalid_argument>([&] { (void)variance(empty); },
                                         "variance rejects empty input");

    const std::array with_nan{1.0, std::numeric_limits<double>::quiet_NaN()};
    const std::array with_infinity{1.0, std::numeric_limits<double>::infinity()};
    expect_throws<std::invalid_argument>([&] { (void)mean(with_nan); }, "mean rejects NaN");
    expect_throws<std::invalid_argument>([&] { (void)median(with_infinity); },
                                         "median rejects infinity");
    expect_throws<std::invalid_argument>([&] { (void)variance(with_nan); }, "variance rejects NaN");

    constexpr std::array valid_values{1.0, 2.0};
    expect_throws<std::invalid_argument>(
        [&] { (void)variance(valid_values, static_cast<VarianceMode>(255)); },
        "variance rejects an unknown mode");
}

void test_deterministic_parallel_statistics() {
    using sasd::numerics::execution::ExecutionOptions;
    using sasd::numerics::execution::ExecutionPolicy;
    using sasd::numerics::statistics::mean;
    using sasd::numerics::statistics::variance;
    using sasd::numerics::statistics::VarianceMode;

    std::vector<double> values(20'000);
    for (std::size_t index = 0; index < values.size(); ++index) {
        values[index] = static_cast<double>(index + 1);
    }

    const ExecutionOptions sequential{ExecutionPolicy::sequential, 1, 1};
    const ExecutionOptions parallel{ExecutionPolicy::parallel, 4, 1};
    const double sequential_mean = mean(values, sequential);
    const double parallel_mean = mean(values, parallel);
    const double repeated_parallel_mean = mean(values, parallel);
    const double sequential_variance = variance(values, VarianceMode::population, sequential);
    const double parallel_variance = variance(values, VarianceMode::population, parallel);

    expect(std::abs(sequential_mean - 10'000.5) < 1.0e-10,
           "large reference mean is correct within floating-point roundoff");
    expect(std::abs(parallel_mean - sequential_mean) < 1.0e-10,
           "parallel mean agrees with sequential evaluation");
    expect(repeated_parallel_mean == parallel_mean,
           "a fixed parallel configuration has deterministic reduction semantics");
    expect(std::abs(parallel_variance - sequential_variance) <=
               std::abs(sequential_variance) * 1.0e-15,
           "parallel variance agrees with sequential evaluation");
}

} // namespace

int main() {
    test_reference_values();
    test_numerical_boundaries();
    test_validation();
    test_deterministic_parallel_statistics();

    if (failures != 0) {
        std::cerr << failures << " C++ statistics test(s) failed.\n";
        return 1;
    }

    std::cout << "All " << checks << " C++ statistics checks passed.\n";
    return 0;
}

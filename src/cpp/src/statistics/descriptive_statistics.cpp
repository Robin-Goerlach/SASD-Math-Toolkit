#include "sasd/numerics/statistics/descriptive_statistics.hpp"

#include "detail/parallel_for.hpp"

#include <algorithm>
#include <cmath>
#include <cstddef>
#include <limits>
#include <numeric>
#include <stdexcept>
#include <vector>

namespace sasd::numerics::statistics {
namespace {

class CompensatedSum final {
  public:
    void add(const double value) noexcept {
        const double next = sum_ + value;
        if (std::abs(sum_) >= std::abs(value)) {
            correction_ += (sum_ - next) + value;
        } else {
            correction_ += (value - next) + sum_;
        }
        sum_ = next;
    }

    [[nodiscard]] double value() const noexcept { return sum_ + correction_; }

  private:
    double sum_ = 0.0;
    double correction_ = 0.0;
};

class ScaledSumOfSquares final {
  public:
    void add(const double value) noexcept {
        const double magnitude = std::abs(value);
        if (magnitude == 0.0) {
            return;
        }

        // Store scale^2 * scaled_sum instead of squaring the raw value. This
        // is the same stability idea used by robust Euclidean-norm routines.
        if (scale_ < magnitude) {
            const double ratio = scale_ / magnitude;
            scaled_sum_ = 1.0 + (scaled_sum_ * ratio * ratio);
            scale_ = magnitude;
        } else {
            const double ratio = magnitude / scale_;
            scaled_sum_ += ratio * ratio;
        }
    }

    void combine(const ScaledSumOfSquares& other) noexcept {
        if (other.scale_ == 0.0) {
            return;
        }
        if (scale_ == 0.0) {
            *this = other;
            return;
        }

        if (scale_ < other.scale_) {
            const double ratio = scale_ / other.scale_;
            scaled_sum_ = other.scaled_sum_ + (scaled_sum_ * ratio * ratio);
            scale_ = other.scale_;
        } else {
            const double ratio = other.scale_ / scale_;
            scaled_sum_ += other.scaled_sum_ * ratio * ratio;
        }
    }

    [[nodiscard]] double scale() const noexcept { return scale_; }
    [[nodiscard]] double scaled_sum() const noexcept { return scaled_sum_; }

  private:
    double scale_ = 0.0;
    double scaled_sum_ = 1.0;
};

void require_values(const std::span<const double> values) {
    if (values.empty()) {
        throw std::invalid_argument{"Statistics require at least one value."};
    }
}

execution::ExecutionOptions chunk_execution_options(const std::size_t chunk_count) {
    return {execution::ExecutionPolicy::parallel, chunk_count, 1};
}

template <typename Function>
void for_each_chunk(const std::size_t value_count,
                    const execution::ExecutionOptions& execution_options, Function&& function) {
    const std::size_t chunk_count =
        execution::effective_thread_count(value_count, execution_options);
    const std::size_t base_chunk_size = value_count / chunk_count;
    const std::size_t remainder = value_count % chunk_count;

    detail::parallel_for(
        chunk_count, chunk_execution_options(chunk_count), [&](const std::size_t chunk) {
            const std::size_t begin = (chunk * base_chunk_size) + std::min(chunk, remainder);
            const std::size_t size = base_chunk_size + (chunk < remainder ? 1 : 0);
            function(chunk, begin, begin + size);
        });
}

} // namespace

double mean(const std::span<const double> values,
            const execution::ExecutionOptions& execution_options) {
    require_values(values);

    const std::size_t chunk_count =
        execution::effective_thread_count(values.size(), execution_options);
    std::vector<double> partial_scales(chunk_count, 0.0);

    for_each_chunk(values.size(), execution_options,
                   [&](const std::size_t chunk, const std::size_t begin, const std::size_t end) {
                       double scale = 0.0;
                       for (std::size_t index = begin; index < end; ++index) {
                           if (!std::isfinite(values[index])) {
                               throw std::invalid_argument{
                                   "Statistics require finite input values."};
                           }
                           scale = std::max(scale, std::abs(values[index]));
                       }
                       partial_scales[chunk] = scale;
                   });

    const double scale = *std::max_element(partial_scales.begin(), partial_scales.end());
    if (scale == 0.0) {
        return 0.0;
    }

    std::vector<double> partial_sums(chunk_count, 0.0);
    for_each_chunk(values.size(), execution_options,
                   [&](const std::size_t chunk, const std::size_t begin, const std::size_t end) {
                       CompensatedSum partial;
                       for (std::size_t index = begin; index < end; ++index) {
                           partial.add(values[index] / scale);
                       }
                       partial_sums[chunk] = partial.value();
                   });

    // Chunk results are deliberately combined in index order, so scheduling
    // cannot change the returned floating-point value.
    CompensatedSum total;
    for (const double partial : partial_sums) {
        total.add(partial);
    }

    const double divisor = static_cast<double>(values.size());
    const double normalized_mean = std::clamp(total.value() / divisor, -1.0, 1.0);
    const double result = scale * normalized_mean;
    if (!std::isfinite(result)) {
        throw std::overflow_error{"The arithmetic mean is not representable as a double."};
    }
    return result;
}

double median(const std::span<const double> values) {
    require_values(values);

    std::vector<double> ordered(values.begin(), values.end());
    if (std::any_of(ordered.begin(), ordered.end(),
                    [](const double value) { return !std::isfinite(value); })) {
        throw std::invalid_argument{"Statistics require finite input values."};
    }

    std::sort(ordered.begin(), ordered.end());
    const std::size_t upper = ordered.size() / 2;
    if (ordered.size() % 2 != 0) {
        return ordered[upper];
    }

    // std::midpoint avoids the overflow possible in (lower + upper) / 2.
    return std::midpoint(ordered[upper - 1], ordered[upper]);
}

double variance(const std::span<const double> values, const VarianceMode mode,
                const execution::ExecutionOptions& execution_options) {
    require_values(values);
    if (mode != VarianceMode::population && mode != VarianceMode::sample) {
        throw std::invalid_argument{"Unknown variance mode."};
    }
    if (mode == VarianceMode::sample && values.size() < 2) {
        throw std::invalid_argument{"Sample variance requires at least two values."};
    }

    const double average = mean(values, execution_options);
    const std::size_t chunk_count =
        execution::effective_thread_count(values.size(), execution_options);
    std::vector<ScaledSumOfSquares> partial_sums(chunk_count);

    for_each_chunk(values.size(), execution_options,
                   [&](const std::size_t chunk, const std::size_t begin, const std::size_t end) {
                       ScaledSumOfSquares partial;
                       for (std::size_t index = begin; index < end; ++index) {
                           const double deviation = values[index] - average;
                           if (!std::isfinite(deviation)) {
                               throw std::overflow_error{
                                   "A variance deviation is not representable as a double."};
                           }
                           partial.add(deviation);
                       }
                       partial_sums[chunk] = partial;
                   });

    ScaledSumOfSquares total;
    for (const auto& partial : partial_sums) {
        total.combine(partial);
    }
    if (total.scale() == 0.0) {
        return 0.0;
    }

    const double denominator =
        static_cast<double>(mode == VarianceMode::sample ? values.size() - 1 : values.size());
    const double normalized_sum = total.scaled_sum() / denominator;
    const double maximum = std::numeric_limits<double>::max();

    // Check before the final multiplication so an unrepresentable variance is
    // reported intentionally rather than leaking positive infinity.
    if (total.scale() > std::sqrt(maximum / normalized_sum)) {
        throw std::overflow_error{"The variance is not representable as a double."};
    }

    const double root_mean_square_deviation = total.scale() * std::sqrt(normalized_sum);
    const double result = root_mean_square_deviation * root_mean_square_deviation;
    if (!std::isfinite(result)) {
        throw std::overflow_error{"The variance is not representable as a double."};
    }
    return result;
}

} // namespace sasd::numerics::statistics

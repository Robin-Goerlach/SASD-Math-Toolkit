#include "sasd/numerics/common/iteration_status.hpp"
#include "sasd/numerics/statistics/descriptive_statistics.hpp"

#include <array>
#include <iostream>

int main() {
    using sasd::numerics::common::IterationStatus;
    using sasd::numerics::common::to_string;

    constexpr std::array values{1.0, 2.0, 3.0};
    const double average = sasd::numerics::statistics::mean(values);

    std::cout << "Installed SASD Math Toolkit C++ status: " << to_string(IterationStatus::converged)
              << '\n';
    return average == 2.0 ? 0 : 1;
}

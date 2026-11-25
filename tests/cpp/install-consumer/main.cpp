#include "sasd/numerics/common/iteration_status.hpp"

#include <iostream>

int main() {
    using sasd::numerics::common::IterationStatus;
    using sasd::numerics::common::to_string;

    std::cout << "Installed SASD Math Toolkit C++ status: " << to_string(IterationStatus::converged)
              << '\n';
    return 0;
}

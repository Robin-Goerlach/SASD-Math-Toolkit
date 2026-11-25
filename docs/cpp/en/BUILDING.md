# Building the C++ edition

## Requirements

- CMake 3.25 or newer;
- a C++20 compiler (GCC, Clang, AppleClang or MSVC);
- the platform thread library;
- optional Doxygen for the generated API reference.

The pre-1.0 C++ milestones deliberately build a static reference library. Shared-library symbol
visibility and ABI policy will be designed explicitly before a dynamic target
is offered, rather than inherited accidentally from `BUILD_SHARED_LIBS`.

## Configure, build and test

Run these commands from the repository root:

```bash
cmake -S src/cpp -B build/cpp -DCMAKE_BUILD_TYPE=Release
cmake --build build/cpp --parallel
ctest --test-dir build/cpp --output-on-failure
./build/cpp/samples/sasd_math_toolkit_diagnostics_sample
```

Multi-configuration generators such as Visual Studio select the configuration
at build/test time instead:

```powershell
cmake -S src/cpp -B build/cpp
cmake --build build/cpp --config Release
ctest --test-dir build/cpp -C Release --output-on-failure
```

## CMake options

| Option | Default for top-level build | Purpose |
|---|---:|---|
| `SASD_MATH_TOOLKIT_BUILD_TESTS` | `ON` | Build CTest tests |
| `SASD_MATH_TOOLKIT_BUILD_SAMPLES` | `ON` | Build C++ samples |
| `SASD_MATH_TOOLKIT_BUILD_DOCUMENTATION` | `OFF` | Add Doxygen API target |
| `SASD_MATH_TOOLKIT_DEVELOPER_WARNINGS` | `ON` | Enable `/W4` or `-Wall -Wextra -Wpedantic` |
| `SASD_MATH_TOOLKIT_WARNINGS_AS_ERRORS` | `OFF` | Promote warnings to errors |

CI enables warnings-as-errors with GCC and Clang.
Repository C++ formatting is defined by the root `.clang-format` file (LLVM
base style, four-space indentation and a 100-column target).

## Install and consume

```bash
cmake --install build/cpp --prefix artifacts/cpp-install
cmake -S tests/cpp/install-consumer -B build/install-consumer \
  -DCMAKE_PREFIX_PATH="$PWD/artifacts/cpp-install"
cmake --build build/install-consumer
./build/install-consumer/sasd_math_toolkit_install_consumer
```

Consumers use:

```cmake
find_package(SasdMathToolkit 0.2 CONFIG REQUIRED)
target_link_libraries(my_application PRIVATE SASD::MathToolkit)
```

## API reference

```bash
cmake -S src/cpp -B build/cpp-docs \
  -DSASD_MATH_TOOLKIT_BUILD_TESTS=OFF \
  -DSASD_MATH_TOOLKIT_BUILD_SAMPLES=OFF \
  -DSASD_MATH_TOOLKIT_BUILD_DOCUMENTATION=ON
cmake --build build/cpp-docs --target sasd_math_toolkit_cpp_api
```

Doxygen is the API reference, not a replacement for the user handbook.

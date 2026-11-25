cmake_minimum_required(VERSION 3.25)

project(SasdMathToolkitInstallConsumer LANGUAGES CXX)

find_package(SasdMathToolkit 0.2 REQUIRED CONFIG)

add_executable(sasd_math_toolkit_install_consumer main.cpp)
target_link_libraries(sasd_math_toolkit_install_consumer PRIVATE SASD::MathToolkit)
target_compile_features(sasd_math_toolkit_install_consumer PRIVATE cxx_std_20)

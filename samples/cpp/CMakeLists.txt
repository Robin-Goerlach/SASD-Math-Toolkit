add_executable(
    sasd_math_toolkit_diagnostics_sample
    diagnostics_sample.cpp)

target_link_libraries(
    sasd_math_toolkit_diagnostics_sample
    PRIVATE SASD::MathToolkit)
target_compile_features(sasd_math_toolkit_diagnostics_sample PRIVATE cxx_std_20)
sasd_math_toolkit_enable_warnings(sasd_math_toolkit_diagnostics_sample)

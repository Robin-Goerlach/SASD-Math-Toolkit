add_executable(
    sasd_math_toolkit_common_tests
    common_result_tests.cpp)

target_link_libraries(
    sasd_math_toolkit_common_tests
    PRIVATE SASD::MathToolkit)
target_include_directories(
    sasd_math_toolkit_common_tests
    PRIVATE "${CMAKE_CURRENT_LIST_DIR}/../../src/cpp/src")
target_compile_features(sasd_math_toolkit_common_tests PRIVATE cxx_std_20)
sasd_math_toolkit_enable_warnings(sasd_math_toolkit_common_tests)

add_test(
    NAME cpp.common_result
    COMMAND sasd_math_toolkit_common_tests)

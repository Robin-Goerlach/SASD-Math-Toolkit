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

add_executable(
    sasd_math_toolkit_statistics_tests
    statistics_tests.cpp)

target_link_libraries(
    sasd_math_toolkit_statistics_tests
    PRIVATE SASD::MathToolkit)
target_compile_features(sasd_math_toolkit_statistics_tests PRIVATE cxx_std_20)
sasd_math_toolkit_enable_warnings(sasd_math_toolkit_statistics_tests)

add_test(
    NAME cpp.statistics
    COMMAND sasd_math_toolkit_statistics_tests)

add_executable(
    sasd_math_toolkit_root_finding_tests
    root_finding_tests.cpp)

target_link_libraries(
    sasd_math_toolkit_root_finding_tests
    PRIVATE SASD::MathToolkit)
target_include_directories(
    sasd_math_toolkit_root_finding_tests
    PRIVATE "${CMAKE_CURRENT_LIST_DIR}/../../src/cpp/src")
target_compile_definitions(
    sasd_math_toolkit_root_finding_tests
    PRIVATE
        SASD_MATH_TOOLKIT_CONFORMANCE_DIR="${CMAKE_CURRENT_LIST_DIR}/../../conformance")
target_compile_features(sasd_math_toolkit_root_finding_tests PRIVATE cxx_std_20)
sasd_math_toolkit_enable_warnings(sasd_math_toolkit_root_finding_tests)

add_test(
    NAME cpp.root_finding
    COMMAND sasd_math_toolkit_root_finding_tests)

cmake_minimum_required(VERSION 3.25)

project(
    SasdMathToolkitCpp
    VERSION 0.1.0
    DESCRIPTION "Modern C++ implementation of SASD Math Toolkit"
    LANGUAGES CXX)

include(GNUInstallDirs)
include(CMakePackageConfigHelpers)
find_package(Threads REQUIRED)

option(SASD_MATH_TOOLKIT_BUILD_TESTS "Build the C++ test suite" ${PROJECT_IS_TOP_LEVEL})
option(SASD_MATH_TOOLKIT_BUILD_SAMPLES "Build the C++ samples" ${PROJECT_IS_TOP_LEVEL})
option(SASD_MATH_TOOLKIT_BUILD_DOCUMENTATION "Build the C++ Doxygen API reference" OFF)
option(SASD_MATH_TOOLKIT_DEVELOPER_WARNINGS "Enable strict compiler warnings" ${PROJECT_IS_TOP_LEVEL})
option(SASD_MATH_TOOLKIT_WARNINGS_AS_ERRORS "Treat C++ warnings as errors" OFF)

# The public contract is C++20. Compiler-specific language extensions are kept
# disabled so that accidental GNU/MSVC-only constructs are caught early.
set(CMAKE_CXX_EXTENSIONS OFF)

function(sasd_math_toolkit_enable_warnings target)
    if(NOT SASD_MATH_TOOLKIT_DEVELOPER_WARNINGS)
        return()
    endif()

    if(MSVC)
        target_compile_options(${target} PRIVATE /W4 /permissive-)
        if(SASD_MATH_TOOLKIT_WARNINGS_AS_ERRORS)
            target_compile_options(${target} PRIVATE /WX)
        endif()
    else()
        target_compile_options(${target} PRIVATE -Wall -Wextra -Wpedantic)
        if(SASD_MATH_TOOLKIT_WARNINGS_AS_ERRORS)
            target_compile_options(${target} PRIVATE -Werror)
        endif()
    endif()
endfunction()

add_library(
    sasd_math_toolkit STATIC
    src/common/iteration_status.cpp
    src/execution/execution_options.cpp)
add_library(SASD::MathToolkit ALIAS sasd_math_toolkit)

set_target_properties(
    sasd_math_toolkit
    PROPERTIES
        EXPORT_NAME MathToolkit
        OUTPUT_NAME sasd-math-toolkit
        VERSION ${PROJECT_VERSION}
        SOVERSION ${PROJECT_VERSION_MAJOR})

target_compile_features(sasd_math_toolkit PUBLIC cxx_std_20)
target_link_libraries(sasd_math_toolkit PUBLIC Threads::Threads)
target_include_directories(
    sasd_math_toolkit
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>)
sasd_math_toolkit_enable_warnings(sasd_math_toolkit)

if(SASD_MATH_TOOLKIT_BUILD_TESTS)
    enable_testing()
    add_subdirectory(
        "${CMAKE_CURRENT_SOURCE_DIR}/../../tests/cpp"
        "${CMAKE_CURRENT_BINARY_DIR}/tests")
endif()

if(SASD_MATH_TOOLKIT_BUILD_SAMPLES)
    add_subdirectory(
        "${CMAKE_CURRENT_SOURCE_DIR}/../../samples/cpp"
        "${CMAKE_CURRENT_BINARY_DIR}/samples")
endif()

if(SASD_MATH_TOOLKIT_BUILD_DOCUMENTATION)
    find_package(Doxygen REQUIRED)
    doxygen_add_docs(
        sasd_math_toolkit_cpp_api
        "${CMAKE_CURRENT_SOURCE_DIR}/include"
        COMMENT "Generate the SASD Math Toolkit C++ API reference")
endif()

install(
    TARGETS sasd_math_toolkit
    EXPORT SasdMathToolkitTargets
    ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
    INCLUDES DESTINATION ${CMAKE_INSTALL_INCLUDEDIR})

install(
    DIRECTORY include/
    DESTINATION ${CMAKE_INSTALL_INCLUDEDIR})

configure_package_config_file(
    cmake/SasdMathToolkitConfig.cmake.in
    "${CMAKE_CURRENT_BINARY_DIR}/SasdMathToolkitConfig.cmake"
    INSTALL_DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/SasdMathToolkit")

write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/SasdMathToolkitConfigVersion.cmake"
    VERSION ${PROJECT_VERSION}
    COMPATIBILITY SameMajorVersion)

install(
    EXPORT SasdMathToolkitTargets
    FILE SasdMathToolkitTargets.cmake
    NAMESPACE SASD::
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/SasdMathToolkit")

install(
    FILES
        "${CMAKE_CURRENT_BINARY_DIR}/SasdMathToolkitConfig.cmake"
        "${CMAKE_CURRENT_BINARY_DIR}/SasdMathToolkitConfigVersion.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/SasdMathToolkit")

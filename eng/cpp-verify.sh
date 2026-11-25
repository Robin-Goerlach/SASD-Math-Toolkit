#!/usr/bin/env bash
set -euo pipefail

mode="${1:-full}"
case "$mode" in
    quick|full) ;;
    *)
        echo "Usage: $0 [quick|full]" >&2
        exit 64
        ;;
esac

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

command -v cmake >/dev/null 2>&1 || {
    echo "cmake was not found in PATH." >&2
    exit 127
}

artifacts_dir="${CPP_ARTIFACTS_DIR:-artifacts/cpp-verify}"
case "$artifacts_dir" in
    artifacts/*) ;;
    *)
        echo "CPP_ARTIFACTS_DIR must be a relative path below artifacts/." >&2
        exit 64
        ;;
esac
build_dir="$artifacts_dir/build"
install_dir="$artifacts_dir/install"
consumer_build_dir="$artifacts_dir/install-consumer"

# Verification uses a clean, task-specific build tree. Keeping it below the
# ignored artifacts directory prevents stale CMake state from masking defects.
rm -rf "$artifacts_dir"
mkdir -p "$artifacts_dir"

echo "==> Configure C++"
cmake -S src/cpp -B "$build_dir" \
    -DCMAKE_BUILD_TYPE=Release \
    -DSASD_MATH_TOOLKIT_WARNINGS_AS_ERRORS=ON

echo "==> Build C++"
cmake --build "$build_dir" --parallel

echo "==> Test C++"
ctest --test-dir "$build_dir" --output-on-failure

echo "==> C++ sample smoke test"
sample="$build_dir/samples/sasd_math_toolkit_diagnostics_sample"
sample_output="$artifacts_dir/sample-output.txt"
"$sample" | tee "$sample_output"
grep -q 'status: converged' "$sample_output"
grep -q 'automatic workers' "$sample_output"

echo "==> C++ diff whitespace check"
git diff --check

if [[ "$mode" == "quick" ]]; then
    echo "C++ quick verification passed."
    exit 0
fi

echo "==> Install C++ package"
cmake --install "$build_dir" --prefix "$install_dir"

echo "==> Configure installed-package consumer"
cmake -S tests/cpp/install-consumer -B "$consumer_build_dir" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_PREFIX_PATH="$repo_root/$install_dir"

echo "==> Build installed-package consumer"
cmake --build "$consumer_build_dir" --parallel

echo "==> Run installed-package consumer"
"$consumer_build_dir/sasd_math_toolkit_install_consumer"

echo "C++ full verification passed."

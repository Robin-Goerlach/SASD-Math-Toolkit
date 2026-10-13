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

configuration="${CONFIGURATION:-Release}"
solution="Sasd.Math.Toolkit.slnx"
project="src/dotnet/Sasd.Math.Toolkit/Sasd.Math.Toolkit.csproj"
sample="samples/dotnet/Sasd.Math.Toolkit.Sample/Sasd.Math.Toolkit.Sample.csproj"
artifacts_dir="${ARTIFACTS_DIR:-artifacts/codex-verify}"

command -v dotnet >/dev/null 2>&1 || {
    echo "dotnet was not found in PATH." >&2
    exit 127
}

echo "==> Restore"
dotnet restore "$solution"

echo "==> Release build"
dotnet build "$solution" --configuration "$configuration" --no-restore

echo "==> Tests"
dotnet test "$solution" --configuration "$configuration" --no-build --verbosity normal

echo "==> Diff whitespace check"
git diff --check

if [[ "$mode" == "quick" ]]; then
    echo "Quick verification passed."
    exit 0
fi

echo "==> Prepare verification artifacts"
rm -rf "$artifacts_dir"
mkdir -p "$artifacts_dir/packages" "$artifacts_dir/demo"

echo "==> Pack"
dotnet pack "$project" \
    --configuration "$configuration" \
    --no-build \
    --output "$artifacts_dir/packages"

mapfile -t packages < <(find "$artifacts_dir/packages" -maxdepth 1 -type f -name 'Sasd.Math.Toolkit.*.nupkg' -print)
mapfile -t symbol_packages < <(find "$artifacts_dir/packages" -maxdepth 1 -type f -name 'Sasd.Math.Toolkit.*.snupkg' -print)

if [[ ${#packages[@]} -ne 1 ]]; then
    echo "Expected exactly one .nupkg, found ${#packages[@]}." >&2
    exit 1
fi

if [[ ${#symbol_packages[@]} -ne 1 ]]; then
    echo "Expected exactly one .snupkg, found ${#symbol_packages[@]}." >&2
    exit 1
fi

package="${packages[0]}"
if command -v unzip >/dev/null 2>&1; then
    echo "==> Verify package contents"
    unzip -l "$package" | grep -q 'lib/net10.0/Sasd.Math.Toolkit.dll'
    unzip -l "$package" | grep -q 'lib/net10.0/Sasd.Math.Toolkit.xml'
    unzip -l "$package" | grep -Eq '[[:space:]]README.md$'
    unzip -p "$package" '*.nuspec' | grep -q '<id>Sasd.Math.Toolkit</id>'
    unzip -p "$package" '*.nuspec' | grep -Eq '<license[^>]*>MIT</license>'
else
    echo "WARNING: unzip is unavailable; package-content inspection was skipped." >&2
fi

echo "==> Sample / sparse smoke test"
demo_file="$artifacts_dir/demo/sasd-math-toolkit-demo.html"
sample_output="$artifacts_dir/sample-output.txt"
dotnet run \
    --project "$sample" \
    --configuration "$configuration" \
    --no-build \
    -- "$demo_file" | tee "$sample_output"

test -s "$demo_file"
grep -q '<svg' "$demo_file"
grep -q 'Sparse reference solve' "$sample_output"

echo "==> Final diff whitespace check"
git diff --check

echo "Full verification passed."

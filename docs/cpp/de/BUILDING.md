# C++-Ausgabe bauen

## Voraussetzungen

- CMake ab Version 3.25;
- C++20-Compiler (GCC, Clang, AppleClang oder MSVC);
- Threadbibliothek der Plattform;
- optional Doxygen für die generierte API-Referenz.

Die C++-Meilensteine vor 1.0 erzeugen bewusst eine statische Referenzbibliothek. Symbolsichtbarkeit
und ABI-Regeln für Shared Libraries werden ausdrücklich entworfen, bevor ein
dynamisches Target angeboten wird, statt zufällig `BUILD_SHARED_LIBS` zu erben.

## Konfigurieren, bauen und testen

Aus dem Wurzelverzeichnis des Repositorys:

```bash
cmake -S src/cpp -B build/cpp -DCMAKE_BUILD_TYPE=Release
cmake --build build/cpp --parallel
ctest --test-dir build/cpp --output-on-failure
./build/cpp/samples/sasd_math_toolkit_diagnostics_sample
```

Visual Studio und andere Multi-Configuration-Generatoren erhalten die
Konfiguration erst beim Build und Test:

```powershell
cmake -S src/cpp -B build/cpp
cmake --build build/cpp --config Release
ctest --test-dir build/cpp -C Release --output-on-failure
```

## CMake-Optionen

| Option | Standard bei eigenständigem Build | Zweck |
|---|---:|---|
| `SASD_MATH_TOOLKIT_BUILD_TESTS` | `ON` | CTest-Tests bauen |
| `SASD_MATH_TOOLKIT_BUILD_SAMPLES` | `ON` | C++-Beispiele bauen |
| `SASD_MATH_TOOLKIT_BUILD_DOCUMENTATION` | `OFF` | Doxygen-API-Ziel anlegen |
| `SASD_MATH_TOOLKIT_DEVELOPER_WARNINGS` | `ON` | `/W4` bzw. `-Wall -Wextra -Wpedantic` |
| `SASD_MATH_TOOLKIT_WARNINGS_AS_ERRORS` | `OFF` | Warnungen als Fehler behandeln |

Die CI behandelt Warnungen mit GCC und Clang als Fehler.
Die C++-Formatierung wird durch `.clang-format` im Repository-Wurzelverzeichnis
festgelegt (LLVM-Basis, vier Leerzeichen und 100 Zeichen als Zeilenziel).

## Installieren und verwenden

```bash
cmake --install build/cpp --prefix artifacts/cpp-install
cmake -S tests/cpp/install-consumer -B build/install-consumer \
  -DCMAKE_PREFIX_PATH="$PWD/artifacts/cpp-install"
cmake --build build/install-consumer
./build/install-consumer/sasd_math_toolkit_install_consumer
```

Ein Consumer verwendet:

```cmake
find_package(SasdMathToolkit 0.4 CONFIG REQUIRED)
target_link_libraries(meine_anwendung PRIVATE SASD::MathToolkit)
```

Doxygen erzeugt bei aktivierter Dokumentationsoption das Ziel
`sasd_math_toolkit_cpp_api`. Die API-Referenz ersetzt nicht das Handbuch.

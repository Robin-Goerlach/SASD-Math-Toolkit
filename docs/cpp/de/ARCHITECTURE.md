# C++-Architektur

## 1. Rolle der C++-Ausgabe

Die C++-Ausgabe ist eine portable, abhängigkeitarme Referenzimplementierung
derselben mathematischen Verträge wie SASD Math Toolkit C#/.NET. Gleiches
Verhalten verlangt keinen identischen internen Code. Die C++-API verwendet
RAII, Wertsemantik, Standardbibliothekstypen und eindeutige Besitzverhältnisse.

## 2. Physische Grenzen

```text
src/cpp/                    Header, Implementierung und CMake-Paket
tests/cpp/                  C++-Tests und Installations-Consumer
samples/cpp/                C++-Beispiele
docs/cpp/                   technische C++-Dokumentation
docs/user-guide/cpp/        C++-Markdown-Handbuch
docs/user-guide/LaTeX/cpp/  eigenständiges C++-Satzprojekt
```

Der C++-Build kompiliert weder .NET-, Fortran- noch JavaScript-Code. Umgekehrt
enthält die vorhandene .NET-Solution keine C++-Dateien.

## 3. Namespace und Header

Der Root-Namespace lautet `sasd::numerics`. Fachgebiete bilden untergeordnete
Namespaces wie `common`, `execution`, `root_finding`, `linear_algebra` und
`transforms`. Installierte Header folgen unter `include/sasd/numerics/` derselben
Struktur.

Öffentliche Deklarationen erhalten Doxygen-kompatible Kommentare zu Verträgen,
Toleranzen, Ownership, Terminierung und sinnvoller Komplexität. Kommentare im
Algorithmus erklären mathematische oder numerische Gründe und nicht bloß die
Syntax der folgenden Zeile.

## 4. Fehler- und Ergebnismodell

Ungültige öffentliche Eingaben werden mit passenden normalen C++-Exceptions
abgewiesen. Erwartete numerische Ergebnisse wie Iterationslimit oder Breakdown
verwenden Statusobjekte. `IterativeResult<T>` trennt Näherungswert, Status,
Iterationszahl, Residuum und optionale Diagnosemeldung.

Ein endliches Residuum beweist keine Konvergenz. Residuen verschiedener
Problemklassen sind nicht ohne Weiteres vergleichbar.

## 5. Ownership

RAII ist verbindlich. Werttypen und `std::vector` werden bevorzugt. Views wie
`std::span` sind erlaubt, wenn ihre Lebensdauer eindeutig ist. Öffentliche
Ergebnisse dürfen keine versteckten Referenzen auf temporären Aufruferspeicher
halten. Manuelles `new` und `delete` gehört nicht zur normalen Numerik.

## 6. Parallelitätsgrenze

Parallelität ist über `execution::ExecutionOptions` explizit; es gibt keine
veränderbare globale Thread-Einstellung. `automatic` nutzt mehrere Kerne nur
bei unabhängiger und ausreichend großer Arbeit. `sequential` unterstützt
Diagnose und verhindert verschachtelte Parallelisierung. `parallel` dient
kontrollierten Benchmarks oder bewusst gesteuerten Aufrufen.

Interne Arbeit wird in deterministische zusammenhängende Bereiche geteilt.
Algorithmen müssen angeben, wenn sich durch eine veränderte Reduktionsreihenfolge
Rundungen ändern können, und ihre numerische Qualität unabhängig prüfen. Siehe
[Parallele Ausführung](PARALLELISM.md).

## 7. Abhängigkeiten und Backends

Die Referenzbibliothek benötigt die C++-Standardbibliothek und die von CMake
bereitgestellte Plattform-Threadbibliothek. Optionale BLAS/LAPACK-, SIMD-,
OpenMP- oder GPU-Backends kommen nur hinter stabilen SASD-Verträgen und nach
Messungen hinzu. Die portable Implementierung bleibt erhalten.

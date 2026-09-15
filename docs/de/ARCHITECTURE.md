# Architektur

## 1. Ziel

Das SASD Math Toolkit soll eine langfristig wiederverwendbare mathematische Grundlage werden. V1 orientiert sich funktional an der historischen Numerical-Methods-Sammlung, die Architektur wird aber nicht an alte Pascal-Dateien oder Kapitelstrukturen gebunden.

## 2. Repository-Aufbau

- `spec/`: sprachneutrale Verträge, Referenzfälle und spätere Cross-Language-Konformität.
- `src/dotnet/`: erste Implementierung in C#/.NET 10.
- `tests/dotnet/`: automatisierte numerische Tests.
- `samples/dotnet/`: Beispiele und Demonstrationsprogramme.
- `docs/en/`: primäre technische Dokumentation.
- `docs/de/`: deutsche Dokumentation.

Spätere Implementierungen kommen parallel hinzu, z. B. `src/cpp`, `src/fortran`, `src/java` und `src/javascript`. Dadurch bleibt kein Sprach-Frontend der architektonische Mittelpunkt des Gesamtprojekts.

## 3. .NET-Struktur

Das Assembly/Package heißt `Sasd.Math.Toolkit`; der Namespace beginnt bewusst mit `Sasd.Numerics`, damit es keine unnötige Namenskollision mit `System.Math` gibt.

Die fachlichen Bereiche sind getrennte Namespaces für Common, Root Finding, Interpolation, Differentiation, Integration, lineare Algebra, Differentialgleichungen, Approximation, Transformationen und Geometrie. Neue Bereiche wie Statistik, Optimierung oder Spezialfunktionen kommen später als gleichrangige Namespaces hinzu.

## 4. Fehler-, Konvergenz- und Diagnosemodell

Die Bibliothek trennt Aufruferfehler von erwartbarer numerischer Beendigung.

### 4.1 Aufrufverträge

Ungültige Parameter führen zu normalen .NET-Exceptions (`ArgumentException`, `ArgumentOutOfRangeException`). Öffentliche numerische Eingaben sollen endlich sein, sofern eine API nichts anderes ausdrücklich dokumentiert.

### 4.2 Erwartbare numerische Beendigung

Iterative Ergebnisse verwenden Statuswerte wie `Converged`, `MaximumIterationsReached`, `NumericalBreakdown` oder `NotBracketed`. Adaptive Quadratur und adaptive ODE-Integration besitzen eigene Status-Enums, weil Tiefenlimit und Mindestschritt fachlich andere Zustände sind als ein allgemeines Iterationslimit.

Die Architektur verlangt konsistente Semantik, nicht einen einzigen übergroßen universellen Status-Enum.

### 4.3 Status, Wert und Residuum sind getrennt

`IterativeResult<T>` trennt Beendigungsstatus, berechneten Wert und Residuum. `Converged` sagt nur aus, dass das dokumentierte Konvergenzkriterium erfüllt wurde. `HasFiniteResidual` sagt, ob das generische Residuenfeld aktuell eine endliche Diagnose enthält; daraus folgt keine Konvergenz.

Ein Residuum ist ein problembezogener Defekt wie `|f(x)|`, `||A*x-b||∞` oder `||A*v-lambda*v||2`. Diese Größen haben unterschiedliche Skalen und sind nicht allein wegen des gemeinsamen Begriffs „Residuum“ vergleichbar. Bei schlecht konditionierten Problemen folgt aus einem kleinen Residuum außerdem nicht automatisch ein kleiner Vorwärtsfehler.

Die ausführlichen Regeln stehen in [`NUMERISCHE-DIAGNOSTIK.md`](NUMERISCHE-DIAGNOSTIK.md).

## 5. Datenstrukturen

Der erste Matrixtyp ist ein kleiner, abhängigkeitfreier `DenseMatrix`. Er hält klassische Algorithmen verständlich und ist ausdrücklich kein HPC-Ersatz für BLAS/LAPACK.

Eine spätere Backend-Grenze kann ausgewählte Operationen an optimierte native Provider delegieren, ohne den öffentlichen SASD-Vertrag zu verändern.

Ergebnisobjekte mit Arrays oder Matrizen sollen defensive Besitzverhältnisse bzw. Kopien bevorzugen, wenn nachträgliche Mutation ein abgeschlossenes numerisches Ergebnis ungültig machen würde. Performance-orientierte Alternativen können später über ausdrückliche Verträge ergänzt werden.

## 6. Numerische Leitlinien

- Toleranzen sind explizit und nicht als veränderbarer globaler Zustand versteckt.
- `NumericConstants.DefaultTolerance` ist ein Standardwert, keine globale Genauigkeitsgarantie.
- `NumericConstants.NearlyZero` ist ein absoluter Schutzschwellwert, kein Maschinen-Epsilon und keine universelle Näherungsgleichheit.
- Robuste Formulierungen wie Partial Pivoting oder skalierte Normberechnung sind Standard, auch wenn historische Kompatibilitätsvarianten zusätzlich angeboten werden.
- Verständlichkeit und Testbarkeit gehen zunächst vor Mikrooptimierung, SIMD- oder Cache-Spezialisierung.
- Für große/HPC-Probleme können später optionale BLAS/LAPACK-Backends angebunden werden, ohne die mathematischen Verträge zu verändern.

## 7. Erweiterungsstrategie

Die Borland-kompatible V1 ist ein Meilenstein und nicht die endgültige Architektur. Neue Mathematik wird nach Fachdomäne und Abhängigkeitsrichtung ergänzt, nicht nach den Kapitelnummern eines historischen Handbuchs.

M3 kann wiederverwendbare skalierte Toleranz-/Vergleichsabstraktionen, Statistik-Grundlagen, Geometrie und Simulation ergänzen. Solche Abstraktionen sollen aus tatsächlich wiederholten Anforderungen entstehen und nicht jede einfache V1-Toleranz nachträglich in unnötigen Framework-Code verpacken.

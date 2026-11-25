# Sparse-Solver-Auswahl und Release-Referenzfälle

Die Sparse-Schicht 2026 enthält mehrere Krylov-Verfahren mit bewusst unterschiedlichen mathematischen Verträgen. Dieses Dokument ordnet sie ein, beschreibt die Release-Referenzfälle der Managed-Implementierung und hält die Architekturentscheidungen der M3.2-Konsolidierungsrunde fest.

## Mit der Matrixstruktur beginnen

Die Solverwahl sollte mit den bekannten Eigenschaften der Matrix beginnen und nicht mit dem Algorithmusnamen, der gerade am vertrautesten klingt.

| Matrix / Workload | Erste SASD-Wahl | Warum |
|---|---|---|
| Symmetrisch positiv definit (SPD) | PCG, meist mit Jacobi als Baseline | kurze Rekurrenz, geringer Speicherbedarf, nutzt SPD-Struktur |
| SPD, bereits gut skaliert oder nur wenige Iterationen erwartet | CG | vermeidet Aufbau/Anwendung eines Preconditioners |
| Allgemein nichtsymmetrisch, Residualrobustheit steht im Vordergrund | Restarted GMRES | explizite Residualminimierung im aktuellen Krylov-Raum |
| Allgemein nichtsymmetrisch, Speicher der Krylov-Basis ist kritisch | BiCGSTAB | feste Anzahl von O(n)-Arbeitsvektoren |

Das sind Startregeln und keine universelle Performance-Rangliste. Spektrum, Skalierung, Restart-Länge, Qualität des Preconditioners und Kosten des Matrix-Vektor-Produkts können das praktische Ergebnis verändern.

## Gemeinsamer Konvergenzvertrag

Alle iterativen Sparse-Solver verwenden dieselbe öffentliche Abbruchregel:

```text
||b - A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

Das Ergebnismodell meldet Anfangsresiduum, Endresiduum, Norm der rechten Seite, effektive Schwelle, Iterationszahl und Abbruchstatus. Ein Solver darf nicht allein deshalb `Converged` zurückgeben, weil ein günstiges rekursives oder projiziertes Residuum klein geworden ist. Vor einer Erfolgsmeldung wird das physikalische Residuum explizit neu berechnet.

`SparseMatrixDiagnostics.ResidualEuclideanNorm` stellt dieselbe physikalische Größe unabhängig von einem konkreten Solver öffentlich bereit. Anwendungen und Release-Tests können damit eine zurückgegebene Lösung durch direkte Neuberechnung von `||b-A*x||2` prüfen. Die Diagnose unterstützt auch rechteckige Matrizen und verwendet skalierte Akkumulation, damit große endliche Residualkomponenten nicht durch vermeidbaren Zwischenüberlauf verloren gehen.

Dieser gemeinsame öffentliche Vertrag ist wichtiger als der Versuch, alle Solver intern in dieselbe Rekurrenz zu pressen. CG/PCG, Arnoldi-/Givens-GMRES und BiCGSTAB besitzen tatsächlich unterschiedliche numerische Zustände und bleiben als eigenständige Algorithmen lesbar.

## Preconditioning-Vertrag

`ISparsePreconditioner` beschreibt eine angenäherte Inversoperation und keine explizit materialisierte inverse Matrix.

- PCG verlangt einen Preconditioner, der mit der SPD-Rekurrenz verträglich ist.
- GMRES und BiCGSTAB verwenden Right Preconditioning und können eine breitere Klasse fester linearer angenäherter Inversoperationen verwenden.
- `JacobiPreconditioner` ist die erste preiswerte Baseline und wird nicht als optimale Lösung für jedes Problem dargestellt.

Stärkere Incomplete-Factorization-Preconditioner bleiben aufgeschoben, bis reale SASD-Workloads ihre zusätzliche Komplexität rechtfertigen.

## Strukturierte Release-Referenzfälle

Kleine Unit-Tests bleiben für exakte Randfälle wichtig. Ein Release Candidate benötigt aber zusätzlich deterministische Systeme, die groß genug sind, um wiederholte Krylov-Schritte sowie Restart- und Preconditioning-Pfade auszuüben. `SparseSolverReferenceTests` enthält deshalb drei solverübergreifende Regressionsszenarien:

1. Ein symmetrisch positiv definites tridiagonales 64x64-System mit bekannter nichttrivialer Lösung. CG, PCG, restarted GMRES und BiCGSTAB müssen dieselbe Lösung rekonstruieren und denselben True-Residual-Vertrag erfüllen.
2. Ein diagonal dominantes nichtsymmetrisches tridiagonales 48x48-System. Right-preconditioned GMRES und BiCGSTAB starten bewusst mit unterschiedlichen Anfangswerten und müssen zur selben bekannten Lösung konvergieren.
3. Ein separates nichtsymmetrisches 40x40-System berechnet nach GMRES und BiCGSTAB unabhängig `b-A*x` neu und prüft damit, dass `SparseLinearSolveResult.ResidualNorm` das physikalische Residuum und nicht nur eine interne Rekurrenzschätzung beschreibt.

`SparseDiagnosticsTests` prüft zusätzlich rechteckige Residuen, große endliche Residualnormen sowie Validierungs- und Arithmetikgrenzen der öffentlichen Residualdiagnose.

Das sind Regressions-/Referenzfälle, **keine Benchmarks**. Sie enthalten keine Geschwindigkeitsbehauptung und verlangen bewusst nicht, dass ein Krylov-Verfahren weniger Iterationen benötigt als ein anderes.

## Release-nahes Beispiel

`SparseLinearAlgebraExample` im .NET-Sample-Projekt ergänzt einen kompakten Release-Smoke-Test. Ein deterministisches nichtsymmetrisches tridiagonales 24x24-System wird sowohl mit right-preconditioned restarted GMRES als auch mit BiCGSTAB gelöst. Beide Ergebnisse werden gegen die bekannte Lösung verglichen und das physikalische Residuum wird unabhängig neu berechnet.

Das normale Sample-Programm führt diesen Fall nach der Erzeugung des HTML-/SVG-Berichts aus und gibt Iterations- und Residualdiagnosen auf der Konsole aus. Damit übt ein gewöhnlicher Sample-Lauf neben den klassischen grafischen Beispielen auch die moderne Sparse-API aus.

## Speicher- und Allokationsmodell

Die Managed-Referenzimplementierung hält Sparse-Daten in kanonischem CSR und materialisiert innerhalb iterativer Lösungen keine dichten Matrizen.

- CG/PCG verwenden eine feste Anzahl von O(n)-Vektoren.
- BiCGSTAB verwendet ebenfalls eine feste Anzahl von O(n)-Vektoren.
- Restarted GMRES speichert eine Arnoldi-Basis, deren Speicherbedarf mit `restartLength` wächst; Restarting setzt diesem Wachstum eine explizite Grenze.
- `CsrMatrix.Multiply(ReadOnlySpan<double>, Span<double>)` erlaubt die Wiederverwendung von Solver-Arbeitspuffern, statt für jedes Matrix-Vektor-Produkt ein neues Ergebnisarray anzulegen.
- Öffentliche Post-Solve-Diagnosen dürfen aus Gründen der Klarheit einen temporären Vektor anlegen; sie liegen nicht im Solver-Hot-Loop.

Hier werden keine Throughput- oder Allokationsprozente behauptet. Dedizierte Benchmark-Arbeit bleibt Teil des späteren Performance-Backend-Meilensteins.

## Konsolidierungsentscheidung zu gemeinsamen Hilfsfunktionen

Die Konsolidierungsprüfung hat wiederholte Low-Level-Operationen in den Krylov-Implementierungen gefunden, etwa skalierte Vektornormen, kompensierte Skalarprodukte, Preconditioner-Anwendung und True-Residual-Neuberechnung. Sie werden an dieser Release-Grenze bewusst **nicht** vollständig in eine große interne Utility-Klasse verschoben.

Der Grund ist architektonisch und nicht nur historisch gewachsene Duplikation: Jeder Solver verbindet mit diesen Operationen andere Breakdown-Semantik und anderen diagnostischen Kontext. Die Prüfungen nahe an der jeweiligen mathematischen Rekurrenz zu halten, macht die Algorithmen derzeit leichter auditierbar. Das für Aufrufer wichtige gemeinsame Verhalten ist dagegen in öffentlichen Verträgen zentralisiert (`SparseIterativeSolverOptions`, `SparseLinearSolveResult`, `ISparsePreconditioner` und die Residualdiagnose). Ein kleiner interner Numerik-Kern kann später immer noch extrahiert werden, wenn Profiling oder Wartung einen konkreten Vorteil zeigen, ohne solver-spezifische Fehlerbehandlung zu verschleiern.

## M3.2-Release-Grenze — Fundament abgeschlossen

Das M3.2-Managed-Referenzfundament besteht jetzt aus:

- kanonischem unveränderlichem CSR;
- Sparse-Matvec, Transponieren und preiswerten Normen;
- gemeinsamer Konvergenz-/Ergebnissemantik;
- solverneutraler Preconditioner-Anwendung;
- Jacobi-Preconditioning;
- CG und PCG für SPD-Systeme;
- restarted right-preconditioned GMRES;
- right-preconditioned BiCGSTAB;
- öffentlicher unabhängiger True-Residual-Diagnostik;
- strukturierten solverübergreifenden Regressionsfällen;
- einem release-nahen Sparse-Sample.

Öffentliche Benennung/XML-Oberfläche und Allokationsmodell wurden für diese Grenze geprüft. Eigenes CSC, ILU/Incomplete Cholesky, weitere Krylov-Verfahren und benchmarkgetriebenes Tuning bleiben ausdrücklich aufgeschoben, bis konkrete Workloads sie rechtfertigen.

Der nächste Repository-Schritt ist das **repositoryweite Release-Candidate-Audit** und nicht noch ein weiterer Sparse-Solver.

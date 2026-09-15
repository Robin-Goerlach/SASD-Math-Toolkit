# Sparse-Solver-Auswahl und Referenzfälle

Die Sparse-Schicht 2026 enthält inzwischen mehrere Krylov-Verfahren mit bewusst unterschiedlichen mathematischen Verträgen. Dieses Dokument ordnet sie ein und definiert die strukturierten Regressionsfälle vor dem nächsten Release Candidate.

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

Dieser gemeinsame Vertrag ist für Anwendungscode wichtiger als der Versuch, alle Solver intern in dieselbe Rekurrenz zu pressen. CG/PCG, Arnoldi-/Givens-GMRES und BiCGSTAB besitzen tatsächlich unterschiedliche numerische Zustände und sollen als eigenständige Algorithmen lesbar bleiben.

## Preconditioning-Vertrag

`ISparsePreconditioner` beschreibt eine angenäherte Inversoperation und keine explizit materialisierte inverse Matrix.

- PCG verlangt einen Preconditioner, der mit der SPD-Rekurrenz verträglich ist.
- GMRES und BiCGSTAB verwenden Right Preconditioning und können eine breitere Klasse fester linearer angenäherter Inversoperationen verwenden.
- `JacobiPreconditioner` ist die erste preiswerte Baseline und wird nicht als optimale Lösung für jedes Problem dargestellt.

Stärkere Incomplete-Factorization-Preconditioner bleiben aufgeschoben, bis reale SASD-Workloads ihre zusätzliche Komplexität rechtfertigen.

## Strukturierte Release-Referenzfälle

Kleine Unit-Tests bleiben für exakte Randfälle wichtig. Ein Release Candidate benötigt aber zusätzlich deterministische Systeme, die groß genug sind, um wiederholte Krylov-Schritte sowie Restart- und Preconditioning-Pfade auszuüben. `SparseSolverReferenceTests` ergänzt deshalb drei solverübergreifende Regressionsszenarien:

1. Ein symmetrisch positiv definites tridiagonales 64x64-System mit bekannter nichttrivialer Lösung. CG, PCG, restarted GMRES und BiCGSTAB müssen dieselbe Lösung rekonstruieren und denselben True-Residual-Vertrag erfüllen.
2. Ein diagonal dominantes nichtsymmetrisches tridiagonales 48x48-System. Right-preconditioned GMRES und BiCGSTAB starten bewusst mit unterschiedlichen Anfangswerten und müssen zur selben bekannten Lösung konvergieren.
3. Ein separates nichtsymmetrisches 40x40-System berechnet nach GMRES und BiCGSTAB unabhängig `b-A*x` neu und prüft damit, dass `SparseLinearSolveResult.ResidualNorm` das physikalische Residuum und nicht nur eine interne Rekurrenzschätzung beschreibt.

Das sind Regressions-/Referenzfälle, **keine Benchmarks**. Sie enthalten keine Geschwindigkeitsbehauptung und verlangen bewusst nicht, dass ein Krylov-Verfahren weniger Iterationen benötigt als ein anderes.

## Speichermodell

Die Managed-Referenzimplementierung hält Sparse-Daten in kanonischem CSR und materialisiert innerhalb iterativer Lösungen keine dichten Matrizen.

- CG/PCG verwenden eine feste Anzahl von O(n)-Vektoren.
- BiCGSTAB verwendet ebenfalls eine feste Anzahl von O(n)-Vektoren.
- Restarted GMRES speichert eine Arnoldi-Basis, deren Speicherbedarf mit `restartLength` wächst; Restarting setzt diesem Wachstum eine explizite Grenze.

Deshalb ist die Restart-Länge ein öffentlicher Parameter und kein verborgener Implementierungswert.

## M3.2-Release-Grenze

Das aktuelle M3.2-Fundament besteht aus:

- kanonischem unveränderlichem CSR;
- Sparse-Matvec, Transponieren und preiswerten Normen;
- gemeinsamer Konvergenz-/Ergebnissemantik;
- solverneutraler Preconditioner-Anwendung;
- Jacobi-Preconditioning;
- CG und PCG für SPD-Systeme;
- restarted right-preconditioned GMRES;
- right-preconditioned BiCGSTAB;
- strukturierten solverübergreifenden Regressionsfällen.

Vor dem Release Candidate soll noch eine letzte Sparse-Konsolidierungsrunde öffentliche Benennung/XML-Dokumentation, duplizierte interne Numerik-Hilfen, Allokationsverhalten und release-nahe Beispiele prüfen. CSC, ILU/Incomplete Cholesky und zusätzliche Krylov-Verfahren liegen ausdrücklich außerhalb dieser Release-Grenze, sofern dieses Audit keinen konkreten Blocker aufdeckt.

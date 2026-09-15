# Matrixnormen und Konditionsdiagnostik

Dieses Dokument beschreibt die Skalierungs- und Konditionswerkzeuge aus der 2026-Modernisierung der dichten linearen Algebra.

## Warum dieser Bereich wichtig ist

Ein kleines Residuum beweist nicht, dass eine berechnete Lösung nahe an der exakten mathematischen Lösung liegt. Schlecht konditionierte Probleme können kleine Störungen der Eingangsdaten und Rundungsfehler stark verstärken. Das SASD Math Toolkit macht Matrixskalierung, numerischen Rang und Kondition deshalb ausdrücklich sichtbar, statt diese Fragen in einzelnen Solvern zu verstecken.

## Matrixnormen

`MatrixNorms` stellt bereit:

- `MaxAbsoluteEntry(A)` — größter absoluter Matrixeintrag;
- `OneNorm(A)` — größtmögliche absolute Spaltensumme;
- `InfinityNorm(A)` — größtmögliche absolute Zeilensumme;
- `FrobeniusNorm(A)` — Quadratwurzel der Summe aller quadrierten Einträge;
- `SpectralNorm(A)` — größter Singulärwert.

Die Implementierungen der 1-, Unendlich- und Frobeniusnorm verwenden dort, wo es numerisch sinnvoll ist, skalierte Akkumulation. Dadurch führen große endliche Matrixwerte nicht unnötig früh zu Zwischenüberläufen. Liegt die mathematisch verlangte Norm selbst außerhalb des endlichen `double`-Bereichs, wird eine `ArithmeticException` ausgelöst, statt still `Infinity` zurückzugeben.

`SpectralNorm` benötigt eine SVD. Wenn eine Anwendung zusätzlich Rang, Singulärvektoren oder Kondition benötigt, sollte sie eine `SingularValueDecomposition` erzeugen und wiederverwenden, statt nur für die Spektralnorm eine zusätzliche Zerlegung auszuführen.

## Gemeinsamer Diagnosebericht

`MatrixConditionDiagnostics.Analyze(A)` kombiniert die günstigen eintragsbasierten Normen mit genau einer SVD und liefert:

- Dimensionen;
- größten absoluten Eintrag;
- 1-, Unendlich-, Frobenius- und Spektralnorm;
- numerischen Rang;
- absoluten und relativen Rangschwellwert;
- linke und rechte Nullität;
- 2-Norm-Konditionszahl;
- reziproke 2-Norm-Konditionszahl.

Der Bericht trennt bewusst rechteckigen Vollrang von der Dimension des Nullraums. Eine 2x3-Matrix kann beispielsweise Rang 2 und damit vollen rechteckigen Rang besitzen, gleichzeitig aber rechte Nullität 1 haben. Genau dieser rechte Nullraum erklärt, warum ein unterbestimmtes Gleichungssystem unendlich viele Lösungen besitzen kann.

## Rangschwellwert

Für die SVD gilt weiterhin die Rangregel

`singularValue > largestSingularValue * relativeRankTolerance`.

Eine Änderung der Rangsensitivität verändert das numerische Modell des Problems. Sie ist kein allgemeiner Trick, um eine schwierige Matrix „durchzubekommen“. Eine als numerischer Nullraum verworfene Richtung wird auch von Pseudoinverser und SVD-Least-Squares nicht verwendet.

## Konditionszahl

Für eine numerisch vollrangige Matrix basiert die 2-Norm-Konditionszahl auf

`kappa2(A) = sigma_max / sigma_min`.

Ist die Matrix beim konfigurierten Schwellwert rangdefizient, ist `ConditionNumber2` positiv unendlich und `ReciprocalConditionNumber2` gleich null.

Die Konditionszahl ist eine Eigenschaft der Problemformulierung und kein Qualitätswert des Solvers. Auch ein stabiler Algorithmus kann korrekt melden, dass das zugrunde liegende Problem empfindlich ist.

## Performance-Grenze

Die eintragsbasierten Normen sind einfache Matrixdurchläufe. SVD-basierte Diagnostik ist bewusst teurer. `MatrixConditionDiagnostics.Analyze` führt deshalb nur eine SVD aus und verwendet sie gemeinsam für Spektralnorm, Rang und Kondition.

Die Managed-Implementierung bleibt der nachvollziehbare Referenzpfad. Große Produktionsaufgaben können später optionale BLAS-/LAPACK-Backends verwenden, ohne die diagnostische Semantik zu ändern.

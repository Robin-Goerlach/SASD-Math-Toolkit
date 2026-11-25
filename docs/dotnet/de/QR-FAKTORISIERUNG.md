# Householder-QR-Faktorisierung

## Rolle im modernisierten Toolkit

QR ist das erste bewusst nach der historischen Kompatibilitätsphase ergänzte numerische Fundament. Es beseitigt eine konkrete Schwäche der bisherigen Least-Squares-Implementierung: Normalgleichungen sind einfach, aber die Bildung von `A^T*A` quadriert die Konditionszahl.

`QrFactorization` wird deshalb vor dem späteren SVD-Meilenstein zur Standardbasis für dichtes Least Squares.

## Vertrag

`QrFactorization.Decompose(A)` akzeptiert derzeit quadratische oder hohe endliche dichte Matrizen (`rows >= columns`). Householder-Reflektoren werden kompakt im Row-Major-Layout zusammen mit der Diagonale von `R` gespeichert.

Öffentlich verfügbar sind:

- `Rows` und `Columns`
- `RelativeRankTolerance`
- `EstimatedRank`
- `IsFullColumnRank`
- `UpperTriangularFactor`
- `ThinOrthogonalFactor`
- `SolveLeastSquares(b)`

Der dünne orthogonale Faktor besitzt `m x n`, der obere Faktor `n x n` Elemente.

## Numerische Entscheidungen

Householder-Reflexionen werden dem klassischen Gram-Schmidt-Verfahren vorgezogen, weil sie die Orthogonalität bei allgemeinen dichten Matrizen deutlich robuster erhalten.

Spaltennormen werden mit einer skalierten Sum-of-Squares-Formel berechnet und nicht als naives `sqrt(sum(x*x))`. Dadurch werden vermeidbare Über- und Unterläufe bei der Normbildung reduziert.

Die Rangschätzung vergleicht `|R(i,i)|` relativ zum größten Diagonaleintrag. Sie ist bewusst nur eine Schätzung: Sie reicht zum Ablehnen klar rangdefizienter Dreieckslösungen, ersetzt aber keine Singulärwertanalyse.

## Least-Squares-Lösung

Für eine Matrix mit vollem Spaltenrang bestimmt der Solver den Minimierer von

`||A*x - b||2`

ohne `Q` vollständig aufzubauen:

1. kompakte Reflektoren auf `b` anwenden und `Q^T*b` bilden;
2. das führende obere Dreieckssystem mit `R` lösen;
3. die `n` Koeffizienten zurückgeben.

Der nicht benötigte Rest von `Q^T*b` enthält orthogonale Residualkoordinaten.

## Bewusste Grenzen

Der erste QR-Meilenstein behauptet nicht, jedes Least-Squares-Problem zu lösen.

- unterbestimmte Matrizen (`m < n`) werden abgelehnt;
- rangdefizientes Least Squares wird bei der konfigurierten Rangtoleranz abgelehnt;
- Pseudoinversen bleiben der geplanten SVD vorbehalten;
- Konditionszahlen werden nicht aus der QR-Diagonale erraten.

So liefert die API nicht still irgendeine Lösung, wenn stärkere Diagnostik notwendig ist.

## Performance

Die Faktorisierung speichert ein kompaktes Row-Major-Arbeitsarray und die `R`-Diagonale. Least-Squares-Lösungen verwenden diese Reflektoren erneut und erzeugen keine vollständige `m x m`-Matrix `Q`. `ThinOrthogonalFactor` wird nur auf ausdrücklichen Wunsch für Diagnose oder Folgealgorithmen materialisiert.

Die Implementierung bleibt eine Managed-Referenz. Blocking, natives BLAS/LAPACK, SIMD und Parallel-Kernel bleiben benchmarkgetriebene spätere Erweiterungen.

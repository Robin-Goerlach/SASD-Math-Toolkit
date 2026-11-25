# Performance-Leitlinie und V1-Optimierungsrunde

## Ziel

Die V1-Optimierungsrunde verbessert heiße Pfade, ohne die abhängigkeitfreie Referenzimplementierung in schwer wartbaren HPC-Code zu verwandeln. Korrektheit, Finite-Value-Diagnostik und stabile öffentliche APIs bleiben zwingend; vermeidbare Allokationen, wiederholte Callback-Auswertungen und schlechte Nutzung des Row-Major-Speichers sollen aber nicht nur der optischen Einfachheit wegen bestehen bleiben.

## Umgesetzte Optimierungen

### Dichte Matrizen

`DenseMatrix` behält den geprüften öffentlichen Indexer. Vertrauenswürdige Algorithmen im selben Assembly können nach einmaliger Zeilenprüfung interne Spans verwenden. Matrix-Vektor-Multiplikation greift im Hot Loop direkt auf den Row-Major-Speicher zu. Matrix-Matrix-Multiplikation verwendet die Reihenfolge Zeile–innere Dimension–Spalte, sodass rechte Matrix und Ergebnis zeilenweise zusammenhängend gelesen bzw. geschrieben werden. Dafür sind weder `unsafe`, Parallelisierung noch manuelles SIMD nötig.

### LU-Faktorisierung und Lösen

Elimination und Substitution arbeiten intern über Zeilen-Spans statt über millionenfach wiederholte geprüfte Indexerzugriffe. Ein skalares Solve verwendet für Vorwärts- und Rückwärtssubstitution denselben Arbeitsvektor. Mehrere rechte Seiten werden jetzt wirklich gemeinsam gelöst: Permutation und Dreieckssubstitution laufen über alle RHS-Spalten, statt für jede Spalte temporäre Vektoren anzulegen und den skalaren Solver neu aufzurufen. Davon profitiert automatisch auch die Inversenberechnung.

### Least Squares

Allgemeine Basisfunktionen werden nur einmal je Messpunkt ausgewertet und in einer kompakten Designmatrix zwischengespeichert. Von der symmetrischen Normalmatrix wird nur das obere Dreieck akkumuliert und anschließend gespiegelt. Polynomfits erzeugen Potenzen per Rekurrenz statt Power-Delegates anzulegen und `Math.Pow` wiederholt aufzurufen. Das numerische Modell bleibt unverändert: V1 verwendet weiterhin Normalgleichungen mit pivotiertem dichten Solver; QR/SVD ist später für schlecht konditionierte Probleme sinnvoll.

### FFT, Faltung und Korrelation

Öffentliche FFT-Aufrufe kopieren weiterhin Daten des Aufrufers. Intern neu erzeugte Arbeitsarrays werden dagegen direkt in-place transformiert. Real-FFT-Pfade vermeiden LINQ und eine zweite komplexe Kopie. Ein kompaktes Realspektrum darf ein frisch intern erzeugtes Bin-Array übernehmen, bleibt nach außen aber unveränderlich. Faltung und Korrelation transformieren ihre Zero-Padding-Puffer direkt, multiplizieren Spektren in-place und transformieren denselben Puffer zurück; reelle Varianten erzeugen nicht länger zuerst zusätzliche komplexe Eingabearrays.

## Bewusst nicht umgesetzt

Diese Runde führt keine unsicheren Pointer, manuelles SIMD, `ArrayPool`-Lebenszykluskomplexität, Parallel-Loops, Cache-Blocking, spezielle Real-FFT, native BLAS/LAPACK-Backends oder Laufzeit-Grenzwerte in der CI ein. Solche Entscheidungen benötigen repräsentative Benchmarks. Die aktuelle Runde konzentriert sich auf klare algorithmische und allokationsbezogene Kosten mit geringem semantischem Risiko.

## Regressionen

Die CI bleibt korrektheitsorientiert. Optimierungstests prüfen mathematische Gleichwertigkeit, Unveränderlichkeit von Eingaben und beobachtbare Arbeitsreduktion, etwa genau eine Basisfunktionsauswertung pro Messpunkt. Wall-Clock-Tests werden vermieden, weil Shared-CI-Laufzeiten zu stark schwanken. Ein separates Benchmark-Projekt ist nach V1 sinnvoll, sobald repräsentative SASD-Workloads vorliegen.

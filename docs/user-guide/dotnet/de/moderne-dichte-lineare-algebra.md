# Moderne dichte lineare Algebra — Erweiterungen 2026

Das klassische Matrixkapitel bleibt gültig. Die Modernisierung ergänzt Zerlegungen, die besser zu heutigen Workloads passen, und koppelt die Verfahrenswahl stärker an die Struktur der Matrix statt jedes Problem als allgemeine Gauß-Elimination zu behandeln.

## Householder-QR

Allgemeines und polynomiales Least Squares bevorzugen inzwischen Householder-QR statt Normalgleichungen. QR vermeidet die Bildung von `A^T*A`, quadriert die Konditionszahl also nicht nur für die Anpassung, und stellt eine praktische Vollrangdiagnostik bereit.

```csharp
var qr = QrFactorization.Decompose(design);
if (qr.IsFullColumnRank)
{
    var coefficients = qr.SolveLeastSquares(observations);
}
```

Für gewöhnliche quadratische/hohe Vollrang-Regression bleibt QR der bevorzugte Ausgangspunkt, weil es günstiger als eine vollständige SVD ist.

## Cholesky

Für eine symmetrisch positiv definite Matrix:

```csharp
var factorization = CholeskyFactorization.Decompose(a);
var x = factorization.Solve(b);
```

Die Faktorisierung speichert `A = L*L^T`, kann mehrere rechte Seiten ohne erneute Zerlegung lösen und bietet `LogDeterminant()` für skalierungsempfindliche Statistikaufgaben.

Cholesky sollte nicht auf eine lediglich symmetrische oder beliebige Matrix erzwungen werden. Positive Definitheit gehört zum mathematischen Vertrag und ist kein Implementierungsdetail.

## Singulärwertzerlegung

Die SVD übernimmt Fälle, in denen Rang und Kondition selbst Teil des Problems sind:

```csharp
var svd = SingularValueDecomposition.Decompose(a);

Console.WriteLine(svd.EstimatedRank);
Console.WriteLine(svd.ConditionNumber);

var minimumNorm = svd.SolveLeastSquares(b);
var pseudoInverse = svd.PseudoInverse();
```

Für eine `m x n`-Matrix werden dünne Faktoren bereitgestellt mit

```text
A = U * S * V^T
```

und `min(m,n)` absteigend sortierten Singulärwerten.

Die Managed-Referenzimplementierung verwendet eine einseitige Jacobi-SVD. Die Zerlegung wird bewusst nicht über `A^T*A` gewonnen; die Normalmatrix würde die Konditionszahl quadrieren und kann gerade schwache Singulärrichtungen verlieren, die mit einer SVD diagnostiziert werden sollen.

### Numerischer Rang ist toleranzabhängig

`EstimatedRank` basiert auf

```text
singularValue > LargestSingularValue * RelativeRankTolerance
```

Der resultierende absolute Grenzwert steht als `RankThreshold` zur Verfügung. Das ist eine numerische Entscheidung. Ein mathematisch von null verschiedener Singulärwert kann bewusst verworfen werden, wenn er relativ zur dominanten Skala zu klein für eine stabile Lösung ist.

Dieselbe Schwelle steuert `SolveLeastSquares` und `PseudoInverse`, damit Diagnose und Berechnung dieselben Singulärrichtungen verwenden.

### Rangdefizientes Least Squares

Für rangdefiziente oder unterbestimmte Probleme liefert `SolveLeastSquares` die Minimum-Euklidische-Norm-Lösung zu den beibehaltenen Singulärrichtungen. Das ist oft sinnvoller, als willkürlich eine Untermenge von Spalten auszuwählen.

Beispiel:

```csharp
var a = new DenseMatrix(new double[,]
{
    { 1.0, 0.0, 1.0 },
    { 0.0, 1.0, 1.0 }
});

var svd = SingularValueDecomposition.Decompose(a);
var x = svd.SolveLeastSquares(new[] { 1.0, 1.0 });
```

Das System besitzt unendlich viele exakte Lösungen. Die SVD liefert die Minimalnormlösung ungefähr `(1/3, 1/3, 2/3)`.

### Pseudoinverse

`PseudoInverse()` bildet die Moore-Penrose-Pseudoinverse mit derselben Abschneideschwelle. Sie ist sinnvoll, wenn eine nachfolgende Methode tatsächlich die Matrix benötigt. Für wiederholte rechte Seiten sollte die SVD wiederverwendet und `SolveLeastSquares` aufgerufen werden, statt jedes Mal die komplette Pseudoinverse zu materialisieren.

### Konditionszahl

Für eine numerisch vollrangige Matrix liefert `ConditionNumber` das Verhältnis vom größten zum kleinsten Singulärwert und damit die 2-Norm-Konditionszahl. Bei numerischer Rangdefizienz wird positive Unendlichkeit gemeldet; `ReciprocalConditionNumber` ist dann null.

Eine große Konditionszahl bedeutet, dass kleine Störungen der Eingangsdaten deutlich größere Änderungen der Lösung verursachen können. Sie bewertet nicht die Qualität der Implementierung, sondern die Empfindlichkeit des mathematischen Problems.

## Matrixnormen und gemeinsamer Diagnosebericht

Moderne numerische Anwendungen müssen häufig mehr beantworten als „hat der Solver einen Vektor geliefert?“. Matrixskalierung, Rang und Nullraumdimension entscheiden oft darüber, ob ein Ergebnis fachlich belastbar ist.

Der Helfer `MatrixNorms` bietet die gebräuchlichsten Normen:

```csharp
var one = MatrixNorms.OneNorm(a);
var infinity = MatrixNorms.InfinityNorm(a);
var frobenius = MatrixNorms.FrobeniusNorm(a);
var spectral = MatrixNorms.SpectralNorm(a);
```

`OneNorm` ist die größte absolute Spaltensumme, `InfinityNorm` die größte absolute Zeilensumme, `FrobeniusNorm` die euklidische Norm aller Einträge und `SpectralNorm` der größte Singulärwert. Frobenius- und Absolutsummenberechnung verwenden skalierte Akkumulation, damit große endliche Einträge nicht unnötig früh zu Zwischenüberläufen führen.

Wer mehrere Diagnosen gemeinsam benötigt, sollte eine einzige Analyse verwenden:

```csharp
var diagnostics = MatrixConditionDiagnostics.Analyze(a);

Console.WriteLine(diagnostics.EstimatedRank);
Console.WriteLine(diagnostics.LeftNullity);
Console.WriteLine(diagnostics.RightNullity);
Console.WriteLine(diagnostics.ConditionNumber2);
```

Der gemeinsame Bericht berechnet die günstigen eintragsbasierten Normen direkt und verwendet genau eine SVD gemeinsam für Spektralnorm, Rang und 2-Norm-Kondition. Dadurch werden versehentlich mehrfach ausgeführte identische Zerlegungen vermieden.

Linke und rechte Nullität werden bewusst getrennt dargestellt. Eine breite 2x3-Matrix kann vollen rechteckigen Rang 2 und trotzdem rechte Nullität 1 besitzen. Bei einem unterbestimmten System beschreibt dieser rechte Nullraum genau die Richtungen, die zu einer Lösung addiert werden können, ohne die rechte Seite zu verändern.

Ein kleines Residuum und eine große Konditionszahl können gleichzeitig auftreten. Das Residuum sagt, dass der berechnete Vektor die dargestellten Gleichungen gut erfüllt; die Konditionszahl sagt, dass die dargestellten Gleichungen selbst empfindlich auf kleinste Störungen reagieren können.

## Least-Squares-Routing im Toolkit

Die allgemeine API `LeastSquares.FitBasis` wählt den dichten Solver jetzt bewusst:

1. quadratische/hohe Designmatrix mit vollem Spaltenrang: Householder-QR;
2. rangdefiziente oder unterbestimmte Designmatrix: SVD-Minimalnormlösung.

Benannte Modelle dürfen strengere Identifizierbarkeitsregeln behalten. So weist der fünfgliedrige Fourier-Helfer weiterhin eine Phasenwahl zurück, die nicht alle fünf benannten Koeffizienten bestimmen kann, statt still eine von unendlich vielen äquivalenten Koeffizientenlösungen zurückzugeben.

## Verfahrensauswahl

| Problemstruktur | Sinnvoller Ausgangspunkt |
| --- | --- |
| Allgemeines quadratisches dichtes System | Pivotierte LU |
| Symmetrisch positiv definites System | Cholesky |
| Quadratisches/hohes Vollrang-Least-Squares | Householder-QR |
| Rangdefizientes Least Squares | SVD |
| Unterbestimmtes Minimalnorm-System | SVD |
| Matrixskalierung / 1-, Unendlich-, Frobeniusnorm | `MatrixNorms` |
| Numerischer Rang / 2-Norm-Kondition | SVD oder `MatrixConditionDiagnostics` |
| Explizite Pseudoinverse benötigt | SVD |
| Sehr großer dichter Produktions-Workload | Später optionaler BLAS/LAPACK-artiger Backend |

Die Verfahren ergänzen sich. Die SVD liefert die reichhaltigste Diagnose der drei modernen Zerlegungen, ist aber nicht die billigste Lösung für jedes gut strukturierte System.

## Performance-Perspektive

Die aktuellen QR-, Cholesky- und SVD-Implementierungen sind abhängigkeitfreie Managed-Referenzalgorithmen. Sie vermeiden offensichtliche unnötige Arbeit und numerisch ungünstige Formulierungen, sollen aber bei sehr großen Matrizen keine hochoptimierte Hersteller-BLAS/LAPACK schlagen. Die SASD-Strategie bleibt: nachvollziehbare deterministische Referenzschicht behalten, reale Workloads benchmarken und später optionale beschleunigte Backends hinter stabilen Konzepten ergänzen.

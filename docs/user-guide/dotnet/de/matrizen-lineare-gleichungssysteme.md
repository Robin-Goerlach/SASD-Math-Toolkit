# Matrizen und lineare Gleichungssysteme

Viele numerische Verfahren führen letztlich auf ein lineares Gleichungssystem

```text
A * x = b
```

mit der Koeffizientenmatrix `A`, dem unbekannten Vektor `x` und der rechten Seite `b`. Das SASD Math Toolkit stellt dafür eine bewusst kleine dichte Matrixbasis sowie direkte und iterative Lösungsverfahren für den V1-Referenz- und Lernumfang bereit.

Die Implementierung soll zuerst verständlich sein. Sie ist kein Ersatz für einen großen Sparse-Solver oder eine optimierte BLAS/LAPACK-Umgebung.

## 1. `DenseMatrix`

Eine Matrix kann aus einem rechteckigen C#-Array erzeugt werden:

```csharp
using Sasd.Numerics.LinearAlgebra;

var matrix = new DenseMatrix(new double[,]
{
    { 4.0, 1.0 },
    { 2.0, 3.0 }
});

Console.WriteLine(matrix.Rows);     // 2
Console.WriteLine(matrix.Columns);  // 2
Console.WriteLine(matrix.IsSquare); // true
```

Einträge sind veränderlich:

```csharp
matrix[0, 1] = 1.5;
```

die Dimensionen dagegen nach der Konstruktion fest. Matrixeinträge müssen endlich sein. `NaN` und Unendlich werden abgewiesen, weil sie sonst nachfolgende numerische Rechnungen still kontaminieren würden.

`Clone()` erzeugt eine unabhängige Kopie. Matrix-Vektor- und Matrix-Matrix-Multiplikation stehen ebenfalls zur Verfügung:

```csharp
var y = matrix.Multiply(new[] { 1.0, 2.0 });
var product = matrix.Multiply(DenseMatrix.Identity(2));
```

Die Multiplikation verwendet bewusst einfache Schleifen. Bei arithmetischem Überlauf wird eine Ausnahme ausgelöst, statt eine Matrix mit unendlichen Werten zurückzugeben.

## 2. Gauß-Elimination

Für ein einzelnes kleines dichtes System ist `SolveGaussian` das direkte klassische Verfahren:

```csharp
var a = new DenseMatrix(new double[,]
{
    { 2.0, 1.0 },
    { 5.0, 7.0 }
});

var b = new[] { 11.0, 13.0 };
var x = LinearSystemSolvers.SolveGaussian(a, b);
```

Die Methode kopiert `A` und `b`; die Objekte des Aufrufers werden nicht verändert.

### Partielle Pivotisierung

Partielle Pivotisierung ist standardmäßig aktiv und sollte normalerweise aktiv bleiben. In jedem Eliminationsschritt wird der betragsmäßig größte verfügbare Eintrag der aktuellen Pivotspalte gewählt.

Eine reguläre Matrix kann beispielsweise mit einer Null auf der ersten Diagonalposition beginnen:

```text
0  2
1  3
```

Eine Gauß-Elimination ohne Pivotisierung kann den ersten Diagonaleintrag nicht verwenden. Mit partieller Pivotisierung werden die Zeilen vertauscht und die Rechnung kann normal fortgesetzt werden.

`partialPivoting: false` bleibt für historischen und didaktischen Vergleich erhalten, nicht weil dies für Anwendungen empfohlen wäre.

## 3. Wiederverwendbare LU-Faktorisierung

Wenn mehrere Systeme dieselbe Koeffizientenmatrix verwenden, sollte die Elimination nicht jedes Mal wiederholt werden. Die Matrix wird einmal faktorisiert:

```csharp
var lu = LinearSystemSolvers.FactorizeLu(a);

var x1 = lu.Solve(new[] { 11.0, 13.0 });
var x2 = lu.Solve(new[] { 4.0, 9.0 });
```

Es gilt die Konvention

```text
P * A = L * U
```

mit der Permutation `P`, der unteren Dreiecksmatrix `L` mit Einsen auf der Diagonale und der oberen Dreiecksmatrix `U`.

Für Kontrolle und Diagnose stehen zur Verfügung:

```csharp
var lower = lu.LowerTriangular;
var upper = lu.UpperTriangular;
var permutation = lu.Permutation;
var toleranceUsed = lu.PivotTolerance;
```

Die öffentlich gelieferten Matrizen bzw. Arrays sind unabhängige Kopien, wenn eine Veränderung sonst die Faktorisierung beschädigen könnte.

Mehrere rechte Seiten können als Spalten einer `DenseMatrix` übergeben werden:

```csharp
var bMany = DenseMatrix.Identity(2);
var xMany = lu.Solve(bMany);
```

Genau dieses Muster wird auch zum Aufbau einer Inversen verwendet.

## 4. Determinanten

Die Determinante kann direkt berechnet werden:

```csharp
var determinant = LinearSystemSolvers.Determinant(a);
```

oder aus einer bereits vorhandenen LU-Faktorisierung:

```csharp
var determinant = lu.Determinant();
```

Existiert die Faktorisierung bereits, ist der LU-Weg sinnvoller, weil die Diagonale von `U` und die Parität der Zeilenvertauschungen wiederverwendet werden.

Eine Determinante nahe null kann auf Singularität hinweisen, ist aber allein kein zuverlässiges Maß für die Kondition einer Matrix. Ihr Betrag hängt außerdem stark von der Skalierung ab.

## 5. Inverse Matrix

Das Toolkit bietet:

```csharp
var inverse = LinearSystemSolvers.Inverse(a);
```

Intern wird genau eine LU-Faktorisierung erzeugt und gegen sämtliche Spalten der Einheitsmatrix gelöst.

Wenn das eigentliche Ziel jedoch `A*x=b` ist, sind `SolveGaussian` oder `LuFactorization.Solve` normalerweise besser. Die explizite Bildung von `A^-1` benötigt mehr Arbeit und kann numerische Fehler verstärken, ohne zusätzliche Information zu liefern.

Eine Inverse sollte verwendet werden, wenn ein Algorithmus wirklich die inverse Matrix benötigt – nicht nur, weil die symbolische Formel `x=A^-1*b` lautet.

## 6. Pivot-Toleranz und numerische Singularität

Die direkten Verfahren verwenden eine positive absolute `pivotTolerance`. Ein Pivotkandidat mit einem Betrag kleiner oder gleich diesem Grenzwert wird als numerisch null behandelt.

```csharp
var lu = LuFactorization.Decompose(a, pivotTolerance: 1e-12);
```

Das ist eine praktische Schutzschwelle, aber keine vollständige Konditionsanalyse. Eine Matrix kann Pivots oberhalb der Schwelle besitzen und dennoch schlecht konditioniert sein.

Bei unterschiedlich skalierten Problemfamilien muss dieselbe absolute Toleranz nicht gleich sinnvoll sein. V1 hält die Regel bewusst einfach und explizit; spätere SASD-Ausbaustufen können Skalierung und Konditionsschätzung ergänzen, ohne das grundlegende Lösungsmodell zu ändern.

## 7. Residuen

Nach dem Lösen sollte geprüft werden, wie gut der berechnete Vektor die tatsächlich übergebenen Gleichungen erfüllt:

```csharp
var residual = LinearSystemSolvers.ResidualInfinityNorm(a, x, b);
```

Berechnet wird

```text
||A*x - b||∞ = max_i |(A*x-b)_i|
```

Ein kleines Residuum ist ein wichtiges Indiz, beweist bei einer schlecht konditionierten Matrix aber nicht, dass `x` nahe an der exakten Lösung liegt.

Die Unterscheidung ist zentral:

- **Residuum**: Wie gut erfüllt der berechnete Vektor die angegebenen Gleichungen?
- **Vorwärtsfehler**: Wie nahe liegt der Vektor an der exakten mathematischen Lösung?
- **Kondition**: Wie empfindlich reagiert die Lösung auf kleine Änderungen der Eingangsdaten?

V1 stellt das Residuum bereit, aber noch keinen Konditionsschätzer.

## 8. Gauss-Seidel-Iteration

Gauss-Seidel ist eine iterative Alternative:

```csharp
var result = LinearSystemSolvers.GaussSeidel(
    new DenseMatrix(new double[,]
    {
        { 4.0, 1.0 },
        { 2.0, 3.0 }
    }),
    new[] { 1.0, 2.0 },
    tolerance: 1e-12,
    maximumIterations: 200);

if (result.Converged)
{
    Console.WriteLine(result.Value[0]);
    Console.WriteLine(result.Residual);
}
```

Konvergenz wird erst gemeldet, wenn sowohl die größte Änderung einer Komponente als auch die Unendlichkeitsnorm des Residuums innerhalb der verlangten Toleranz liegen.

Mögliche Statuswerte sind unter anderem:

- `Converged` – die V1-Konvergenzkriterien wurden erfüllt;
- `MaximumIterationsReached` – das Iterationsbudget ist aufgebraucht;
- `NumericalBreakdown` – beispielsweise bei einer Null bzw. nahezu Null auf der Diagonale oder bei numerischem Überlauf.

Gauss-Seidel **konvergiert nicht für jede reguläre Matrix**. Strikte Diagonaldominanz ist eine verbreitete hinreichende, aber keine notwendige Bedingung. Soll einfach ein kleines beliebiges dichtes System gelöst werden, ist pivotierte LU normalerweise der sicherere Ausgangspunkt.

## 9. Exceptions gegenüber Iterationsstatus

Die lineare Algebra folgt derselben SASD-Konvention wie die übrigen Bereiche.

Ungültige Verträge wie falsche Dimensionen, nicht-endliche Eingangswerte oder eine nicht-positive Toleranz führen zu Exceptions. Auch eine direkte Faktorisierung, die mangels brauchbarem Pivot nicht fortgesetzt werden kann, wirft eine `ArithmeticException`.

Gauss-Seidel ist dagegen ein iterativer Prozess. Erwartbare iterative Ergebnisse wie das Erreichen der Maximalzahl von Iterationen oder ein numerischer Zusammenbruch werden über `IterativeResult<double[]>` und `IterationStatus` dargestellt.

## 10. Verfahrensauswahl

Eine praktische V1-Faustregel lautet:

| Situation | Sinnvoller Ausgangspunkt |
| --- | --- |
| Ein kleines dichtes System | Pivotiertes `SolveGaussian` |
| Viele rechte Seiten mit demselben `A` | `FactorizeLu`, danach wiederholtes `Solve` |
| Determinante bei bereits vorhandener LU | `lu.Determinant()` |
| Die inverse Matrix selbst wird benötigt | `lu.Inverse()` / `LinearSystemSolvers.Inverse` |
| Iteratives Experiment mit geeigneter Matrix | `GaussSeidel` |
| Großes/Sparse/Hochleistungsproblem | Spezialisierter Backend statt V1-Referenzmatrix |

## 11. Numerische Grenzen

Lineare Gleichungssysteme zeigen besonders deutlich, weshalb „das Programm hat eine Zahl geliefert“ nicht gleichbedeutend mit „das Problem ist numerisch sicher“ ist.

Zu beachten sind insbesondere:

- schlecht skalierte Zeilen oder Spalten;
- nahezu linear abhängige Gleichungen;
- sehr unterschiedliche Größenordnungen der Koeffizienten;
- ein kleines Residuum, das wegen der Gesamtskalierung des Problems allein wenig aussagekräftig ist;
- iterative Verfahren, deren Konvergenzverhalten nicht geprüft wurde.

Die V1-Implementierung stellt bewusst partielle Pivotisierung, explizite Pivotschwellen, Residuen und Fehlerdiagnosen bereit und bleibt gleichzeitig klein genug, um sie nachvollziehen zu können. Konditionsschätzung, Sparse-Speicherung und Hochleistungskerne gehören in spätere Meilensteine und werden nicht hinter einer scheinbar einfachen V1-API versteckt.

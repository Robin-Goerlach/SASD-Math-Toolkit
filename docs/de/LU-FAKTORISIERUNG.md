# LU-Faktorisierung

## Zweck

`LuFactorization` ist der wiederverwendbare Baustein für die direkte Matrixfaktorisierung im V1-Kompatibilitätsumfang. Gleichzeitig dient er als Grundlage für spätere numerische Verfahren, die viele Gleichungssysteme mit derselben Koeffizientenmatrix lösen müssen.

Die Implementierung ist unabhängig neu entwickelt und verwendet eine klassische LU-Zerlegung mit partieller Zeilenpivotierung.

## Mathematische Konvention

Es gilt

```text
P * A = L * U
```

mit der ursprünglichen Matrix `A`, der Permutationsmatrix `P`, einer unteren Dreiecksmatrix `L` mit Einsen auf der Diagonale und der oberen Dreiecksmatrix `U`.

`LuFactorization.Permutation[i]` gibt an, welche ursprüngliche Zeile in Zeile `i` von `P * A` steht.

## Verwendung

```csharp
var lu = LinearSystemSolvers.FactorizeLu(matrix);
var x = lu.Solve(b);
var determinant = lu.Determinant();
var inverse = lu.Inverse();
```

Die Faktorisierung kann für beliebig viele rechte Seiten wiederverwendet werden. Mehrere rechte Seiten lassen sich außerdem als Spalten einer `DenseMatrix` übergeben.

## Architekturentscheidung

Faktorisierung und Lösen sind bewusst getrennt. Ein Einmal-Verfahren wäre für einen einzelnen Solve ausreichend, aber Eigenwertverfahren, Inversenbildung sowie spätere Statistik-/Optimierungsverfahren benötigen häufig wiederholte Lösungen mit derselben Matrix.

Intern werden `L` und `U` platzsparend gemeinsam gespeichert. Für Dokumentation, Tests und Diagnose liefern `LowerTriangular` und `UpperTriangular` jeweils unabhängige Matrizen zurück.

Partielle Pivotierung wählt in jeder Spalte den betragsmäßig größten verfügbaren Pivot. Das bleibt gut nachvollziehbar und ist numerisch deutlich robuster als eine ungepivotete Faktorisierung.

Singuläre bzw. gemäß `pivotTolerance` numerisch singuläre Matrizen führen zu einer `ArithmeticException`.

## Einbindung

`LinearSystemSolvers.Inverse` verwendet nun eine einzige LU-Faktorisierung für alle Spalten der Einheitsmatrix. Auch `EigenSolvers.InversePowerMethod` faktorisiert die Koeffizientenmatrix nur einmal und verwendet diese Faktorisierung für alle Iterationen.

Das ist keine Mikrooptimierung, sondern bildet die mathematische Struktur des Verfahrens sauber im API ab.

## Später

Die aktuelle Implementierung ist bewusst eine verständliche dichte Referenzimplementierung. Skalierte Pivotierung, Konditionsschätzung, Sparse-Matrizen und optionale BLAS/LAPACK-Backends bleiben spätere Erweiterungen.

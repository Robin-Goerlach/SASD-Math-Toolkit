# Dichte Matrizen und lineare Gleichungssysteme

## Zweck

Die lineare Algebra in V1 stellt eine abhängigkeitfreie Referenzimplementierung für den historischen Numerical-Methods-Umfang bereit und schafft gleichzeitig wiederverwendbare Bausteine für spätere SASD-Projekte.

Die zentrale Trennung lautet:

```text
DenseMatrix
   |
   +-- Speicherung / Multiplikation
   |
LinearSystemSolvers
   +-- einmalige Gauß-Elimination
   +-- Determinante
   +-- Residualdiagnose
   +-- Gauss-Seidel-Iteration
   |
LuFactorization
   +-- wiederverwendbare P*A=L*U-Zerlegung
   +-- wiederholte Vektor-/Matrix-Lösungen
   +-- Wiederverwendung für Determinante / Inverse
```

Damit bleiben Datenrepräsentation, einmalige Verfahren und wiederverwendbarer Faktorisierungszustand getrennt.

## DenseMatrix-Invariante

`DenseMatrix` besitzt veränderliche Einträge, aber unveränderliche Dimensionen. Jeder gespeicherte Eintrag muss endlich sein. Konstruktoren und Indexer weisen `NaN` und Unendlich zurück.

Diese Invariante ist beabsichtigt. Ein ungültiger Eingangswert soll möglichst nahe an seiner Quelle auffallen und nicht erst mehrere Algorithmen später wie ein scheinbares Konvergenzproblem wirken.

Der Typ bleibt bewusst klein: Sparse-Speicherung, Views, Slicing oder eine umfangreiche Operatorabstraktion gehören nicht in V1.

## Arithmetischer Überlauf

Auch endliche Eingangswerte können während Multiplikation oder Elimination überlaufen. Wichtige Produkte und Akkumulationen werden deshalb geprüft; statt still Unendlich zu speichern, wird eine `ArithmeticException` ausgelöst.

Das ist Diagnosehärtung und keine Arithmetik mit beliebiger Genauigkeit. Wenn der Wertebereich von IEEE-754-`double` nicht genügt, muss das Problem passend skaliert werden.

## Gauß-Elimination

`LinearSystemSolvers.SolveGaussian` arbeitet auf Kopien von Koeffizientenmatrix und rechter Seite und führt anschließend Rückwärtseinsetzen durch.

Partielle Pivotisierung ist standardmäßig eingeschaltet. In Pivotspalte `k` wird die verbleibende Zeile mit dem größten Betrag `abs(A[i,k])` gewählt. Der optionale ungepivotete Pfad bleibt für historischen und didaktischen Vergleich erhalten.

Ein Pivot mit Betrag kleiner oder gleich der positiven absoluten `pivotTolerance` führt zu `ArithmeticException`. Damit werden singuläre bzw. unter der gewählten Schwelle numerisch singuläre Matrizen erkannt.

## LU-Faktorisierung

`LuFactorization` verwendet die Konvention

```text
P * A = L * U
```

und speichert `L` und `U` kompakt in einer gemeinsamen Matrix. `Permutation`, `LowerTriangular` und `UpperTriangular` liefern für Diagnosen defensive bzw. unabhängige Daten. `PivotTolerance` hält fest, mit welcher Schwelle die Faktorisierung erzeugt wurde.

Wiederholte Lösungen sollen dieselbe Faktorisierung verwenden. Das ist eine architektonische Trennung und nicht nur ein Geschwindigkeitstrick: Die Zerlegung ist der vorbereitende Zustand, Dreieckslösen die wiederholbare Operation.

## Determinanten

`LinearSystemSolvers.Determinant` behält eine direkte Eliminationsimplementierung. Eine numerisch singuläre Determinante kann dadurch natürlich als null zurückgegeben werden, ohne zuerst ein Faktorisierungsobjekt erzeugen zu müssen, das die Matrix ablehnen würde.

Existiert bereits eine LU-Faktorisierung, ist `LuFactorization.Determinant` der bevorzugte Weg und multipliziert die Diagonaleinträge von `U` zusammen mit der Parität der Permutation.

Beide Pfade erkennen nicht-endlichen arithmetischen Überlauf.

## Inverse

`LinearSystemSolvers.Inverse` verwendet genau eine LU-Faktorisierung und löst sämtliche Spalten der Einheitsmatrix. Die Methode akzeptiert dieselbe Pivot-Toleranz.

Die API existiert, weil eine inverse Matrix für manche Algorithmen tatsächlich ein benötigtes Objekt ist und zum historischen Kompatibilitätsumfang gehört. Für `A*x=b` ist sie nicht der empfohlene Lösungsweg; direktes Lösen vermeidet eine unnötige Matrix und verhält sich normalerweise numerisch günstiger.

## Residualvertrag

`ResidualInfinityNorm` berechnet

```text
max_i |(A*x-b)_i|
```

und validiert Matrix-/Vektordimensionen sowie endliche Vektorwerte. Nicht-endliche Arithmetik während der Residualberechnung wird als `ArithmeticException` gemeldet.

Ein Residuum ist eine Rückwärtsdiagnose. Es ist weder eine Konditionsschätzung noch automatisch eine Schranke für den Vorwärtsfehler.

## Gauss-Seidel

Gauss-Seidel liefert `IterativeResult<double[]>`. Konvergenz erfordert gleichzeitig:

1. größte Komponentenänderung `<= tolerance` und
2. Unendlichkeitsnorm des Residuums `<= tolerance`.

Bei Null bzw. nahezu Null auf der Diagonale sowie nicht-endlicher Arithmetik durch Divergenz/Überlauf wird `NumericalBreakdown` zurückgegeben. Ist das Iterationsbudget ausgeschöpft, folgt `MaximumIterationsReached`.

Damit bleibt die toolkitweite Regel erhalten: ungültige API-Eingaben führen zu Exceptions; erwartbare Ergebnisse eines gültigen iterativen Prozesses werden durch Statuswerte dargestellt.

## Kondition

V1 behauptet ausdrücklich nicht, die Konditionszahl zu schätzen. Partielle Pivotisierung und Pivotschwelle erhöhen die Robustheit, machen aber aus einem schlecht konditionierten Problem kein gut konditioniertes.

Eine spätere SASD-Ausbaustufe der linearen Algebra kann skalierte Pivotisierung, Konditionsschätzung, iterative Verfeinerung und optionale Hochleistungsbackends ergänzen. Diese Funktionen sollten auf den jetzigen Abstraktionen aufbauen, statt die V1-Referenzimplementierung vorschnell zu verkomplizieren.

## Teststrategie

Regressionstests decken ab:

- endliche Matrixinvariante und Indexgrenzen;
- Dimensionsprüfung und Überlauferkennung bei Multiplikation;
- Determinante, numerische Null und Determinantenüberlauf;
- Gauß-Lösung mit und ohne erforderliche Pivotisierung;
- Erhalt der vom Aufrufer übergebenen Daten;
- LU-Rekonstruktion, wiederholte Lösungen, defensive Permutationsdaten, Determinante und Inverse;
- Gauss-Seidel-Konvergenz und Breakdown-Status;
- Residualvalidierung und analytische Referenzlösungen.

Dieselbe Matrixbasis wird zusätzlich indirekt durch Eigenwert-, Least-Squares- und weitere höherstufige Algorithmen getestet.

# Eigenwerte und Eigenvektoren

Bei einem Eigenwertproblem werden ein Skalar `lambda` und ein von Null verschiedener Vektor `v` gesucht, so dass

`A * v = lambda * v`

gilt. Solche Probleme treten unter anderem bei Stabilitätsanalysen, Schwingungsformen, wiederholten linearen Transformationen und Hauptrichtungen auf. SASD Math Toolkit V1 enthält mehrere klassische Verfahren für reelle dichte Matrizen. Sie lösen unterschiedliche Aufgaben; es gibt nicht den einen Eigenwertsolver, der für jede Matrix die beste Wahl ist.

## Welches Verfahren soll ich verwenden?

Verwende **`PowerMethod`**, wenn ein Eigenpaar zum betragsmäßig größten Eigenwert gesucht wird und das Spektrum eine hinreichend klare dominante Richtung besitzt.

Verwende **`InversePowerMethod`**, wenn der Eigenwert nahe Null gesucht wird und die Matrix nicht singulär ist. Das V1-Verfahren arbeitet unverschoben und wählt nicht heimlich einen Shift.

Verwende **`WielandtSecondEigenpair`** vor allem für den klassischen historischen Ablauf zum Eigenwert mit dem zweitgrößten Betrag. Es ist ein wertvolles Referenzverfahren, aber mehrfache bzw. nahezu mehrfache dominante Eigenwerte sind eine bekannte Schwachstelle.

Verwende **`CyclicJacobi`**, wenn die Matrix reell und symmetrisch ist und das vollständige Eigensystem benötigt wird. Für kleine dichte symmetrische Probleme ist dies häufig die informativste V1-API, weil alle Eigenwerte und eine orthonormale Eigenvektorbasis zurückgegeben werden.

V1 enthält noch keinen allgemeinen QR-/Schur-Eigensolver für nicht-symmetrische Matrizen und keine allgemeine API für komplexe Eigenwerte.

## Erstes Beispiel mit der Potenzmethode

```csharp
using Sasd.Numerics.LinearAlgebra;

var matrix = new DenseMatrix(new double[,]
{
    { 4.0, 1.0, 0.0 },
    { 1.0, 3.0, 0.0 },
    { 0.0, 0.0, 1.0 }
});

var result = EigenSolvers.PowerMethod(
    matrix,
    tolerance: 1e-12,
    maximumIterations: 500);

if (result.Converged)
{
    var pair = result.Value;
    Console.WriteLine($"lambda = {pair.Eigenvalue}");
    Console.WriteLine($"Residuum = {result.Residual}");

    foreach (var component in pair.Components)
    {
        Console.WriteLine(component);
    }
}
else
{
    Console.WriteLine($"{result.Status}: {result.Message}");
}
```

Das Ergebnis besteht bewusst nicht nur aus einer Zahl. `IterativeResult<Eigenpair>` dokumentiert, wie das Verfahren beendet wurde:

- `Converged` bedeutet, dass die Konvergenzkriterien des Verfahrens erfüllt wurden;
- `Iterations` enthält die Zahl der verwendeten Iterationen;
- `Residual` ist bei Potenz- und inverser Potenzmethode der euklidische Defekt der Eigenwertgleichung;
- `MaximumIterationsReached` liefert die letzte Näherung, kennzeichnet sie aber ausdrücklich als nicht bis zum geforderten Kriterium konvergiert;
- `NumericalBreakdown` kennzeichnet einen numerischen Zustand, in dem die Iteration nicht sinnvoll fortgesetzt werden konnte.

Ungültige Methodenparameter werden als Argument-Ausnahmen behandelt und nicht als normale Iterationsstatuswerte.

## Eigenvektoren sind nicht eindeutig

Ist `v` ein Eigenvektor, dann ist jedes von Null verschiedene skalare Vielfache ebenfalls ein Eigenvektor. Insbesondere beschreiben `v` und `-v` dieselbe Eigenrichtung. Die SASD-Solver normalisieren ihre Ergebnisvektoren, aber ein Test sollte normalerweise nicht nur deshalb fehlschlagen, weil eine andere mathematisch korrekte Implementierung das umgekehrte Vorzeichen gewählt hat.

Bei einem mehrfachen Eigenwert sind sogar die einzelnen Eigenvektoren innerhalb des zugehörigen Eigenraums nicht eindeutig. Ein vollständiger symmetrischer Eigensolver darf eine andere orthonormale Basis desselben Unterraums liefern und trotzdem korrekt sein.

## Ein Eigenpaar unabhängig prüfen

Für ein Paar `(lambda, v)` misst

`||A*v - lambda*v||2`

wie gut die definierende Gleichung erfüllt ist. SASD stellt diese Diagnose direkt bereit:

```csharp
var residual = EigenSolvers.EigenpairResidualNorm(matrix, result.Value);
```

Dies ist eine sehr nützliche Regressions- und Plausibilitätsprüfung. Ein kleines Residuum bedeutet jedoch **nicht**, dass das Eigenwertproblem gut konditioniert ist. Eng zusammenliegende Eigenwerte können empfindlich sein, obwohl ein berechnetes Paar die Gleichung mit kleinem Defekt erfüllt.

`Eigenpair` besitzt seine Vektordaten selbst. `Components` ist schreibgeschützt; `Eigenvector` liefert eine defensive Array-Kopie. Änderungen an dieser Kopie verändern das numerische Ergebnis nicht.

## Potenzmethode genauer betrachtet

Die Potenziteration multipliziert den aktuellen Vektor wiederholt mit der Matrix und normalisiert das Ergebnis. Unter passenden Voraussetzungen verlieren die Anteile zu betragsmäßig kleineren Eigenwerten relativ an Bedeutung, so dass sich die dominante Eigenrichtung durchsetzt.

Daraus folgt unmittelbar eine Einschränkung: Der Startvektor muss einen von Null verschiedenen Anteil in der gesuchten Eigenrichtung besitzen. SASD verwendet standardmäßig deterministisch den Einsvektor. Ist bekannt, dass dieser zur gesuchten Richtung orthogonal sein könnte, sollte ein eigener Startvektor angegeben werden:

```csharp
var result = EigenSolvers.PowerMethod(
    matrix,
    initialVector: new[] { 1.0, 0.2, -0.4 },
    tolerance: 1e-12);
```

Ein kleiner spektraler Abstand zwischen dem größten und zweitgrößten Betrag bedeutet häufig langsamere Konvergenz. Sind zwei dominante Beträge gleich, kann die einfache Potenzmethode ungeeignet sein.

Die Implementierung normalisiert skaliert. Deshalb scheitert ein sehr großer, aber endlicher Startvektor nicht allein daran, dass seine Komponenten bei einer naiven Normberechnung quadriert würden.

## Inverse Potenzmethode

Die unverschobene inverse Iteration löst wiederholt

`A * y = x`

und normalisiert `y`. Eigenrichtungen zu kleinen Eigenwertbeträgen werden durch die inverse Operation verstärkt. SASD faktorisiert `A` einmal per LU mit partieller Pivotisierung und verwendet diese Faktorisierung für alle Iterationen erneut.

```csharp
var smallest = EigenSolvers.InversePowerMethod(
    matrix,
    tolerance: 1e-12,
    maximumIterations: 200,
    pivotTolerance: 1e-14);
```

`pivotTolerance` gehört zur LU-Faktorisierung: Pivots bis zu dieser Schwelle werden als numerisch singulär behandelt. Dieser Wert ist nicht mit der Eigenwert-Konvergenztoleranz zu verwechseln.

Da die V1-Methode unverschoben ist, kann eine singuläre Matrix mit dieser API nicht behandelt werden. Die LU-Faktorisierung bricht bereits vor Beginn der Iteration ab. Eine spätere verschobene inverse Iteration sollte einen ausdrücklichen `shift`-Parameter erhalten.

## Wielandt-Deflation und zweites Eigenpaar

Die klassische Wielandt-Transformation entfernt über ein Rang-1-Update einen bereits bekannten Eigenwert. Die direkte API ist sinnvoll, wenn bereits ein approximiertes Eigenpaar vorliegt:

```csharp
var dominant = EigenSolvers.PowerMethod(matrix, tolerance: 1e-12);
if (!dominant.Converged)
{
    throw new InvalidOperationException("Dominantes Eigenpaar ist nicht konvergiert.");
}

var deflation = WielandtDeflation.Create(
    matrix,
    dominant.Value,
    residualTolerance: 1e-8);

Console.WriteLine(deflation.SourceResidual);
```

Das Quellresiduum wird geprüft, weil aus einem beliebigen Vektor-/Eigenwertpaar zwar formal eine Matrix konstruiert werden könnte, daraus aber keine verlässliche Deflation des gewünschten Eigenwerts folgen würde.

Für den kompletten historischen Ablauf gibt es:

```csharp
var second = EigenSolvers.WielandtSecondEigenpair(
    matrix,
    tolerance: 1e-11);
```

Dabei wird zunächst das dominante Paar berechnet, dann deflationiert, anschließend das dominante Paar der deflationierten Matrix gesucht und zuletzt der zweite Eigenvektor in die Koordinaten der Ausgangsmatrix zurückgeführt.

Bei mehrfachen oder nahezu mehrfachen dominanten Eigenwerten kann diese Rückabbildung schlecht konditioniert sein. Deshalb `second.Status` und `second.Message` auswerten und nicht jeden zurückgegebenen Wert automatisch als konvergiert betrachten.

## Vollständiges symmetrisches Eigensystem mit Jacobi

Für eine reelle symmetrische Matrix garantiert der Spektralsatz reelle Eigenwerte und eine orthonormale Eigenvektorbasis. Das zyklische Jacobi-Verfahren nutzt diese Struktur und reduziert die Nebendiagonalelemente durch wiederholte Rotationen.

```csharp
var decompositionResult = EigenSolvers.CyclicJacobi(
    matrix,
    tolerance: 1e-12,
    maximumSweeps: 100,
    symmetryTolerance: 1e-12);

if (!decompositionResult.Converged)
{
    Console.WriteLine(decompositionResult.Message);
}

var decomposition = decompositionResult.Value;
for (var i = 0; i < decomposition.Size; i++)
{
    var lambda = decomposition.GetEigenvalue(i);
    var vector = decomposition.GetEigenvector(i);
    var pair = decomposition.GetEigenpair(i);
    var equationResidual = EigenSolvers.EigenpairResidualNorm(matrix, pair);

    Console.WriteLine($"{i}: lambda={lambda}, Residuum={equationResidual}");
}
```

Wichtige Konventionen:

- Eigenwerte werden nach ihrem **numerischen Wert absteigend**, nicht nach dem Betrag sortiert;
- in `decomposition.Eigenvectors` stehen die Eigenvektoren spaltenweise;
- `GetEigenvalue` vermeidet eine komplette Eigenwert-Array-Kopie, wenn nur ein Skalar gebraucht wird;
- `GetEigenvector` liefert eine unabhängige Kopie;
- auch die öffentlichen Array-/Matrixeigenschaften der Zerlegung liefern defensive Kopien.

### Was bedeutet das Jacobi-`Residual`?

Bei `CyclicJacobi` ist `decompositionResult.Residual` die Frobenius-Norm der Nebendiagonale der transformierten Arbeitsmatrix. Sie beschreibt, wie weit die Jacobi-Diagonalisierung fortgeschritten ist.

Dies ist eine andere Diagnose als `||A*v-lambda*v||2`. Zur unabhängigen Prüfung der finalen Eigenpaare sollte deshalb `EigenpairResidualNorm` wie im Beispiel aufgerufen werden.

## Toleranzen sind Abbruchkriterien, keine garantierten Dezimalstellen

Eine Toleranz wie `1e-12` ist ein internes Abbruchkriterium des numerischen Verfahrens. Sie verspricht nicht zwölf korrekte Dezimalstellen. Skalierung, Kondition, Rundungsfehler, spektrale Abstände und die Struktur der Matrix spielen ebenfalls eine Rolle.

Bei schwierigeren Problemen sind folgende Prüfungen sinnvoll:

1. `Status`, `Iterations` und das verfahrensspezifische Residuum ansehen;
2. zusätzlich `EigenpairResidualNorm` berechnen;
3. bei vollständigen symmetrischen Zerlegungen bei Bedarf die Orthogonalität der Eigenvektoren prüfen;
4. bei vermuteter Empfindlichkeit eine andere gültige Toleranz oder einen anderen Startvektor ausprobieren;
5. für folgenschwere oder große Rechnungen mit einer unabhängigen etablierten Bibliothek gegenprüfen.

## Umfang und Performance

Die Verfahren sind für kleine und mittlere dichte Referenzprobleme, Lehre, deterministische SASD-Tests und spätere sprachübergreifende Konformitätsprüfungen gedacht. Sie verwenden absichtlich nachvollziehbare Schleifen statt Blockalgorithmen, SIMD-spezifischen Code oder native Abhängigkeiten.

Für große dichte, dünnbesetzte oder hochperformante Eigenwertprobleme sollte ein späteres SASD-Backend etablierte BLAS/LAPACK- bzw. spezialisierte Sparse-Bibliotheken verwenden und dabei klare SASD-Verträge beibehalten.

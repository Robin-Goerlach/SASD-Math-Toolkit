# Nullstellen von Gleichungen

Bei der Nullstellensuche wird ein Wert `x` gesucht, für den

`f(x) = 0`

gilt. Das SASD Math Toolkit trennt dabei bewusst drei verwandte Aufgaben: reelle skalare Gleichungen, komplexwertige Gleichungen und die Nullstellensuche bei Polynomen. Die öffentlichen APIs liefern einen Konvergenzstatus, statt so zu tun, als müsse jede mathematisch gültige Iteration zwangsläufig erfolgreich sein.

## Welches Verfahren passt zu welchem Problem?

| Situation | Sinnvoller Einstieg | Wichtigster Kompromiss |
|---|---|---|
| Eine reelle Nullstelle liegt nachweislich in einem Intervall mit Vorzeichenwechsel | `RootSolvers.Bisection` | Langsamer, aber bei gültiger Klammerung sehr robust |
| Gute Startnäherung und Ableitung sind vorhanden | `RootSolvers.NewtonRaphson` | Meist schnell, aber empfindlich gegenüber Startwert und kleinen Ableitungen |
| Keine Ableitung verfügbar, aber zwei brauchbare Startwerte | `RootSolvers.Secant` | Häufig schneller als Bisektion, aber ohne geschützte Klammerung |
| Eine Polynomnullstelle und ein brauchbarer Startwert | `PolynomialRootSolvers.NewtonHorner` | Effiziente gemeinsame Polynom-/Ableitungsauswertung, lokale Konvergenz |
| Eine allgemeine komplexe Funktion darf die reelle Achse verlassen | `ComplexRootSolvers.Muller` | Drei Startwerte; kann komplexe Nullstellen erreichen |
| Eine Polynomnullstelle soll im komplexen Bereich gefunden werden | `PolynomialRootSolvers.Laguerre` | Für Polynome spezialisiert; nutzt erste und zweite Ableitung |
| Alle Polynomnullstellen werden benötigt | `PolynomialRootSolvers.FindAllRootsLaguerre` | Wiederholte Laguerre-Suche, Deflation und abschließendes Polishing |

Es gibt kein universell bestes Verfahren. Eine reelle, geklammerte Nullstelle ist numerisch eine andere Aufgabe als das Bestimmen sämtlicher komplexer Nullstellen eines Polynoms.

## Gemeinsame Optionen und Ergebnisdaten

Die skalaren Verfahren sowie Muller, Newton-Horner und Laguerre verwenden `RootFindingOptions`:

```csharp
using Sasd.Numerics.RootFinding;

var options = new RootFindingOptions(
    Tolerance: 1e-12,
    MaximumIterations: 100);
```

`Tolerance` muss positiv sein und wird in der aktuellen Implementierung sowohl für das Residuum als auch für eine relativ skalierte Änderung des Iterationswerts verwendet. `MaximumIterations` ist eine Sicherheitsgrenze und keine Zusage, dass vorher Konvergenz eintreten muss.

Skalare Verfahren liefern `RootResult`. Besonders wichtig sind:

- `Root`: beste zurückgegebene Nullstellennäherung;
- `FunctionValue`: `f(Root)` an dieser Stelle;
- `Residual`: `abs(FunctionValue)`;
- `Iterations`: Anzahl vollständig ausgeführter Iterationen;
- `Status`: Grund für die Beendigung;
- `Converged`: Kurzform für `Status == IterationStatus.Converged`;
- `Message`: optionale Diagnosemeldung.

Komplexe Einzelnullstellenverfahren liefern `ComplexRootResult`; dort ist `Residual` der Betrag `|f(root)|`. `FindAllRootsLaguerre` gibt `PolynomialRootsResult` mit allen gefundenen Nullstellen, Gesamtiterationszahl und maximalem Residuum gegenüber dem ursprünglichen Polynom zurück.

Programmierfehler bzw. Vertragsverletzungen wie `NaN`-Eingaben oder ein ungültiges Intervall werden als Exceptions gemeldet. Erwartbare numerische Ergebnisse wie eine fehlende Bisektionsklammer, eine fast verschwindende Newton-Ableitung oder das Erreichen der Iterationsgrenze erscheinen dagegen im Ergebnisstatus.

## Bisektion: das robuste Intervallverfahren

Bisektion ist die erste Wahl, wenn eine stetige Funktion an den Endpunkten eines Intervalls unterschiedliche Vorzeichen besitzt. Für

`f(x) = cos(x) - x`

klammert `[0, 1]` die gesuchte Nullstelle:

```csharp
using Sasd.Numerics.RootFinding;

var result = RootSolvers.Bisection(
    x => Math.Cos(x) - x,
    left: 0.0,
    right: 1.0);

if (!result.Converged)
{
    Console.WriteLine($"Bisektion beendet mit {result.Status}: {result.Message}");
}
else
{
    Console.WriteLine($"Nullstelle = {result.Root:G17}");
    Console.WriteLine($"Residuum = {result.Residual:E3}");
}
```

Das Verfahren halbiert das Intervall wiederholt und behält jeweils die Hälfte, deren Endpunkte weiterhin unterschiedliche Vorzeichen besitzen. Der Fortschritt ist dadurch gut vorhersagbar und wesentlich weniger von einem geschickten Startwert abhängig als bei Newton- oder Sekanteniteration.

Haben beide Randwerte dasselbe Vorzeichen, liefert das Toolkit `IterationStatus.NotBracketed`. Es wird weder eine Nullstelle erfunden noch stillschweigend auf ein anderes Verfahren gewechselt. Gleiches Vorzeichen beweist nicht, dass im Intervall keine Nullstelle existiert; es bedeutet lediglich, dass die normale Bisektion keine gültige Vorzeichenwechselklammer besitzt.

## Newton-Raphson: schnell bei gutem lokalem Modell

Newton-Raphson verwendet die Tangente

`x(neu) = x - f(x) / f'(x)`.

Für dieselbe Gleichung:

```csharp
var result = RootSolvers.NewtonRaphson(
    x => Math.Cos(x) - x,
    x => -Math.Sin(x) - 1.0,
    initialGuess: 0.5);
```

In der Nähe einer einfachen Nullstelle konvergiert Newton häufig deutlich schneller als die Bisektion. Das Verfahren ist aber auf brauchbare lokale Ableitungsinformation angewiesen. Ein ungünstiger Startwert kann die Iteration von der gewünschten Nullstelle wegführen, und eine Ableitung nahe null macht den Newton-Schritt instabil. Das SASD Math Toolkit meldet diesen Fall mit `IterationStatus.NumericalBreakdown`, anstatt durch eine fast verschwindende Zahl zu dividieren.

Newton-Raphson ist sinnvoll, wenn die Ableitung zuverlässig und eine plausible Startnäherung vorhanden ist. Soll eine Intervallklammer unbedingt erhalten bleiben, ist Bisektion die sicherere Wahl; ein späteres hybrides Verfahren kann auf diesen Grundbausteinen aufsetzen.

## Sekantenverfahren: lokale Iteration ohne Ableitungsfunktion

Das Sekantenverfahren nähert die Ableitung aus zwei Funktionswerten an:

```csharp
var result = RootSolvers.Secant(
    x => Math.Cos(x) - x,
    firstGuess: 0.0,
    secondGuess: 1.0);
```

Es benötigt keine separate Ableitungsfunktion und konvergiert oft schneller als Bisektion. Anders als bei der Bisektion bilden die beiden Werte jedoch keine dauerhaft geschützte Klammer. Werden die beiden Funktionswerte nahezu gleich, ist die Sekantensteigung numerisch nicht mehr brauchbar; das Ergebnis meldet dann `NumericalBreakdown`.

## Polynome und Horner-Schema

Die Polynom-APIs verwenden `Sasd.Numerics.Polynomials.Polynomial`. Die Koeffizienten werden in **absteigender Potenzreihenfolge** angegeben. Beispiel:

```csharp
using Sasd.Numerics.Polynomials;

// x^3 - 2x - 5
var polynomial = new Polynomial([1.0, 0.0, -2.0, -5.0]);
```

Diese Reihenfolge ist wichtig. `[1, 0, -2, -5]` bedeutet nicht „Konstante zuerst“.

`PolynomialRootSolvers.NewtonHorner` verbindet die Newton-Iteration mit erweiterter Horner-Auswertung. Polynomwert und erste Ableitung werden dabei in einem gemeinsamen Durchlauf bestimmt:

```csharp
using System.Numerics;
using Sasd.Numerics.RootFinding;

var result = PolynomialRootSolvers.NewtonHorner(
    polynomial,
    initialGuess: new Complex(2.0, 0.0));
```

Nach einer gefundenen Nullstelle kann `Polynomial.Deflate(root)` den zugehörigen linearen Faktor entfernen. Das Deflationsergebnis enthält Quotientenpolynom und Rest. Ein großer Rest ist ein Hinweis darauf, dass der übergebene Wert keine ausreichend genaue Nullstelle war.

## Muller: von reellen Startwerten in die komplexe Ebene

Das Muller-Verfahren legt durch drei Punkte lokal ein quadratisches Modell. Da die Diskriminante komplex ausgewertet wird, kann eine auf der reellen Achse beginnende Folge von selbst in die komplexe Ebene wechseln.

Für `f(z) = z^2 + 1`:

```csharp
using System.Numerics;

static Complex Function(Complex z) => (z * z) + Complex.One;

var result = ComplexRootSolvers.Muller(
    Function,
    Complex.Zero,
    Complex.One,
    new Complex(2.0, 0.0));
```

Ein konvergiertes Ergebnis sollte nahe `+i` oder `-i` liegen. Welche Nullstelle erreicht wird, hängt vom numerischen Weg ab. Ein Einzelnullstellenverfahren garantiert bei mehreren Lösungen nicht ohne zusätzliche Voraussetzungen eine bestimmte davon.

## Laguerre für Polynomnullstellen

Laguerres Verfahren ist auf Polynome zugeschnitten und verwendet Polynomwert sowie erste und zweite Ableitung. Es eignet sich besonders, wenn Nullstellen komplex sein können:

```csharp
var quartic = new Polynomial([1.0, 0.0, 0.0, 0.0, 1.0]); // x^4 + 1
var oneRoot = PolynomialRootSolvers.Laguerre(quartic, Complex.One);
```

Werden alle Nullstellen benötigt, sollte die höhere API verwendet werden, statt selbst eine Deflationsschleife zu schreiben:

```csharp
var polynomial = new Polynomial([1.0, 0.0, 0.0, 0.0, -1.0]); // x^4 - 1
var allRoots = PolynomialRootSolvers.FindAllRootsLaguerre(polynomial);

if (allRoots.Converged)
{
    foreach (var root in allRoots.Roots)
    {
        Console.WriteLine(root);
    }

    Console.WriteLine($"maximales Residuum = {allRoots.MaximumResidual:E3}");
}
```

Die aktuelle All-Roots-Implementierung ist deterministisch. Sie verwendet wiederholte Laguerre-Suchen, synthetische Deflation, feste Ausweichstartwerte auf Basis einer Cauchy-Nullstellenschranke und danach ein Polishing gegenüber dem ursprünglichen Polynom. Dieses Polishing ist wichtig, weil sich Rundungsfehler bei aufeinanderfolgenden Deflationen aufsummieren können.

## Konvergenz richtig interpretieren

Ein kleiner Iterationsschritt und ein kleines Residuum sind verwandte, aber nicht identische Aussagen. Die aktuellen Nullstellensolver akzeptieren Konvergenz, wenn entweder das Funktionsresiduum innerhalb der Toleranz liegt oder die Änderung zwischen zwei Iterationswerten relativ zur Größenordnung ausreichend klein geworden ist. Bei wichtigen Rechnungen sollte deshalb neben `Converged` immer auch `Residual` betrachtet werden.

Eine konvergierte Nullstelle ist außerdem nicht automatisch genau die gewünschte Nullstelle. Funktionen können mehrere oder mehrfache Nullstellen besitzen; Startwerte beeinflussen Newton, Sekante, Muller und Laguerre. Die Bisektion bietet eine stärkere Lokalisierung, weil die gesuchte Nullstelle in einem Vorzeichenwechselintervall eingeschlossen bleibt – allerdings nur bei einer gültigen reellen Klammerung.

## Sinnvoller Arbeitsablauf

Zuerst sollte geklärt werden, ob eine allgemeine reelle Gleichung, eine komplexe Gleichung oder ein Polynom vorliegt. Gibt es für ein reelles Problem ein Intervall mit sicherem Vorzeichenwechsel, eignet sich die Bisektion gut als robuste Referenz. Newton oder Sekante sind sinnvoll, wenn Geschwindigkeit wichtiger ist und brauchbare Startinformationen vorhanden sind. Bei Polynomen sollten die spezialisierten Verfahren bevorzugt werden, weil sie Horner-Auswertung, Deflation und All-Roots-Unterstützung wiederverwenden. Für nachvollziehbare numerische Ergebnisse sollten Terminierungsstatus, Residuum und verwendete Toleranzen immer mit dokumentiert werden.

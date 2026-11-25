# Numerische Rezepte, Diagnostik und typische Fehlerfälle

Eine numerische Routine liefert selten nur eine Zahl. Eine belastbare Anwendung muss zusätzlich wissen, **wie das Verfahren beendet wurde**, **wie gut der zurückgegebene Wert das mathematische Problem erfüllt**, **welche Toleranz verwendet wurde** und **ob das Problem selbst möglicherweise schlecht konditioniert ist**.

Dieses Kapitel führt die Diagnosekonventionen des SASD Math Toolkit an einer Stelle zusammen. Es ersetzt die Fachkapitel nicht, sondern liefert eine gemeinsame Lesart für Ergebnisse aus Nullstellensuche, Integration, linearer Algebra, Eigenwertverfahren, ODE-Solvern und Approximation.

## 1. Vier Fragen vor der Übernahme eines numerischen Ergebnisses

Bevor ein Rückgabewert als brauchbar gilt, sollten vier getrennte Fragen beantwortet werden:

1. **War der Aufruf gültig?** Falsche Dimensionen, nicht-endliche Eingaben, unmögliche Optionen oder ein ungültiges Intervall sind Vertragsverletzungen und führen normalerweise zu einer .NET-Exception.
2. **Wie wurde das Verfahren beendet?** Iterative und adaptive Routinen liefern Statuswerte wie `Converged`, `MaximumIterationsReached`, `NumericalBreakdown`, `NotBracketed`, `MaximumDepthReached` oder `MinimumStepSizeReached`.
3. **Wie groß ist der Defekt?** Ein Residuum oder eine Fehlerschätzung sagt etwas über die Näherung aus, aber seine Bedeutung ist problemspezifisch.
4. **Ist das Problem für die gewünschte Interpretation ausreichend gut konditioniert?** Ein kleines Residuum bedeutet nicht automatisch einen kleinen Fehler in der gesuchten Größe.

Diese Fragen sind in der API absichtlich getrennt. Ein Status ist kein Residuum, ein Residuum ist kein Vorwärtsfehler, und eine Toleranz ist keine Garantie für eine bestimmte Zahl korrekter Dezimalstellen.

## 2. Exceptions und Ergebnisstatus haben unterschiedliche Aufgaben

Für die C#/.NET-Implementierung gilt folgende Grundregel:

- **Ungültige API-Aufrufe oder ungültige numerische Eingaben** werden mit normalen .NET-Exceptions wie `ArgumentException` oder `ArgumentOutOfRangeException` abgelehnt.
- **Erwartbare numerische Ergebnisse nach einem gültigen Aufruf** werden normalerweise über Ergebnisstatus gemeldet.
- **Direkte Arithmetik ohne sinnvolles endliches Ergebnis** kann `ArithmeticException` auslösen, wenn kein nützlicher iterativer Status zurückgegeben werden kann.

Ein Bisection-Intervall ohne Vorzeichenwechsel ist beispielsweise ein mathematisch sinnvoller Ausgang. `RootResult.Status` meldet deshalb `IterationStatus.NotBracketed`. Eine Eingabe `double.NaN` ist dagegen ein ungültiger Aufruf und wird bereits vor Beginn der Iteration abgelehnt.

Für Anwendungscode ist diese Trennung wichtig. Exceptions sollten nicht der normale Kontrollfluss für „nicht konvergiert“ sein; umgekehrt sollten Exceptions nicht so behandelt werden, als seien sie lediglich ein weiterer Konvergenzstatus.

## 3. `Converged` ist eine Beendigungsinformation, kein Exaktheitsbeweis

Das generische `IterativeResult<T>` enthält `Status`, `Iterations`, `Value`, ein optionales `Residual` und eine optionale Diagnose `Message`.

```csharp
var result = LinearSystemSolvers.GaussSeidel(
    matrix,
    rightHandSide,
    tolerance: 1e-10,
    maximumIterations: 500);

if (!result.Converged)
{
    Console.WriteLine($"Abbruch mit {result.Status}: {result.Message}");
}

if (result.HasFiniteResidual)
{
    Console.WriteLine($"Residuum = {result.Residual:E3}");
}
```

`Converged` bedeutet, dass das Verfahren **sein dokumentiertes Konvergenzkriterium** erfüllt hat. Das Ergebnis ist deshalb nicht mathematisch exakt. Konditionierung, Gleitkommarundung, Modellfehler, Diskretisierungsfehler und ungeeignete Skalierung können weiterhin relevant sein.

Umgekehrt kann ein nicht konvergiertes Ergebnis durchaus eine brauchbare letzte Näherung und ein endliches Residuum enthalten. Die neue Eigenschaft `HasFiniteResidual` ist deshalb bewusst unabhängig von `Converged`.

## 4. Residuum, Vorwärtsfehler und Fehlerschätzung sind nicht dasselbe

### Nullstellensuche

Für eine skalare Nullstellennäherung `x` ist `RootResult.Residual`

`|f(x)|`.

Ein kleiner Wert zeigt, dass `x` die Gleichung `f(x)=0` gut erfüllt. Er misst jedoch nicht direkt `|x-x*|` zur exakten Nullstelle `x*`. In der Nähe mehrfacher Nullstellen oder bei sehr kleiner Ableitung kann ein kleines Funktionsresiduum mit einem deutlich größeren Fehler in `x` zusammenfallen.

```csharp
var root = RootSolvers.NewtonRaphson(
    x => Math.Cos(x) - x,
    x => -Math.Sin(x) - 1.0,
    0.5);

Console.WriteLine(root.Status);
Console.WriteLine(root.Root);
Console.WriteLine(root.Residual);
```

### Lineare Gleichungssysteme

Für `A*x=b` berechnet `LinearSystemSolvers.ResidualInfinityNorm`

`||A*x-b||∞`.

Das ist ein Gleichungsdefekt und keine direkte Schranke für `||x-x*||`. Bei einer schlecht konditionierten Matrix kann ein kleines Residuum trotzdem zu einer empfindlichen Lösung gehören.

```csharp
var x = LinearSystemSolvers.SolveGaussian(matrix, rightHandSide);
var residual = LinearSystemSolvers.ResidualInfinityNorm(matrix, x, rightHandSide);
```

Bei direkten Solvern ist diese nachträgliche Residuenberechnung ein sinnvoller unabhängiger Anwendungstest, weil der direkte Solver selbst kein iteratives Ergebnisobjekt zurückgibt.

### Eigenpaare

Für `A*v=lambda*v` berechnet `EigenSolvers.EigenpairResidualNorm`

`||A*v-lambda*v||2`.

```csharp
var result = EigenSolvers.PowerMethod(matrix);
var residual = EigenSolvers.EigenpairResidualNorm(matrix, result.Value);
```

Ein kleines Eigenpaarresiduum bestätigt die Eigenwertgleichung in absolutem Sinn. Es sagt jedoch nicht, dass der Eigenwert unempfindlich gegenüber Störungen ist. Mehrfache oder eng benachbarte Eigenwerte können insbesondere einzelne Eigenvektoren grundsätzlich empfindlich machen.

### Adaptive Integration

`AdaptiveIntegrationResult.EstimatedError` und `RombergIntegrationResult.EstimatedError` sind **algorithmische Fehlerindikatoren**, die aus Verfeinerung bzw. Extrapolation entstehen. Sie sind kein Vergleich mit einem dem Programm unbekannten exakten Integral.

Wenn eine analytische Lösung oder eine deutlich genauere unabhängige Referenz vorhanden ist, sollte sie zusätzlich verwendet werden.

### Adaptive ODE-Integration

Runge-Kutta-Fehlberg steuert eine **lokale Schrittfehlerschätzung**. Eine erfolgreiche lokale Fehlerkontrolle macht die gesamte berechnete Trajektorie nicht exakt. `AdaptiveOdeResult` meldet deshalb, ob der Endpunkt erreicht wurde und wie viele Versuchsschritte akzeptiert oder verworfen wurden. Für wichtige Anwendungen können zusätzlich Invarianten, analytische Lösungen oder unabhängige feinere Rechnungen geprüft werden.

## 5. Eine Toleranz muss zur Problemskala passen

Eine Zahl wie `1e-10` hat allein keine universelle Bedeutung.

Bei einem Problem mit natürlichen Werten um `1e-6` und einem anderen um `1e12` beschreibt dieselbe absolute Schwelle völlig unterschiedliche Anforderungen. Einige Toolkit-Verfahren verwenden bewusst absolute Toleranzen, weil dies der klarste historische bzw. Referenzvertrag ist. Adaptives RKF bietet dagegen getrennte absolute und relative Toleranzen, weil dort beide benötigt werden.

Praktisch gilt:

- In der Methodendokumentation nachsehen, **welche Größe mit der Toleranz verglichen wird**.
- Toleranzen aus der physikalischen oder mathematischen Skala des Problems ableiten.
- `NumericConstants.DefaultTolerance` nicht als Zusage einer festen Anzahl richtiger Dezimalstellen interpretieren.
- `NumericConstants.NearlyZero` nicht als allgemeine Gleitkomma-Gleichheitsprüfung verwenden.

`NearlyZero` ist ein Schutzwert der Referenzimplementierung für numerisch vernachlässigbare Pivots, Divisoren und Normen. Öffentliche APIs wie LU-Faktorisierung und inverse Iteration besitzen eine explizite Pivot-Toleranz, wenn die Anwendung diese Entscheidung an ihre Problemskala anpassen muss.

## 6. Häufige Beendigungsstatus und sinnvolle Reaktionen

### `MaximumIterationsReached`

Das Verfahren blieb numerisch sinnvoll, erfüllte sein Kriterium aber nicht innerhalb des Limits.

Sinnvolle Fragen sind: Ist die Toleranz realistisch? Ist der Startwert schlecht? Konvergiert das Verfahren grundsätzlich langsam? Passt das Verfahren zum Problem? Wird das Residuum noch kleiner?

Einfach nur das Iterationslimit zu erhöhen ist erst dann sinnvoll, wenn diese Fragen geklärt sind.

### `NumericalBreakdown`

Eine notwendige numerische Voraussetzung ist verloren gegangen: ein Divisor wurde vernachlässigbar klein, ein Iterat nicht-endlich, eine Zwischenrechnung lief über oder eine andere Invariante ging verloren.

Nicht automatisch mit sehr viel lockerer Toleranz neu starten. Zuerst Diagnosemeldung und Skalierung prüfen. Ein Breakdown weist häufig auf ein strukturelles Problem hin.

### `NotBracketed`

Ein bracketing-basiertes Nullstellenverfahren erhielt ein Intervall ohne erforderlichen Vorzeichenwechsel. Ein besseres Intervall wählen oder mit Fachwissen nach einer Klammer suchen. Dieser Status bedeutet nicht „die Funktion besitzt nirgends eine Nullstelle“.

### `MaximumDepthReached` / `NumericalResolutionReached`

Adaptive Quadratur konnte die normale Verfeinerung nicht fortsetzen. Der Rückgabewert ist die beste am Grenzpunkt verfügbare Näherung, aber `Converged` ist falsch. Ursachen können Singularitäten, Unstetigkeiten, starke Oszillation oder eine unrealistische Toleranz sein.

### `MinimumStepSizeReached`

Die adaptive ODE-Integration müsste unter die konfigurierte minimale Schrittweite gehen, um die lokale Fehlertoleranz einzuhalten. Toleranz, Mindestschritt und Verhalten der Differentialgleichung prüfen. Das kann auch auf Steifheit hindeuten; RKF45 ist kein spezieller steifer Solver.

### Singuläre oder numerisch singuläre Matrix

Direkte Verfahren können `ArithmeticException` auslösen, wenn ein Pivot unter die konfigurierte Schwelle fällt. Zu unterscheiden sind eine tatsächlich singuläre Matrix, eine stark schlecht konditionierte Matrix und eine Matrix, deren Skalierung nicht zur gewählten absoluten Pivot-Toleranz passt.

## 7. Nicht-endliche Werte sind Fehlerzustände, keine normalen Daten

Die numerischen V1-Verfahren erwarten endliche Gleitkommawerte, sofern eine API nichts anderes dokumentiert. `NaN` und Unendlich werden an öffentlichen Grenzen soweit sinnvoll abgelehnt. Liefert ein Callback der Anwendung während einer Rechnung einen nicht-endlichen Wert, meldet das Verfahren je nach Vertrag einen numerischen Breakdown oder eine Arithmetic-Exception.

Diese Politik ist beabsichtigt. Eine stillschweigende `NaN`-Fortpflanzung über viele Iterationen kann einen formal abgeschlossenen Ablauf erzeugen, ohne dass noch eine brauchbare Diagnose möglich ist.

Auch Anwendungen sollten importierte Daten vor der Rechnung prüfen und genügend Kontext aufbewahren, um die ursprüngliche Zeile, Probe, Messung oder den Parameter zu identifizieren.

## 8. Konditionierung ist etwas anderes als algorithmische Konvergenz

Ein stabiles Verfahren kann ein schlecht konditioniertes Problem nicht gut konditioniert machen.

Beispiele:

- nahezu singuläre lineare Systeme können große Änderungen in `x` aus kleinen Änderungen in `A` oder `b` erzeugen;
- mehrfache oder eng benachbarte Polynomnullstellen reagieren empfindlich auf Koeffizientenänderungen;
- mehrfache oder geclusterte Eigenwerte können einzelne Eigenvektoren instabil machen;
- numerische Differentiation verstärkt Rauschen;
- hochgradige Polynominterpolation kann ein schlechtes Modell sein, obwohl die Interpolationsgleichungen exakt gelöst wurden.

„Der Solver ist konvergiert“ ist deshalb nur ein Teil der Qualitätsaussage.

## 9. Unabhängige Prüfrezepte

Für wichtige Rechnungen sollte die zweite Prüfung möglichst nicht nur dasselbe Abbruchkriterium wiederholen.

### Nullstelle

Originalfunktion an der gemeldeten Nullstelle auswerten und `RootResult.Residual` protokollieren. Wenn eine Ableitung verfügbar ist, prüfen, ob die Funktion dort nahezu flach ist.

### Direktes lineares Gleichungssystem

`||A*x-b||∞` mit `ResidualInfinityNorm` neu berechnen. Bei sensitiven Anwendungen zusätzlich eine anders skalierte Formulierung oder eine unabhängige hochwertige Linear-Algebra-Bibliothek vergleichen.

### Eigenpaar

`EigenpairResidualNorm` auf der Originalmatrix verwenden. Bei einer vollständigen symmetrischen Zerlegung zusätzlich Orthogonalität und gegebenenfalls Rekonstruktion von `A` prüfen.

### Integration

Bei schwierigen Integranden mit strengerer Toleranz oder einem anderen Verfahren wiederholen. Die Übereinstimmung verschiedener Verfahren ist stärker als nur ein größeres Rekursionslimit desselben Verfahrens.

### ODE

Bekannte Invarianten, analytische Lösung, feinere Schrittweite oder unabhängigen Solver vergleichen. Bei langen Integrationen den Fehlerverlauf untersuchen und nicht nur den letzten Punkt.

### Least Squares

Residuen im **ursprünglichen Datenraum** betrachten, nicht nur in transformierten Koordinaten. Die benannten transformierten SASD-Modelle berichten ihre Diagnostik deshalb im ursprünglichen `y`-Raum.

## 10. Ein praktisches Abnahmemuster

```csharp
var result = EigenSolvers.PowerMethod(
    matrix,
    tolerance: 1e-10,
    maximumIterations: 500);

if (!result.Converged)
{
    throw new InvalidOperationException(
        $"Eigenwertberechnung endete mit {result.Status}: {result.Message}");
}

var residual = EigenSolvers.EigenpairResidualNorm(matrix, result.Value);
if (residual > 1e-8)
{
    throw new InvalidOperationException(
        $"Eigenpaarresiduum {residual:E3} überschreitet die Anwendungsgrenze.");
}

var eigenpair = result.Value;
```

Wichtig ist die Trennung zwischen **Algorithmustoleranz** und **Abnahmegrenze der Anwendung**. Die Bibliothek steuert ihren numerischen Prozess; die Anwendung entscheidet, ob das Ergebnis für ihren fachlichen, wissenschaftlichen oder technischen Zweck gut genug ist.

## 11. Was protokolliert werden sollte

Für später reproduzierbare numerische Ergebnisse sollten mehr Daten als nur der Endwert gespeichert werden:

- Algorithmusname und relevante Optionen;
- Eingabeskala oder Datensatzidentität;
- Beendigungsstatus;
- Anzahl Iterationen, Verfeinerungen oder Schritte;
- Residuum bzw. Fehlerschätzung, wenn vorhanden;
- Diagnosemeldung;
- Toolkit-Version bzw. Commit bei Forschungs- oder Validierungsabläufen.

Nur `success=true` zu speichern verwirft genau die Informationen, die bei späteren Abweichungen am hilfreichsten sind.

## 12. Kurze Prüfliste

Vor der Übernahme eines Ergebnisses prüfen:

- Eingaben sind endlich und erfüllen den API-Vertrag;
- der Beendigungsstatus ist verstanden;
- Residuum/Fehlerindikator hat die erwartete Definition und Skalierung;
- die Toleranz passt zum Problem;
- Iterations-, Tiefen- oder Schrittgrenzen haben das Verfahren nicht verfrüht beendet;
- keine Diagnosemeldung weist auf einen Breakdown hin;
- Konditionierung oder Datenrauschen entwerten die Interpretation nicht;
- bei wichtigen Ergebnissen existiert eine unabhängige Prüfung.

Diese Diagnosehaltung ist bewusst Teil der Architektur des SASD Math Toolkit. V1 bevorzugt explizites, prüfbares numerisches Verhalten gegenüber APIs, die nur eine nackte Zahl zurückgeben und verbergen, wie sie entstanden ist.

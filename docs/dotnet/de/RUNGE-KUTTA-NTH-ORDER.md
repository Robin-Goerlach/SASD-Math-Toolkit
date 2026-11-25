# RK4 für skalare Differentialgleichungen n-ter Ordnung

## Zweck

`RungeKutta.FourthOrderNthOrder` löst skalare Anfangswertprobleme beliebiger Ordnung der Form

`y^(n) = g(x, y, y', ..., y^(n-1))`.

Der Aufrufer übergibt den Anfangszustand

`[y(x0), y'(x0), ..., y^(n-1)(x0)]`.

Damit wird der historische V1-Komfortpunkt für RK4 bei Gleichungen n-ter Ordnung mit einer modernen C#-API abgeschlossen. Die Implementierung ist neuer SASD-Code auf Grundlage der mathematischen Standardreduktion auf ein System erster Ordnung; historischer Borland-Quelltext oder Handbuchtext wird nicht übernommen.

## Reduktion auf ein Begleitsystem erster Ordnung

Wir definieren die Zustandskomponenten

`z0 = y`

`z1 = y'`

`...`

`z(n-1) = y^(n-1)`.

Dann wird die skalare Gleichung n-ter Ordnung zum System

`z0' = z1`

`z1' = z2`

`...`

`z(n-2)' = z(n-1)`

`z(n-1)' = g(x, z0, z1, ..., z(n-1))`.

`FourthOrderNthOrder` erzeugt genau dieses Begleitsystem und delegiert die numerische Arbeit an denselben RK4-Systemkern, der bereits von den APIs erster und zweiter Ordnung verwendet wird. Dadurch gibt es nur eine RK4-Stufenimplementierung, die gepflegt und getestet werden muss.

## API-Vertrag

Der Callback `highestDerivative` erhält das aktuelle `x` sowie einen schreibgeschützten Zustand in fester Reihenfolge:

`[y, y', y'', ..., y^(n-1)]`.

Die Anzahl der Elemente in `initialState` definiert `n`. Der Zustand darf nicht leer sein und alle Anfangswerte müssen endlich sein.

Ein Problem dritter Ordnung benötigt beispielsweise

`initialState: [y0, yPrime0, yDoublePrime0]`.

Der Callback liefert für diesen Zustand nur die höchste Ableitung `y'''` zurück.

## Ergebnismodell

Jeder zurückgegebene `NthOrderOdePoint` enthält:

- `X` — den Wert der unabhängigen Variablen;
- `Y` — die Kurzform für Ableitungsordnung null;
- `Order` — die Anzahl der Zustandskomponenten und damit die Ordnung der Gleichung;
- `State` — einen schreibgeschützten Snapshot `[y, y', ..., y^(n-1)]`;
- `GetDerivative(k)` — direkten Zugriff über die mathematische Ableitungsordnung.

Das Ergebnisobjekt kopiert den vom Solver erzeugten Zustand. Eine spätere Veränderung eines Arrays kann deshalb eine bereits zurückgegebene Trajektorie nicht unbemerkt verändern.

## Beispiel: Gleichung dritter Ordnung mit Sinuslösung

Betrachtet wird

`y''' = -y'`

mit

`y(0)=0`, `y'(0)=1`, `y''(0)=0`.

Die exakte Lösung lautet `y=sin(x)`.

```csharp
var points = RungeKutta.FourthOrderNthOrder(
    (_, state) => -state[1],
    x0: 0.0,
    initialState: [0.0, 1.0, 0.0],
    xEnd: Math.PI / 2.0,
    step: 0.01);

var final = points[^1];
Console.WriteLine(final.Y);                // ungefähr 1
Console.WriteLine(final.GetDerivative(1)); // ungefähr 0
Console.WriteLine(final.GetDerivative(2)); // ungefähr -1
```

## Schrittweite und Validierung

Das Verfahren verwendet die angeforderte feste nominale RK4-Schrittweite und verkürzt nur den letzten Schritt, wenn dies nötig ist, um exakt bei `xEnd` zu enden. Der gemeinsame RK4-Kern prüft, dass die Dimension des Begleitsystems erhalten bleibt und keine nicht-endlichen Zwischenwerte auftreten.

Ungültige Konfigurationen wie ein leerer Anfangszustand, eine nicht-positive Schrittweite, nicht-endliche Anfangswerte oder ein Endpunkt vor beziehungsweise am Startpunkt werden als gewöhnliche Argumentfehler gemeldet. Eine während der Integration entstehende nicht-endliche Ableitung ist dagegen ein numerischer Fehler und führt zu einer `ArithmeticException`.

## Umfang

Dies ist eine Komfort-API für eine einzelne skalare Gleichung n-ter Ordnung. Gekoppelte Systeme erster Ordnung verwenden bereits `FourthOrderSystem`; eine eigene Komfort-API für gekoppelte Systeme zweiter Ordnung bleibt ein separater V1-Meilenstein. Spezielle Verfahren für steife Probleme, adaptive Wrapper für n-te Ordnung, Ereigniserkennung und Dense Output bleiben spätere Erweiterungen und werden nicht versteckt in diese Referenzimplementierung eingebaut.

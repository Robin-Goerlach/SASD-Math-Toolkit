# Lineares Shooting für Randwertprobleme zweiter Ordnung

## Zweck

`LinearShooting.Solve` löst skalare lineare Dirichlet-Randwertprobleme der Form

`y'' = p(x) y' + q(x) y + r(x)`

mit vorgegebenen Randwerten

`y(x0) = alpha` und `y(xEnd) = beta`.

Die Implementierung ist eine eigenständige C#-Umsetzung des mathematischen Shooting-Verfahrens. Der historische Borland-Katalog dient nur als funktionales V1-Ziel; historischer Quelltext oder Handbuchtext wird nicht übernommen.

## Unterschied zum Anfangswertproblem

RK4 benötigt normalerweise `y(x0)` und `y'(x0)`. Beim Dirichlet-Randwertproblem sind dagegen `y(x0)` und `y(xEnd)` bekannt. Die Anfangssteigung fehlt. Beim Shooting wird genau diese Steigung so bestimmt, dass die integrierte Anfangswertlösung den gewünschten rechten Randwert trifft.

Bei einer **linearen** Gleichung hängt der rechte Randwert linear von der unbekannten Anfangssteigung ab. Deshalb genügen zwei RK4-Integrationen; eine iterative Nullstellensuche ist noch nicht erforderlich.

## Die beiden Hilfsprobleme

Der Solver berechnet zunächst eine partikuläre Lösung `u`:

`u'' = p u' + q u + r`, `u(x0)=alpha`, `u'(x0)=0`

und eine homogene Sensitivitätslösung `v`:

`v'' = p v' + q v`, `v(x0)=0`, `v'(x0)=1`.

Die gesuchte Lösung entsteht dann als

`y = u + c v`

mit

`c = (beta - u(xEnd)) / v(xEnd)`.

Durch die gewählten Anfangswerte der Hilfslösungen ist `c` zugleich die rekonstruierte Anfangssteigung `y'(x0)`.

Beide Hilfsprobleme werden mit `RungeKutta.FourthOrderSecondOrder` integriert. Das Shooting-Verfahren dupliziert also keine RK4-Stufenformeln.

## Ergebnismodell

`LinearShootingResult` stellt bereit:

- `Points` — unveränderliche Trajektorienpunkte mit `x`, `y` und `y'`;
- `InitialSlope` — die durch die Randbedingung bestimmte Anfangssteigung;
- `FinalPoint` — bequemer Zugriff auf den rechten Endpunkt;
- `RightBoundaryResidual` — berechnetes `y(xEnd) - beta`;
- `AuxiliaryRightValue` — der Nenner `v(xEnd)` der Shooting-Korrektur.

Das Residuum ist hilfreich beim Vergleich verschiedener Schrittweiten. Ein sehr kleines Randresiduum bedeutet jedoch nicht automatisch, dass die gesamte innere Trajektorie entsprechend genau ist; es ist keine vollständige globale Fehlerschätzung.

## Singuläre oder schlecht konditionierte Randabbildung

Die Korrektur teilt durch `v(xEnd)`. Ist dieser Wert null, kann die vorgegebene rechte Randbedingung die fehlende Anfangssteigung mit dieser Shooting-Konstruktion nicht bestimmen. Ein sehr kleiner Wert ist aus demselben Grund numerisch gefährlich.

`singularityTolerance` ist deshalb eine explizite absolute Schwelle. Standardwert ist `1e-12`; bei ungewöhnlich skalierten Problemen sollte eine zur Anwendung passende Schwelle gewählt werden.

## Beispiel

Für

`y'' = 2`, `y(0)=1`, `y(1)=4`

lautet die exakte Lösung `y = 1 + 2x + x^2`; die fehlende Anfangssteigung ist 2.

```csharp
var result = LinearShooting.Solve(
    _ => 0.0, // p(x)
    _ => 0.0, // q(x)
    _ => 2.0, // r(x)
    x0: 0.0,
    leftValue: 1.0,
    xEnd: 1.0,
    rightValue: 4.0,
    step: 0.05);

Console.WriteLine(result.InitialSlope);          // ungefähr 2
Console.WriteLine(result.FinalPoint.Y);          // ungefähr 4
Console.WriteLine(result.RightBoundaryResidual); // nahe null
```

## Umfang und Grenzen

Die aktuelle API behandelt skalare lineare explizite Gleichungen zweiter Ordnung mit Wertvorgaben an beiden Intervallenden. Sie ist noch kein allgemeines Framework für Robin-/Neumann-Randbedingungen und auch noch nicht der nichtlineare Shooting-Solver.

Der nächste historische V1-Meilenstein im Randwertbereich ist das nichtlineare Shooting. Dort hängt der rechte Endwert nichtlinear von der geratenen Anfangssteigung ab, sodass eine iterative Nullstellensuche erforderlich wird.

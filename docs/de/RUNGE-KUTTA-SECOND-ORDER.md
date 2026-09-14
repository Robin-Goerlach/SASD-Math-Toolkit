# RK4 für skalare Differentialgleichungen zweiter Ordnung

## Zweck

`RungeKutta.FourthOrderSecondOrder` löst skalare Anfangswertprobleme zweiter Ordnung der Form

`y'' = g(x, y, y')`

mit Anfangswerten sowohl für `y(x0)` als auch für `y'(x0)`.

Damit ist der historische V1-Komfortbaustein für RK4 und Differentialgleichungen zweiter Ordnung abgedeckt. Die Implementierung ist eigenständig aus dem mathematischen Verfahren entwickelt und übernimmt weder historischen Borland-Quelltext noch Handbuchtext.

## Rückführung auf ein System erster Ordnung

Für eine Gleichung zweiter Ordnung ist keine zweite Runge-Kutta-Formel nötig. Man setzt

`v = y'`.

Damit entsteht das System

`y' = v`

`v' = g(x, y, v)`.

Die SASD-API führt genau diese Transformation aus und verwendet anschließend denselben RK4-Kern für Systeme erster Ordnung. Diese Entscheidung ist bewusst architektonisch: Es gibt nur eine System-RK4-Implementierung, die getestet und gepflegt werden muss. Die API für zweite Ordnung ist eine typsichere Komfortschicht darüber.

## Ergebnismodell

Jeder zurückgegebene `SecondOrderOdePoint` enthält:

- `X` – den Wert der unabhängigen Variablen;
- `Y` – den Lösungswert `y`;
- `FirstDerivative` – den gleichzeitig integrierten Wert `y'`.

Die erste Ableitung gehört zum Zustand eines Anfangswertproblems zweiter Ordnung und ist beispielsweise bei mechanischen Modellen als Geschwindigkeit oder Winkelgeschwindigkeit unmittelbar relevant.

## Schrittweitenverhalten

Das Verfahren verwendet die vorgegebene feste nominale RK4-Schrittweite. Ist der Rest bis `xEnd` kürzer, wird nur der letzte Schritt verkürzt, damit der gewünschte Endpunkt exakt erreicht wird.

Das unterscheidet sich bewusst vom Adams-Mehrschrittverfahren, bei dem gleiche Abstände mathematisch erforderlich sind und deshalb das gesamte Gitter angepasst wird.

## Validierung und numerische Fehler

Anfangswerte, Endpunkt und Schrittweite werden explizit validiert. Der gemeinsam verwendete RK4-Systemkern prüft zusätzlich die Dimension der Ableitungsvektoren und darauf, dass Zwischenwerte endlich bleiben. Nicht endliche Ableitungen oder Zustände werden gemeldet, statt `NaN` oder unendlich unbemerkt in die restliche Trajektorie zu übernehmen.

## Beispiel

Für den harmonischen Oszillator

`y'' = -y`, `y(0)=0`, `y'(0)=1`

lautet die exakte Lösung `y=sin(x)` und `y'=cos(x)`.

```csharp
var points = RungeKutta.FourthOrderSecondOrder(
    (_, y, _) => -y,
    x0: 0.0,
    y0: 0.0,
    firstDerivative0: 1.0,
    xEnd: Math.PI / 2.0,
    step: 0.01);

var final = points[^1];
Console.WriteLine(final.Y);               // ungefähr 1
Console.WriteLine(final.FirstDerivative); // ungefähr 0
```

## Umfang

Diese API behandelt eine skalare Gleichung zweiter Ordnung. Die allgemeine Komfort-API für n-te Ordnung und eine eigene Komfort-API für gekoppelte Systeme zweiter Ordnung bleiben getrennte V1-Meilensteine. Beide sollen denselben Systemkern weiterverwenden und keine parallelen RK4-Implementierungen einführen.

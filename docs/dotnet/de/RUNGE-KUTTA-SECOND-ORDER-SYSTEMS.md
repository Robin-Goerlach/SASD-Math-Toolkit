# RK4 für gekoppelte Differentialgleichungssysteme zweiter Ordnung

## Zweck

`RungeKutta.FourthOrderSecondOrderSystem` löst gekoppelte Systeme der Form

`Y'' = G(x, Y, Y')`.

Dabei ist `Y` ein Vektor abhängiger Variablen. Für alle Komponenten werden Anfangswerte für `Y(x0)` und `Y'(x0)` benötigt.

Damit ist der historische V1-Komfortpunkt für gekoppelte Systeme zweiter Ordnung abgeschlossen. Die Implementierung ist eigenständiger neuer C#-Code auf Basis der mathematischen Standardreduktion auf erste Ordnung; historischer Borland-Quellcode oder Handbuchtext wird nicht übernommen.

## Reduktion auf ein System erster Ordnung

Für `m` Gleichungen zweiter Ordnung wird der Vektor `V = Y'` eingeführt. Damit entsteht

`Y' = V`

`V' = G(x, Y, V)`.

Intern wird der Zustand als

`[Y0, ..., Y(m-1), V0, ..., V(m-1)]`

gepackt und anschließend über denselben getesteten RK4-Systemkern integriert, den auch die anderen RK4-Komfort-APIs verwenden. Diese interne Packung bleibt vor dem Aufrufer verborgen.

## Callback-Modell

Der Callback erhält:

- `x`;
- einen schreibgeschützten Vektor der aktuellen Werte;
- einen schreibgeschützten Vektor der zugehörigen ersten Ableitungen.

Er liefert für jede Gleichung genau eine zweite Ableitung zurück. Die Dimension muss zu den Anfangsvektoren passen. Der Callback erhält Kopien der Werte- und Ableitungsgruppen, damit versehentliche Änderungen keinen internen RK4-Stufenzustand beschädigen.

## Ergebnismodell

Jeder `SecondOrderSystemOdePoint` ist ein unveränderlicher Snapshot mit:

- `X`;
- `Values`;
- `FirstDerivatives`;
- `Dimension`;
- den Indexhilfen `GetValue(i)` und `GetFirstDerivative(i)`.

Der Konstruktor kopiert beide Vektoren. Spätere Änderungen an aufrufereigenen Arrays können daher eine bereits berechnete Trajektorie nicht verändern.

## Beispiel: zwei gekoppelte Oszillatoren

```csharp
var end = Math.PI / (2.0 * Math.Sqrt(2.0));

var points = RungeKutta.FourthOrderSecondOrderSystem(
    (_, values, _) =>
    {
        var difference = values[0] - values[1];
        return [-difference, difference];
    },
    x0: 0.0,
    initialValues: [1.0, -1.0],
    initialFirstDerivatives: [0.0, 0.0],
    xEnd: end,
    step: 0.01);
```

Die Gleichungen lauten `y0'' = -(y0-y1)` und `y1'' = +(y0-y1)`. Das Beispiel ist tatsächlich gekoppelt, weil jede Beschleunigung von beiden aktuellen Werten abhängt.

## Validierung und numerische Fehler

Die Anfangsvektoren müssen nichtleer, endlich und gleich groß sein. Auch der Beschleunigungsvektor muss dieselbe Dimension besitzen. Nichtendliche Zwischenwerte werden vom gemeinsamen RK4-Systemkern erkannt, anstatt `NaN` oder Unendlich still in die weitere Rechnung zu übernehmen.

## Umfang

Die API ist ein festschrittiges explizites RK4-Referenzverfahren. Sie ist kein Spezialsolver für steife mechanische Systeme, Zwangsbedingungen, symplektische Integration oder differential-algebraische Gleichungen. Solche Erweiterungen können nach V1 gezielt ergänzt werden, ohne die gut verständliche Referenzimplementierung jetzt unnötig zu verkomplizieren.

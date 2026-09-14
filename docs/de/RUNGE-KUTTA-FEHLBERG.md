# Adaptives Runge-Kutta-Fehlberg-Verfahren 4(5)

## Zweck

`RungeKuttaFehlberg.Integrate` löst skalare Anfangswertprobleme erster Ordnung

`y' = f(x, y),   y(x0) = y0`

mit adaptiver Schrittweite. Damit ist der historische V1-Punkt Runge-Kutta-Fehlberg umgesetzt, jedoch mit einer modernen C#-API, einem eigenen Ergebnisobjekt und expliziter Konfiguration.

Die Implementierung ist eine eigenständige Umsetzung des klassischen mathematischen Verfahrens. Historischer Borland-/Pascal-Quelltext und Handbuchtext werden nicht übernommen.

## Eingebettetes 4(5)-Paar

Jeder Versuchsschritt berechnet sechs Ableitungsstufen, die gemeinsam für eine Näherung vierter und fünfter Ordnung verwendet werden. Die absolute Differenz beider Näherungen dient als lokale Fehlerschätzung. Bei einem akzeptierten Schritt wird die Näherung fünfter Ordnung übernommen.

Die lokale Akzeptanzschwelle kombiniert absoluten und relativen Fehler:

`AbsoluteTolerance + RelativeTolerance * max(|y_alt|, |y_neu|)`

Dadurch gibt es nahe Null eine absolute Untergrenze, während bei größeren Lösungswerten ein proportionaler Fehleranteil berücksichtigt wird.

## Schrittweitensteuerung

Die Fehlerschätzung verhält sich näherungsweise wie die fünfte Potenz der Schrittweite. Deshalb verwendet die Steuerung eine Korrektur mit fünfter Wurzel und einen Sicherheitsfaktor. `MinimumScaleFactor` und `MaximumScaleFactor` begrenzen starke Sprünge der Schrittweite.

`RungeKuttaFehlbergOptions` stellt insbesondere Anfangs-, Minimal- und Maximalschrittweite, absolute und relative Toleranz, die maximale Anzahl von Schrittversuchen sowie die Faktoren der Schrittweitensteuerung bereit.

Der Code ist bewusst lehrbuchnah und gut nachvollziehbar gehalten; Optimierungen können später erfolgen, ohne die öffentliche API grundlegend ändern zu müssen.

## Ergebnis und Abbruchzustände

`AdaptiveOdeResult` enthält den Anfangspunkt und alle akzeptierten `OdePoint`-Werte sowie die Anzahl akzeptierter und verworfener Schritte. Erwartbare numerische Abbruchfälle werden als Status gemeldet:

- `Completed` — Endpunkt erreicht;
- `MinimumStepSizeReached` — die gewünschte Toleranz würde eine kleinere als erlaubte Schrittweite verlangen;
- `MaximumStepAttemptsReached` — Arbeitsgrenze erreicht;
- `NumericalBreakdown` — ein Zwischenwert wurde nicht endlich oder die Gleitkommaauflösung erlaubt keinen Fortschritt mehr.

Ungültige Parameter bleiben normale Eingabefehler und führen zu .NET-Ausnahmen.

## Derzeitiger Umfang

Die V1-Implementierung ist absichtlich skalar und integriert vorwärts in `x`. Adaptive Systeme, Dense Output, Ereigniserkennung, Steifigkeitserkennung und spezielle Verfahren für steife Differentialgleichungen sind spätere Erweiterungen.

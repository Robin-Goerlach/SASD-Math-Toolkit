# Adams-Bashforth-/Adams-Moulton-Prädiktor-Korrektor

## Zweck

`AdamsBashforthMoulton.Integrate` löst skalare Anfangswertprobleme erster Ordnung

`y' = f(x, y),   y(x0) = y0`

mit dem klassischen Adams-Prädiktor-Korrektor-Verfahren vierter Ordnung. Damit ist der historische V1-Arbeitspunkt für Adams-Bashforth/Adams-Moulton als moderne C#-API umgesetzt.

Die Implementierung ist eigenständiger Clean-Room-Code auf Grundlage des veröffentlichten mathematischen Verfahrens. Historischer Borland-/Pascal-Quelltext und Handbuchtext werden nicht übernommen.

## Unterschied zu Runge-Kutta

Runge-Kutta-Verfahren erzeugen einen neuen Punkt hauptsächlich aus Ableitungsauswertungen innerhalb des aktuellen Schritts. Adams-Verfahren verwenden zusätzlich Ableitungswerte bereits berechneter Punkte. Nach der Startphase kann das die Zahl neuer Funktionsauswertungen pro Schritt reduzieren; dafür benötigt das Verfahren eine gleichmäßige Historie.

Der Vier-Schritt-Adams-Bashforth-Prädiktor lautet

`y(n+1)^p = y(n) + h/24 * [55 f(n) - 59 f(n-1) + 37 f(n-2) - 9 f(n-3)]`.

Anschließend folgt der Adams-Moulton-Korrektor vierter Ordnung

`y(n+1) = y(n) + h/24 * [9 f(n+1) + 19 f(n) - 5 f(n-1) + f(n-2)]`.

Der vorhergesagte Wert liefert zunächst eine Näherung für `f(n+1)`. Standardmäßig wird ein Korrekturdurchlauf ausgeführt. Über `correctorIterations` können mehrere feste Fixpunkt-Korrekturen angefordert werden.

## Start mit RK4

Eine Vier-Schritt-Adams-Formel kann nicht aus nur einem Anfangspunkt starten. Deshalb werden die ersten drei Intervalle mit dem klassischen RK4-Verfahren berechnet. Die skalare RK4-Implementierung stellt dafür intern denselben Einzelschritt bereit, den auch die öffentliche RK4-Routine verwendet.

Besteht das gesamte Intervall aus weniger als vier Schritten, bleibt die Berechnung folgerichtig vollständig in dieser RK4-Startphase.

## Gleichmäßiges Gitter und `maximumStep`

Die festen Mehrschrittkoeffizienten setzen gleiche Abstände voraus. Ein verkürzter letzter Schritt würde diese Voraussetzung verletzen. `maximumStep` wird deshalb als maximal gewünschter Abstand und nicht als zwingend exakt zu verwendende Schrittweite interpretiert.

Für die Intervalllänge `L` wird

`N = ceil(L / maximumStep)`

gewählt und anschließend gleichmäßig mit

`h = L / N`

integriert. Damit gilt `h <= maximumStep`, die Historie bleibt äquidistant und `xEnd` wird ohne einen Sonderfall für den letzten Adams-Schritt genau erreicht.

## Numerisches Verhalten und Validierung

Die Referenzimplementierung ist bewusst lehrbuchnah und noch nicht auf maximale Performance optimiert. Sie prüft endliche skalare Eingaben, positive Schrittweite und positive Zahl der Korrekturdurchläufe. Nicht-endliche Ableitungs- oder Lösungswerte führen zu einer `ArithmeticException`, statt unbemerkt `NaN` weiterzureichen.

Das Verfahren arbeitet mit fester Schrittweite und besitzt keine eingebettete lokale Fehlerschätzung. Wenn automatische Fehlerkontrolle wichtiger ist als ein regelmäßiges Gitter, ist derzeit `RungeKuttaFehlberg.Integrate` meist die passendere API.

## Spätere Erweiterungen

Denkbar sind Adams-Verfahren für Gleichungssysteme, variable Ordnung und Schrittweite, wiederverwendbare Ableitungshistorien, konvergenzgesteuerte implizite Korrektur sowie steife Mehrschrittverfahren. Diese Punkte gehören bewusst nicht zur V1-Kompatibilitätsimplementierung.

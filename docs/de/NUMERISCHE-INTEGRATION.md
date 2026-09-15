# Design der numerischen Integration

## Umfang

Die V1-Integrationsschicht enthält zusammengesetzte Trapez- und Simpson-Regel, adaptive Simpson-Quadratur, Romberg-Extrapolation, die Fünf-Punkt-Gauß-Legendre-Regel für ein Intervall sowie deren adaptive Verfeinerung. Der Code ist eine unabhängige Clean-Room-Implementierung; historische Dokumentation dient nur zur Definition des Kompatibilitätsziels.

## API-Schichten

Die festen Verfahren liefern einen `double`, weil sie außer der vom Aufrufer gewählten Panelzahl keinen eigenen Konvergenzzustand besitzen. Adaptive Simpson- und Gauß-Legendre-Verfahren besitzen jetzt zwei Ebenen: Die bisherigen Komfortmethoden liefern weiterhin nur den besten Wert, während `AdaptiveSimpsonDetailed` und `AdaptiveGaussLegendre5Detailed` ein `AdaptiveIntegrationResult` mit Status, Fehlerindikator, Callback-Anzahl und Zahl der akzeptierten Panels zurückgeben.

Romberg folgt demselben nicht brechenden Muster. `Romberg` behält den skalaren Rückgabewert; `RombergDetailed` liefert zusätzlich `IterationStatus`, Stufenzahl, Callback-Anzahl und die letzte Differenz auf der Romberg-Diagonale.

Damit bleiben einfache Anwendungen knapp, während Anwendungen mit höheren Qualitätsanforderungen keine Konvergenzinformation wegwerfen müssen.

## Validierungsvertrag

Öffentliche Routinen verlangen endliche Grenzen, `a < b` und eine als positiver endlicher `double` darstellbare Intervallbreite. Feste Verfahren verlangen positive Panelzahlen; bei Composite Simpson muss die Anzahl zusätzlich gerade sein. Adaptive Toleranzen und Tiefengrenzen müssen positiv sein. Romberg wird auf 30 Stufen begrenzt, weil das Referenzmodell Zweierpotenzen in einem `int` verwendet.

Jedes Ergebnis des Integrand-Callbacks wird auf Endlichkeit geprüft. Nicht-endliche Funktionswerte sind Daten-/Vertragsfehler und führen deshalb zu `ArithmeticException`; sie werden nicht als normale adaptive Nichtkonvergenz behandelt.

Auch Zwischenwerte werden geprüft, damit arithmetischer Overflow nahe an seiner Ursache sichtbar wird und nicht als scheinbar gültiges `NaN` oder Unendlich zurückkehrt.

## Adaptive Simpson

Das Verfahren vergleicht eine Simpson-Schätzung über `[a,b]` mit der Summe zweier Simpson-Schätzungen über die Halbintervalle. Der lokale Fehlerindikator lautet `|split - whole| / 15`; der Rückgabewert enthält die klassische Korrektur `delta / 15`. Die gewünschte Toleranz wird zwischen rekursiven Kindern aufgeteilt.

Wird das lokale Kriterium vor Erreichen der Tiefengrenze nicht erfüllt, bleibt die beste korrigierte Schätzung erhalten, der Status lautet aber `MaximumDepthReached`. Kann wegen der Gleitkommaauflösung kein unterschiedlicher Mittelpunkt mehr dargestellt werden, lautet der Status `NumericalResolutionReached`.

## Romberg

Romberg verwendet die vorherige Trapezstufe wieder und wertet nur die neuen ungeraden Gitterpunkte aus. Anschließend erzeugt Richardson-Extrapolation die diagonale Folge. Konvergenz wird über die Absolutdifferenz zweier aufeinanderfolgender Diagonaleinträge geprüft. Sind alle konfigurierten Stufen verbraucht, wird der letzte Wert mit `IterationStatus.MaximumIterationsReached` zurückgegeben, statt still Konvergenz zu behaupten.

## Gauß-Legendre

Die Fünf-Knoten-Regel bildet die Standardknoten und -gewichte von `[-1,1]` auf das gewünschte Intervall ab. Die adaptive Variante vergleicht eine Regel über ein Panel mit der Summe zweier Regeln über dessen Hälften. Bei weiterer Rekursion werden die bereits berechneten Halbintervallwerte als Ganzintervallwerte der Kinder wiederverwendet. Dadurch entfallen unnötige Callback-Wiederholungen, ohne das öffentliche Modell zu verändern.

Der adaptive Gauß-Fehlerwert ist die vom Verfahren verwendete Verfeinerungsdifferenz. Er ist ein Indikator und keine allgemein gültige mathematische Fehlerschranke.

## Performance-Haltung

Der Code bleibt bewusst eine gut lesbare Referenzimplementierung. V1 verwendet keine vektorisierten Kerne, Buffer-Pools, parallele Callback-Ausführung oder spezialisierten Transformationen für Singularitäten. Solche Optimierungen können später hinter denselben öffentlichen Verträgen ergänzt werden, wenn Messungen einen tatsächlichen Bedarf zeigen.

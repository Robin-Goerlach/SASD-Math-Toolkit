# Numerische Differentiation

Die C#-Implementierung deckt nun den vollständigen historischen V1-Bereich der Differentiation mit modernen APIs für Funktionen, tabellarische Daten und kubische Spline-Interpolanten ab.

## Ableitung von Funktionen

`NumericalDifferentiation` stellt Näherungen für erste und zweite Ableitungen bereit. Die Standardmethoden verwenden zentrale Differenzen mit Richardson-Verfeinerung; zusätzlich existieren explizite Fünfpunktformeln.

Diese Verfahren passen, wenn eine Funktion an benachbarten Stellen ausgewertet werden kann. Für bereits gemessene bzw. fest vorliegende Tabellenwerte sind die tabellarischen Verfahren geeigneter.

## Tabellarische Differentiation

`TabularDifferentiation` enthält die historische Familie der Zwei-, Drei- und Fünfpunktverfahren:

- `FirstDerivativeTwoPoint`
- `FirstDerivativeThreePoint`
- `FirstDerivativeFivePoint`
- `SecondDerivativeThreePoint`
- `SecondDerivativeFivePoint`

Die API erhält geordnete `x`- und `y`-Werte sowie den Index, an dem die Ableitung gesucht wird. Im Inneren der Tabelle wird ein lokaler Stützstellenbereich um den gewünschten Punkt gewählt. An den Rändern wird dieser Bereich automatisch verschoben und damit einseitig.

Die SASD-Implementierung setzt keine äquidistanten x-Werte voraus. Die Gewichte werden aus den Ableitungen der lokalen Lagrange-Interpolationspolynome bestimmt. Bei äquidistanten Daten entstehen daraus die bekannten klassischen Differenzenformeln. Damit bleibt die historische Verfahrensfamilie erhalten, wird aber für moderne Mess- und Wissenschaftsdaten allgemeiner nutzbar.

Bewusst geprüft werden gleiche Listenlängen, endliche Werte, streng steigende x-Werte, ein gültiger Zielindex und eine ausreichende Zahl an Stützstellen.

## Spline-Differentiation

`CubicSpline` besitzt nun `FirstDerivative(point)` und `SecondDerivative(point)`. Bei vielen Auswertungen desselben Splines ist dies die bevorzugte Variante.

`SplineDifferentiation` ergänzt komfortable Einmal-Aufrufe für natürliche und geklemmte Splines:

- `NaturalFirstDerivative`
- `NaturalSecondDerivative`
- `ClampedFirstDerivative`
- `ClampedSecondDerivative`

Ein geklemmter Spline ist sinnvoll, wenn die Randableitungen bekannt sind. Ein natürlicher Spline setzt an beiden Enden die zweite Ableitung auf null. Außerhalb des Interpolationsintervalls wird bewusst nicht still extrapoliert.

## Numerische Einordnung

Differentiation verstärkt Rauschen. Ein größerer Stützstellenbereich kann bei glatten Daten den Diskretisierungsfehler reduzieren, verbessert aber verrauschte Messungen nicht automatisch. Auch eine Spline-Ableitung ist die Ableitung des gewählten Interpolanten und keine Garantie für den unbekannten realen Prozess.

Rauschmodelle, Glättung, Unsicherheitsfortpflanzung und robuste lokale Regression gehören deshalb in spätere Statistik- und Datenanalyse-Schichten und nicht in diese V1-Basisroutinen.

# Interpolation – Architektur und numerische Verträge

## Umfang

Der V1-Interpolationsbereich enthält direkte Lagrange-Auswertung, Newton-Verfahren mit dividierten Differenzen, natürliche kubische Splines und geklemmte kubische Splines. Die Implementierung ist eigenständig neu erstellt; historische Unterlagen dienen nur zur Bestimmung des funktionalen Umfangs.

## Aufteilung der API

`InterpolationAlgorithms` enthält Konstruktion und zustandslose Hilfen für Polynominterpolation. `CubicSpline` ist ein unveränderliches, wiederverwendbares Ergebnisobjekt, das Stützstellen und Koeffizientendaten für wiederholte Wert- und Ableitungsauswertungen selbst besitzt.

Diese Trennung ist beabsichtigt: Der Aufbau eines Splines benötigt die Lösung eines tridiagonalen Systems, während die spätere Auswertung nur ein Segment finden und dessen kubisches Polynom auswerten soll.

## Polynominterpolation

`Lagrange` wertet die direkte Lagrange-Basis aus. Das ist eine besonders gut nachvollziehbare Referenzform ohne dauerhaftes Interpolantenobjekt, führt aber bei jedem Aufruf erneut O(n^2)-Arbeit aus.

`NewtonDividedDifferenceCoefficients` berechnet die Dreieckstafel der dividierten Differenzen in einer privaten Kopie von y und liefert die Newton-Koeffizienten zurück. `NewtonDividedDifference` ist eine Komfortmethode, die diese Koeffizienten konstruiert und die geschachtelte Newton-Form direkt auswertet.

Polynom-x-Werte müssen endlich und eindeutig sein, müssen aber nicht sortiert sein. Die übergebene Reihenfolge definiert die Newton-Basis.

## Kubische Splines

Natürliche und geklemmte Konstruktion verwenden gemeinsam `BuildCubicSpline`. Nur die erste und letzte Gleichung unterscheiden sich; innere tridiagonale Elimination und Rückwärtseinsetzen sind identisch. Ein gemeinsamer numerischer Kern reduziert das Risiko, dass sich die beiden Varianten im Laufe der Entwicklung ungewollt auseinanderentwickeln.

Natürliche Randbedingungen setzen die zweiten Ableitungen an den Endpunkten auf null. Geklemmte Randbedingungen verwenden die vom Aufrufer vorgegebenen ersten Ableitungen.

Spline-Stützstellen müssen endlich und streng aufsteigend sein. `CubicSpline` kopiert seine Koeffizientenpuffer und stellt die Stützstellen nur über eine schreibgeschützte Sicht bereit. Auswertungen außerhalb des Stützstellenintervalls werden abgelehnt, statt still zu extrapolieren.

## Umgang mit numerischem Versagen

Vertragsverletzungen wie doppelte x-Werte, unsortierte Spline-Stützstellen, unterschiedliche Arraylängen oder nicht-endliche Eingaben werden als Argument-Ausnahmen gemeldet.

Auch endliche Eingaben können bei Subtraktionen, der Konstruktion dividierter Differenzen, der tridiagonalen Elimination oder der Polynomauswertung überlaufen. Solche numerischen Abbrüche werden als `ArithmeticException` gemeldet; die API soll NaN oder Unendlich nicht als scheinbar erfolgreiches Interpolationsergebnis zurückgeben.

Das entspricht der allgemeinen V1-Konvention: fehlerhafte Eingaben sind von arithmetischem Versagen bei strukturell gültigen Daten zu unterscheiden.

## Position zur Performance

Der V1-Code ist eine verständliche, abhängigkeitsfreie Referenzimplementierung. Baryzentrische Lagrange-Gewichte, wiederverwendbare Newton-Interpolanten, vektorisierte Batch-Auswertung oder spezialisierte Spline-Arbeitspuffer werden bewusst noch nicht eingeführt. Solche Optimierungen können später anhand realer Lastprofile ergänzt werden, ohne die grundlegenden mathematischen Verträge ändern zu müssen.

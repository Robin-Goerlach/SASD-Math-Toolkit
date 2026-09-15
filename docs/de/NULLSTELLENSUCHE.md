# Nullstellensuche

Diese Notiz dokumentiert die Implementierungsverträge hinter den skalaren und komplexen Nullstellen-APIs. Anwenderorientierte Erklärungen und Beispiele stehen unter `docs/user-guide/de/nullstellen.md`.

## Entwurfsprinzipien

Nullstellensuche wird als iteratives numerisches Verfahren mit zwei unterschiedlichen Fehlerklassen behandelt. Ungültige Programmeingaben werden über normale .NET-Exceptions zurückgewiesen. Mathematisch gültige Iterationen, die nicht zuverlässig fortgesetzt werden können, liefern einen expliziten `IterationStatus` wie `NotBracketed`, `NumericalBreakdown` oder `MaximumIterationsReached`.

Konvergenzeinstellungen werden über `RootFindingOptions` explizit übergeben. Es gibt keine veränderliche prozessweite Toleranz und keinen versteckten globalen Iterationszustand.

## Skalare Konvergenzregel

Bisektion, Newton-Raphson und Sekanteniteration akzeptieren Konvergenz, wenn entweder

`|f(x)| <= tolerance`

gilt oder die Änderung des Iterationswerts

`|x(n) - x(n-1)| <= tolerance * max(1, |x(n-1)|)`

erfüllt. Diese kombinierte Residuum-/Schrittregel ist für eine Referenzimplementierung praktisch. Bei empfindlichen Rechnungen sollte trotzdem das zurückgegebene Residuum separat bewertet werden.

## Numerischer Abbruch

Newton-Raphson dividiert nicht durch Ableitungen, deren Betrag an oder unter der Near-Zero-Grenze des Toolkits liegt. Das Sekantenverfahren wendet dieselbe Idee auf die Differenz der beiden Funktionswerte an. Die Bisektion meldet `NotBracketed`, wenn die Funktionswerte an den Anfangsgrenzen dasselbe Vorzeichen haben.

Nicht-endliche Callback-Ergebnisse werden sofort zurückgewiesen. Zusätzlich werden neu berechnete Iterationswerte geprüft, bevor ein Callback erneut aufgerufen wird. Damit kann ein durch Overflow entstandenes `Infinity` nicht stillschweigend wieder in Anwendungscode gelangen.

## Komplexe und polynomielle Verfahren

Mullers Verfahren arbeitet direkt mit `System.Numerics.Complex` und kann die reelle Achse daher natürlich verlassen. Newton-Horner und Laguerre verwenden den unveränderlichen `Polynomial`-Typ und die erweiterte Horner-Auswertung wieder.

`FindAllRootsLaguerre` kombiniert wiederholte Laguerre-Suche und synthetische Deflation. Deterministische Ausweichstartpunkte basieren auf einer Cauchy-Nullstellenschranke. Nach dem Auffinden aller Nullstellen wird jede Lösung noch einmal gegen das ursprüngliche Polynom poliert, um aufsummierte Deflationsfehler zu reduzieren; anschließend wird das maximale Residuum bestimmt.

## Optimierungspolitik

Der aktuelle Code priorisiert expliziten Kontrollfluss, verständliche Diagnosen und reproduzierbares Verhalten. Fortgeschrittene abgesicherte Hybridverfahren, zusätzliche Skalierungsstrategien oder hochoptimierte Polynomsolver können später ergänzt werden, ohne das bestehende Ergebnis-/Statusmodell zu ändern.

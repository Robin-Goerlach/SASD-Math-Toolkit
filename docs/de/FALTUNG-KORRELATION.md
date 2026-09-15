# FFT-Faltung und Kreuzkorrelation

## Zweck

`FastFourierTransform` stellt jetzt lineare Faltung und Kreuzkorrelation sowohl für reelle als auch komplexe Folgen bereit. Damit ist der historische V1-Bereich Faltung/Korrelation vollständig, ohne getrennte numerische Kerne für reelle und komplexe Daten zu duplizieren.

Die Implementierung wurde eigenständig erstellt. Historisches Borland-Material dient lediglich als funktionale Checkliste für die V1-Abdeckung; historischer Quelltext oder Handbuchtext wird nicht übernommen.

## Lineare Faltung

Für endliche Folgen `x` und `h` lautet die lineare Faltung

`y[n] = sum_k x[k] * h[n-k]`

über alle Indizes, an denen beide Folgenelemente existieren.

Die öffentlichen APIs sind:

```csharp
FastFourierTransform.ConvolveComplex(left, right);
FastFourierTransform.ConvolveReal(left, right);
```

Bei nicht leeren Eingaben beträgt die Ergebnislänge immer

`left.Count + right.Count - 1`.

Eine leere Eingabe ergibt ein leeres Ergebnis.

## FFT-Strategie

Die Referenzimplementierung berechnet die lineare Faltung in folgenden Schritten:

1. benötigte Länge der linearen Faltung bestimmen;
2. beide Eingaben bis zur nächsten Zweierpotenz mit Nullen auffüllen;
3. die vorhandene Radix-2-FFT auf beide Folgen anwenden;
4. korrespondierende Frequenzbins multiplizieren;
5. die normierte inverse FFT anwenden;
6. nur die nicht aufgefüllten Samples der linearen Faltung übernehmen.

Diese Umsetzung ist absichtlich gut nachvollziehbar und nicht aggressiv optimiert. Der öffentliche Vertrag bleibt dadurch auch von einer späteren optimierten Backend-Implementierung unabhängig.

Die reelle Überladung wandelt ihre Daten in komplexe Werte um und verwendet denselben komplexen FFT-Faltungskern. Kleine imaginäre Rundungsreste werden beim Rückgabewert der reellen API verworfen.

## Konvention der komplexen Kreuzkorrelation

Bei komplexen Daten muss die Konjugationskonvention ausdrücklich festgelegt werden. Das SASD Math Toolkit definiert

`r[l] = sum_k left[k] * conjugate(right[k-l])`

über die jeweils gültige Überlappung.

Intern wird die Korrelation als Faltung mit der umgekehrten, komplex konjugierten rechten Folge formuliert. Dadurch verwenden Korrelation und Faltung denselben getesteten FFT-Kern.

Die öffentlichen APIs sind:

```csharp
FastFourierTransform.CrossCorrelateComplex(left, right);
FastFourierTransform.CrossCorrelateReal(left, right);
```

Für nicht leere Eingaben enthält das Ergebnis die Lags von

`-(right.Count - 1)` bis `left.Count - 1`.

Der Ergebnisindex `i` entspricht damit

`lag = i - (right.Count - 1)`.

Beim Lag null entspricht die komplexe Korrelation dem überlappenden inneren Produkt

`sum_k left[k] * conjugate(right[k])`.

Diese Zuordnung wird ausdrücklich dokumentiert, weil Bibliotheken unterschiedliche Reihenfolgen und Vorzeichenkonventionen für Korrelation verwenden.

## Beispiel

```csharp
using System.Numerics;
using Sasd.Numerics.Transforms;

Complex[] left = [new(1, 1), new(2, -1)];
Complex[] right = [new(3, 0), new(0, -1)];

var convolution = FastFourierTransform.ConvolveComplex(left, right);
var correlation = FastFourierTransform.CrossCorrelateComplex(left, right);

var zeroLagIndex = right.Length - 1;
Console.WriteLine(correlation[zeroLagIndex]);
```

## Eingabeprüfung

Komplexe FFT-, Faltungs- und Korrelationsdaten müssen endliche Real- und Imaginärteile besitzen. Reelle Helfer verlangen endliche Samples. Nicht-endliche Werte werden bereits an der öffentlichen API-Grenze abgewiesen, statt sich unbemerkt durch eine Transformation fortzupflanzen.

Direkte Aufrufe von `Forward` und `Inverse` verlangen weiterhin eine Zweierpotenz als Länge. Faltungsaufrufer müssen diese Bedingung nicht erfüllen, weil die Helfer intern selbst auf eine geeignete Radix-2-Länge auffüllen.

## Numerische Hinweise

FFT-Faltung unterliegt Gleitkomma-Rundungsfehlern. Ergebnisse sollten daher – außer bei sehr einfachen Sonderfällen – nicht auf exakte Gleichheit geprüft werden. Die Tests vergleichen kurze Fälle mit unabhängig berechneten direkten Ergebnissen und numerischen Toleranzen.

Bei sehr kurzen Folgen könnte eine direkte Doppelschleife schneller sein als das Anlegen der FFT-Arbeitsfelder. V1 bleibt bewusst bei einer klaren FFT-basierten Referenzimplementierung. Hybridschwellen, wiederverwendbare Puffer, SIMD und native FFT-Backends sind spätere Optimierungsthemen nach Profiling und keine V1-API-Frage.

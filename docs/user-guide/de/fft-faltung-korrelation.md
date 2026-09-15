# FFT, Faltung und Korrelation

Die Fast Fourier Transform (FFT) überführt eine abgetastete Folge aus dem Zeit-/Samplebereich in eine diskrete Frequenzdarstellung. Das SASD Math Toolkit verwendet derzeit eine gut lesbare Radix-2-Cooley-Tukey-Referenzimplementierung.

## Komplexe FFT

Für komplexe Samples werden `Forward` und `Inverse` verwendet:

```csharp
using System.Numerics;
using Sasd.Numerics.Transforms;

Complex[] samples = [1, 2, 3, 4];
var spectrum = FastFourierTransform.Forward(samples);
var restored = FastFourierTransform.Inverse(spectrum);
```

Die Vorwärtstransformation ist unskaliert. Die inverse Transformation teilt durch die Folgenlänge, sodass ein Vorwärts-/Rückwärtsdurchlauf die ursprüngliche Folge bis auf Gleitkomma-Rundungsfehler rekonstruiert.

Jede nicht leere Folge muss derzeit eine Zweierpotenz als Länge besitzen.

## Reelle Eingabe: vollständiges oder kompaktes Spektrum

`ForwardReal` ist die direkte Kompatibilitäts-API. Sie wandelt die reellen Samples in komplexe Werte um und liefert alle `N` Fourier-Bins.

Für die meisten Anwendungen ist `ForwardRealCompact` günstiger:

```csharp
using Sasd.Numerics.Transforms;

double[] samples = [1.0, 0.0, -1.0, 0.0, 1.0, 0.0, -1.0, 0.0];
var spectrum = FastFourierTransform.ForwardRealCompact(samples);

Console.WriteLine(spectrum.OriginalLength); // 8
Console.WriteLine(spectrum.BinCount);       // 5

for (var k = 0; k < spectrum.BinCount; k++)
{
    Console.WriteLine($"bin={k}, f/Fs={spectrum.GetNormalizedFrequency(k)}, magnitude={spectrum[k].Magnitude}");
}
```

Bei reeller Eingabe sind die negativen Frequenzbins die komplex konjugierten Gegenstücke der positiven Frequenzbins. Beide Hälften zu speichern wäre redundant. `RealFftSpectrum` speichert deshalb nur Gleichanteil bis Nyquist.

Bei einer Abtastrate `sampleRate` ergibt sich die physikalische Frequenz des Bins `k` mit

```csharp
var frequencyHz = spectrum.GetNormalizedFrequency(k) * sampleRate;
```

## Reelles Signal rekonstruieren

Die kompakte Darstellung wird mit `InverseReal` zurücktransformiert:

```csharp
var restored = FastFourierTransform.InverseReal(spectrum);
```

Das Toolkit rekonstruiert intern die ausgelassene konjugierte Hälfte. Anwendungscode muss das Packformat nicht kennen.

## FFT im Unterschied zur Fourier-Least-Squares-Approximation

Dieses Kapitel darf nicht mit `LeastSquares.FitFiveTermFourier` verwechselt werden. Die FFT analysiert Frequenzbins einer regelmäßig abgetasteten Folge. Das fünfgliedrige Least-Squares-Modell passt dagegen bei einer vorgegebenen Grundfrequenz ein kleines periodisches Modell an und kann auch unregelmäßige x-Koordinaten verwenden. Trotz Sinus-/Kosinusbezug lösen beide Verfahren unterschiedliche Aufgaben.

## Lineare Faltung

Die Faltung kombiniert zwei endliche Folgen, beispielsweise ein Signal und eine Impulsantwort. Reelle und komplexe Überladungen liefern jeweils die vollständige lineare Faltung:

```csharp
using System.Numerics;
using Sasd.Numerics.Transforms;

double[] signal = [1.0, 2.0, 1.0];
double[] kernel = [0.5, -0.5];
var filtered = FastFourierTransform.ConvolveReal(signal, kernel);

Complex[] complexSignal = [new(1, 1), new(2, -1)];
Complex[] complexKernel = [new(3, 0), new(0, -1)];
var complexFiltered = FastFourierTransform.ConvolveComplex(complexSignal, complexKernel);
```

Bei nicht leeren Eingaben enthält das Ergebnis `left.Count + right.Count - 1` Samples. Die Eingaben müssen nicht selbst auf eine Zweierpotenz aufgefüllt werden; der Helfer wählt intern eine passende FFT-Länge und entfernt die Auffüllung anschließend wieder.

## Kreuzkorrelation und Lag-Reihenfolge

Die Kreuzkorrelation misst Ähnlichkeit, während eine Folge relativ zur anderen verschoben wird. Dafür stehen `CrossCorrelateReal` und `CrossCorrelateComplex` bereit.

Bei komplexen Samples verwendet das Toolkit die Konjugation

`r[l] = sum_k left[k] * conjugate(right[k-l])`.

Das Ergebnis ist von Lag `-(right.Count - 1)` bis `left.Count - 1` geordnet. Für einen Ergebnisindex `i` ergibt sich der Lag mit

```csharp
var lag = i - (right.Count - 1);
```

Der Wert bei Lag null befindet sich damit am Index `right.Count - 1`. Bei komplexen Daten entspricht er dem überlappenden komplexen inneren Produkt.

```csharp
var correlation = FastFourierTransform.CrossCorrelateComplex(complexSignal, complexKernel);
var zeroLag = correlation[complexKernel.Length - 1];
```

Korrelationsbibliotheken verwenden unterschiedliche Konventionen. Anwendungscode, der Peak-Positionen interpretiert, sollte diese Index-Lag-Zuordnung daher ausdrücklich berücksichtigen.

## Leere und ungültige Eingaben

Faltung oder Korrelation mit einer leeren Folge liefert ein leeres Ergebnis. Reelle und komplexe Transformationshelfer weisen NaN und Unendlich zurück. Bei direkten FFT-Aufrufen muss die Länge einer nicht leeren Eingabe weiterhin eine Zweierpotenz sein.

## Praktische Grenzen

Die aktuelle Implementierung priorisiert Verständlichkeit und Testbarkeit statt maximalen FFT-Durchsatz. Sie erzeugt Zwischenarrays; auch die kompakte Real-FFT berechnet derzeit zunächst eine vollständige Transformation und verwirft danach die redundante Hälfte, und die Faltung verwendet auch bei sehr kleinen Folgen immer die FFT. Spätere optimierte oder native Backends können diese Details verbessern, ohne die öffentliche API zu ändern.

Bei der Interpretation eines Spektrums sollten Abtastrate, Fensterfunktion, Skalierung und spektrale Leckage ausdrücklich berücksichtigt werden. Die FFT transformiert die gelieferten Samples; sie wendet nicht automatisch eine Fensterfunktion an und leitet auch kein physikalisches Abtastintervall her.

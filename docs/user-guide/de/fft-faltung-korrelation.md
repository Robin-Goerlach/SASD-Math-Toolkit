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

## Faltung und Korrelation

Für reelle Folgen stehen derzeit `ConvolveReal` und `CrossCorrelateReal` zur Verfügung. Komplexe Varianten sind noch V1-Arbeitspunkte und werden hier ergänzt, sobald ihre API stabil ist.

## Praktische Grenzen

Die aktuelle Implementierung priorisiert Verständlichkeit und Testbarkeit statt maximalen FFT-Durchsatz. Sie erzeugt Zwischenarrays, und auch die kompakte Real-API berechnet derzeit zunächst eine vollständige FFT und verwirft danach die redundante Hälfte. Spätere optimierte oder native Backends können dies verbessern, ohne die öffentliche API zu ändern.

Bei der Interpretation eines Spektrums sollten Abtastrate, Fensterfunktion, Skalierung und spektrale Leckage ausdrücklich berücksichtigt werden. Die FFT transformiert die gelieferten Samples; sie wendet nicht automatisch eine Fensterfunktion an und leitet auch kein physikalisches Abtastintervall her.

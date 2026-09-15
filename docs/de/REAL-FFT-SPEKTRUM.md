# Kompaktes Real-FFT-Spektrum

## Zweck

`FastFourierTransform.ForwardRealCompact` ist die komfortable FFT-API für reelle Signale. Sie behält nur die nicht redundante Hälfte des Spektrums und liefert ein `RealFftSpectrum`, das zugleich die ursprüngliche Signallänge speichert.

Die C#-Implementierung wurde eigenständig erstellt. Die historische Borland-Toolbox dient ausschließlich als funktionaler V1-Meilenstein; historischer Quelltext und Handbuchtext werden nicht übernommen.

## Warum das Spektrum eines reellen Signals kompakt sein kann

Für eine reellwertige Eingangsfolge besitzt die diskrete Fouriertransformation hermitesche Symmetrie:

`X[N-k] = conjugate(X[k])`.

Die negativen Frequenzbins enthalten daher keine unabhängige Information. Bei einer Eingangslänge `N` speichert die kompakte Darstellung die Bins

`0 .. N/2`

und damit bei nicht leerer Eingabe insgesamt `N/2 + 1` Bins. Bin 0 ist der Gleichanteil (DC). Bei einer geraden Radix-2-Länge ist Bin `N/2` der Nyquist-Bin.

Die ältere API `ForwardReal` bleibt bestehen und liefert alle `N` komplexen Bins. Die kompakte API verhindert, dass Anwendungscode die Symmetrie selbst verwalten oder rekonstruieren muss.

## Ergebnismodell

`RealFftSpectrum` ist unveränderlich und stellt bereit:

- `OriginalLength` — Anzahl der ursprünglichen reellen Samples;
- `BinCount` — Anzahl der gespeicherten Bins;
- `Bins` — schreibgeschützte Sicht auf die Bins;
- einen Indexer für direkten Bin-Zugriff;
- `GetNormalizedFrequency(k)` — Bin-Frequenz in Zyklen pro Sample;
- `ToArray()` — eine defensive Kopie, wenn ein Array benötigt wird.

Bei einer physikalischen Abtastrate `Fs` gilt für Bin `k`

`f_k = k * Fs / N`.

Damit liefert `GetNormalizedFrequency(k) * Fs` die Frequenz in Hertz, wenn `Fs` in Samples pro Sekunde angegeben ist.

## Rücktransformation

`FastFourierTransform.InverseReal` akzeptiert ein `RealFftSpectrum`. Die fehlenden negativen Frequenzbins werden durch komplexe Konjugation rekonstruiert; danach wird dieselbe normalisierte komplexe inverse FFT verwendet wie im übrigen Toolkit.

Die Vorwärtstransformation ist unskaliert, während die inverse Transformation durch `N` teilt. Ein Vorwärts-/Rückwärtsdurchlauf rekonstruiert daher das ursprüngliche reelle Signal bis auf Gleitkomma-Rundungsfehler.

## Beispiel

```csharp
using Sasd.Numerics.Transforms;

double[] samples = [1.0, 0.0, -1.0, 0.0, 1.0, 0.0, -1.0, 0.0];

var spectrum = FastFourierTransform.ForwardRealCompact(samples);

for (var k = 0; k < spectrum.BinCount; k++)
{
    Console.WriteLine($"{k}: f/Fs={spectrum.GetNormalizedFrequency(k)}, |X|={spectrum[k].Magnitude}");
}

var restored = FastFourierTransform.InverseReal(spectrum);
```

## Aktueller numerischer Umfang

Die Referenz-FFT verwendet Radix-2-Cooley-Tukey. Jede nicht leere FFT-Eingabelänge muss daher eine Zweierpotenz sein. Eine leere Eingabe wird durch ein leeres kompaktes Spektrum repräsentiert. Reelle Eingabewerte müssen endlich sein.

Die kompakte V1-Implementierung berechnet zunächst die vollständige Transformation und behält anschließend nur die unabhängigen Bins. Das ist bewusst einfach und gut überprüfbar. Ein späterer optimierter Backend kann eine gepackte Real-FFT direkt berechnen, ohne die öffentliche `RealFftSpectrum`-Abstraktion zu ändern.

## Beziehung zu Faltung und Korrelation

FFT-Spektren sind auch ein effizienter Rechenweg für Faltung und Kreuzkorrelation. Für reelle Folgen sind entsprechende Helfer bereits vorhanden. Komplexe Faltung und komplexe Kreuzkorrelation bleiben eigene V1-Kompatibilitätspunkte und werden im nächsten Schritt ergänzt.

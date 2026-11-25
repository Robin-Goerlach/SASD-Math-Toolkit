# Migration aus `sasd-toolbox-dotnet`

Das Math Toolkit ist der kanonische Ort für wiederverwendbare numerische
Funktionen. Das Quell-Repository bleibt unverändert, bis die folgende Migration
vollständig geprüft wurde. Eine ähnlich benannte Ersatzfunktion allein reicht
nicht als Löschgrund.

| Quellfunktion | Kanonische Entscheidung | C++-Stand | Späterer .NET-Stand |
| --- | --- | --- | --- |
| `Guard` | Keine allgemeine Utility-API veröffentlichen. Numerische Vorbedingungen werden sprachintern geprüft. | Nur interne Fundament-Prüfungen | Der vorhandene interne `NumericGuard` bleibt kanonisch |
| Mittelwert, Median, Varianz | Mit robuster Überlaufbehandlung, endlichen Eingaben und expliziter Stichproben-/Grundgesamtheitssemantik migrieren. | CPP-M1 | Nach Stabilisierung des C++-Vertrags übertragen |
| Lineare Zweipunktinterpolation | Ohne Duplikation der Lagrange-Interpolation in die Interpolationsdomäne aufnehmen. | Für den Interpolationsmeilenstein geplant | Nach C++ geplant |
| Bisektionsverfahren | Die engere Toolbox-Implementierung nicht kopieren; der umfassendere statusbasierte Math-Toolkit-Vertrag gilt. | CPP-M2-Implementierung abgeschlossen | Vorhandene Implementierung ist kanonisch |
| Demo und Repository-Gerüst | Nicht migrieren; sie sind keine wiederverwendbare Mathematik. | Entfällt | Entfällt |

Vor einer Stilllegung des Quell-Repositories müssen:

1. alle gewünschten Funktionen Implementierung, Tests und Dokumentation besitzen;
2. C++ und danach .NET dieselben Konformitätsfälle erfüllen;
3. keine Pakete, Verbraucher oder Links mehr vom Quell-Repository abhängen;
4. Lizenz- und Herkunftsnachweise erhalten bleiben;
5. zunächst archiviert und nur nach einer ausdrücklichen Entscheidung gelöscht werden.

Dies ist eine semantische Migration und keine zeilenweise Quellcodeübertragung.
Die Implementierungen folgen den mathematischen Definitionen und den
kanonischen SASD-Verträgen.

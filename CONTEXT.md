# SoundBridge — Context

## Glossary

- **UDN (Unique Device Name)** — trwały identyfikator urządzenia UPnP, zapisywany w pliku `device.udn` w katalogu `data/`, konfigurowalny przez `appsettings.json`. Przy pierwszym uruchomieniu generowany, przy kolejnych odczytywany z pliku.
- **ContentDirectory** — usługa UPnP umożliwiająca wzmacniaczom audio przeglądanie i odtwarzanie plików udostępnianych przez SoundBridge.
- **Renderer / Wzmacniacz** — urządzenie audio (np. amplituner sieciowy) które odkrywa ContentDirectory i odtwarza z niego pliki.

## Decisions (ADR candidates)

1. **UDN persistence** — plik `device.udn` w katalogu `data/`, ścieżka konfigurowalna w `appsettings.json`.
2. **Project structure** — `SoundBridge.sln` + `src/SoundBridge.App/` (jeden projekt na start, gotowy na podział).
3. **UPnP hosting** — dwa `BackgroundService`: `UpnpDeviceService` (stos ohNet) + `ContentDirectoryService` (logika serwowania).
4. **Logging** — Serilog: Console + File, sterowane z `appsettings.json` przez `Serilog.Settings.Configuration`.
5. **Configuration** — `appsettings.json`: `SoundBridge.FriendlyName`, `SoundBridge.Manufacturer`, `SoundBridge.UdnFilePath`, sekcja `Serilog`.
6. **Service hosting** — `--service` w args włącza `AddWindowsService()`, bez = konsola, docker też konsola.
7. **DI dla UPnP** — `DvDevice` i providery jako singletony w DI; `ContentDirectoryService` dostaje providera przez konstruktor.
8. **UPnP providers** — tylko standard UPnP.org (`ContentDirectory:1` + `ConnectionManager:1`). OpenHome.org na później.
9. **ConnectionManager** — pełna implementacja, lista protokołów `http-get:*:audio/mpeg:*,http-get:*:audio/wav:*,http-get:*:audio/flac:*,http-get:*:audio/aac:*`.
10. **Docker** — Linux container `mcr.microsoft.com/dotnet/runtime:10.0`, tryb konsola (bez `--service`).
11. **ohNet init** — `Library.Create(initParams)` → `StartDv()` → `DvDeviceStandard(udn)` → `SetEnabled()`. Czeka na cancellation token.
12. **Pierwszy milestone** — `ContentDirectory` zwraca pusty DIDL-Lite (tylko discoverability, brak plików).
13. **Program.cs** — `static class Program` z `Main`, `Host.CreateDefaultBuilder(args)`, tradycyjny styl .NET.

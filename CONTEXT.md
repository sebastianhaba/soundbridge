# SoundBridge — Context

## Glossary

- **UDN (Unique Device Name)** — trwały identyfikator urządzenia UPnP, zapisywany w pliku `device.udn` w katalogu `data/`, konfigurowalny przez `appsettings.json` i zmienne środowiskowe. Przy pierwszym uruchomieniu generowany, przy kolejnych odczytywany z pliku.
- **ContentDirectory** — usługa UPnP umożliwiająca wzmacniaczom audio przeglądanie i odtwarzanie plików udostępnianych przez SoundBridge.
- **Renderer / Wzmacniacz** — urządzenie audio (np. amplituner sieciowy) które odkrywa ContentDirectory i odtwarza z niego pliki.
- **Local Library** — wpis w bazie LiteDB definiujący ścieżkę w systemie plików udostępnianą przez SoundBridge jako korzeń do przeglądania. Każda biblioteka ma unikalną nazwę-ID (np. `"Muzyka"`, `"Radio"`), która jest używana jako `@id` w DIDL-Lite. Zarządzana przez Web API (`/api/local-libraries`). Renderer widzi je jako kontenery najwyższego poziomu.
- **ObjectID** — nieprzezroczysty identyfikator kontenera lub itemu w ContentDirectory. SoundBridge koduje go jako `URL-encode({rootName}/{ścieżka_względna})`. Specjalna wartość `"0"` oznacza korzeń — zwraca listę Local Libraries z bazy.
- **Library Resolver** — komponent który mapuje ObjectID na zasoby w bibliotece. `LocalLibraryResolver` to pierwsza implementacja serwująca lokalne pliki; w przyszłości mogą dojść resolvery dla podcastów, radia itp.
- **ILocalLibraryStore** — interfejs serwisu enkapsulującego dostęp do kolekcji `local_libraries` w LiteDB. Wstrzykiwany do resolwera, kontrolera API i `ContentDirectoryService`.

## Decisions (ADR candidates)

1. **UDN persistence** — plik `device.udn` w katalogu `data/`, ścieżka konfigurowalna w `appsettings.json`.
2. **Project structure** — `SoundBridge.sln` + `src/SoundBridge.App/` (jeden projekt na start, gotowy na podział).
3. **UPnP hosting** — dwa `BackgroundService`: `UpnpDeviceService` (stos ohNet) + `ContentDirectoryService` (logika serwowania).
4. **Logging** — Serilog: Console + File, sterowane z `appsettings.json` przez `Serilog.Settings.Configuration`.
5. **Configuration** — `appsettings.json` + zmienne środowiskowe (`SoundBridge__*`), env vars mają wyższy priorytet. `.NET` standardowe `ConfigurationBuilder` w `CreateBuilder`. Klucze: `FriendlyName`, `UdnFilePath`, `WebServerHost`, `WebServerPort`.
6. **Service hosting** — `--service` w args włącza `AddWindowsService()`, bez = konsola, docker też konsola.
7. **DI dla UPnP** — `DvDevice` i providery jako singletony w DI; `ContentDirectoryService` dostaje providera przez konstruktor.
8. **UPnP providers** — tylko standard UPnP.org (`ContentDirectory:1` + `ConnectionManager:1`). OpenHome.org na później.
9. **ConnectionManager** — pełna implementacja, lista protokołów `http-get:*:audio/mpeg:*,http-get:*:audio/wav:*,http-get:*:audio/flac:*,http-get:*:audio/aac:*`.
10. **Docker** — Linux container `mcr.microsoft.com/dotnet/runtime:10.0`, tryb konsola (bez `--service`).
11. **ohNet init** — `Library.Create(initParams)` → `StartDv()` → `DvDeviceStandard(udn)` → `SetEnabled()`. Czeka na cancellation token.
12. **Drugi milestone** — `ContentDirectory` zwraca prawdziwe treści oparte na strukturze katalogów (FS-on-demand). 
13. **Program.cs** — `static class Program` z `Main`, przejście na `WebApplication.CreateBuilder()`.
14. **Library Roots** — zarządzane przez Web API (`/api/local-libraries`) z zapisem w LiteDB (`data/soundbridge.db`, kolekcja `local_libraries`). `ILocalLibraryStore` enkapsuluje dostęp.
15. **ObjectID encoding** — `URL-encode({rootName}/{ścieżka_względna})`; specjalne `"0"` = lista rootów z DB.
16. **Media hosting** — Kestrel (ASP.NET Core), port i host z configu (`WebServerPort`, `WebServerHost`). URL: `/media/{rootName}/{path...}` z `Results.File()` (range requests przez Kestrel).
17. **Path traversal** — autoryzacja ścieżki względem roota; tylko rozszerzenia audio (mp3, wav, flac, aac).
18. **Library resolver pattern** — `IContentResolver` wstrzykiwany do `SoundBridgeContentDirectory`; `LocalLibraryResolver` — pierwsza implementacja.
19. **Browse** — kontenery przed itemami, alfabetycznie, stronicowane. `childCount="0"`. Oba `BrowseFlag` (DirectChildren + Metadata). `Search` nierozpoczęte.
20. **SystemUpdateID** — Unix timestamp (sekundy od epoch). `ContainerUpdateIDs` puste.
21. **SortCriteria** — ignorowane (`SortCapabilities=""`).
22. **x86 build** — Debug wymusza `<PlatformTarget>x86</PlatformTarget>` ponieważ natywny DLL ohNet jest 32-bitowy. Bez tego `BadImageFormatException` przy starcie.
23. **Web API** — Kontrolery MVC (`LocalLibrariesController`, `MediaController`). `/api/local-libraries` — CRUD. `/media/{**path}` — serwowanie plików audio, zakresowe range request.
24. **LiteDB persistence** — `data/soundbridge.db`, singleton w DI, kolekcja `local_libraries` z indeksem unikalnym na `Name`.
25. **Swagger / Scalar** — `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore`, dostępne zawsze na `/scalar/v1`.
26. **Device metadata** — `Manufacturer` hardcoded `"Sebastian Haba"`, `ManufacturerURL` hardcoded `"https://github.com/sebastianhaba"`, `ModelName` hardcoded `"SoundBridge"`, `ModelNumber` (`"0.1.0"`), `ModelURL` (`"https://github.com/sebastianhaba/soundbridge"`). Tylko `FriendlyName` konfigurowalne.
27. **PresentationURL** — generowany dynamicznie z `WebServerHost` i `WebServerPort` jako `http://{host}:{port}/scalar/v1`. Pomijany jeśli host to wildcard (`0.0.0.0`, `+`, `::`).

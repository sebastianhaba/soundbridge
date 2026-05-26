# SoundBridge — Context

## Glossary

- **UDN (Unique Device Name)** — trwały identyfikator urządzenia UPnP, zapisywany w pliku `device.udn` w katalogu `data/`, konfigurowalny przez `appsettings.json` i zmienne środowiskowe. Przy pierwszym uruchomieniu generowany, przy kolejnych odczytywany z pliku.
- **ContentDirectory** — usługa UPnP umożliwiająca wzmacniaczom audio przeglądanie i odtwarzanie plików udostępnianych przez SoundBridge.
- **Renderer / Wzmacniacz** — urządzenie audio (np. amplituner sieciowy) które odkrywa ContentDirectory i odtwarza z niego pliki.
- **Local Library** — wpis w bazie LiteDB definiujący ścieżkę w systemie plików udostępnianą przez SoundBridge jako korzeń do przeglądania. Każda biblioteka ma unikalną nazwę-ID (np. `"Muzyka"`, `"Radio"`), która jest używana jako `@id` w DIDL-Lite. Zarządzana przez Web API (`/api/local-libraries`). Renderer widzi je jako kontenery najwyższego poziomu.
- **ObjectID** — nieprzezroczysty identyfikator kontenera lub itemu w ContentDirectory. SoundBridge koduje go jako `URL-encode({rootName}/{ścieżka_względna})`. Specjalna wartość `"0"` oznacza korzeń — zwraca listę wszystkich kontenerów najwyższego poziomu (Local Libraries z bazy + Radio Online).
- **Library Resolver** — komponent mapujący ObjectID na zasoby w bibliotece. Każda implementacja `IContentResolver` obsługuje jeden typ biblioteki. `CompositeResolver` (w `SoundBridge.App`) pełni rolę dispatchera: przy ID="0" komponuje wyniki ze wszystkich resolverów, dla niezerowych ID deleguje do właściwego resolvera na podstawie pierwszego segmentu ObjectID (root name).
- **Composite Resolver** — główna implementacja `IContentResolver` w `SoundBridge.App`, która agreguje listę sub-resolverów. Przy `Browse("0", ...)` wywołuje każdy sub-resolver, skleja DIDL-Lite i sumuje `TotalMatches`. Przy niezerowych ID parsuje root name z pierwszego segmentu ObjectID i deleguje do resolvera, który rozpoznaje ten root (przez wyszukanie w swojej kolekcji LiteDB).
- **ILocalLibraryStore** — interfejs serwisu enkapsulującego dostęp do kolekcji `local_libraries` w LiteDB. Rezyduje w `SoundBridge.Libraries.LocalLibrary` wraz z całą implementacją. Wstrzykiwany do resolwera, kontrolera API i `ContentDirectoryService`.
- **Radio Online Library** — wirtualna biblioteka strumieni radiowych, zawsze obecna jako kontener najwyższego poziomu z domyślną nazwą `"Radio Online"`. Nazwa roota jest edytowalna przez API. W przeciwieństwie do Local Libraries nie wymaga jawnego tworzenia — istnieje od pierwszego uruchomienia. Zarządzana przez Web API (`/api/radio-online`).
- **Radio Station** — wpis w kolekcji `radio_stations` w LiteDB reprezentujący pojedynczą stację radiową. Każda stacja ma unikalną `Name`, `Url` (adres strumienia audio lub playlisty) i `MimeType` (np. `audio/mpeg`, `audio/aac`, `audio/flac`, `audio/x-mpegurl`). Stacja jest prezentowana jako kontener (UPnP `storageFolder`) z jednym dzieckiem — itemem `"PlayStream"` (UPnP `audioBroadcast`), którego `<res>` wskazuje bezpośrednio na URL strumienia. W przyszłości jedna stacja będzie mogła mieć wiele streamów (różne bitrate/kodeki).
- **SoundBridge.Abstractions** — projekt z kontraktami i typami współdzielonymi między warstwami. Zawiera `IContentResolver`, `BrowseResult`, `BrowseFlag`, `SoundBridgeOptions`. Referencjonowany przez wszystkie inne projekty.
- **SoundBridge.Shared** — projekt z narzędziami bez zależności nugetowych. Zawiera `DidlLiteBuilder` (generacja DIDL-Lite XML) i `PathValidator` (walidacja ścieżek i rozszerzeń audio).
- **SoundBridge.Libraries.LocalLibrary** — projekt z pełną implementacją obsługi lokalnych bibliotek muzycznych. Zawiera model `LocalLibrary`, `ILocalLibraryStore` + `LocalLibraryStore` (LiteDB), `LocalLibraryResolver` (implementacja `IContentResolver`), `LocalLibrariesController` (Web API CRUD). Referencjonuje `Abstractions`, `Shared` oraz NuGet `LiteDB`.
- **SoundBridge.Libraries.RadioOnline** — projekt z implementacją obsługi strumieni radiowych. Zawiera model `RadioStation`, `IRadioOnlineStore` + `RadioOnlineStore` (LiteDB, kolekcje `radio_online_root` i `radio_stations`), `RadioOnlineResolver` (implementacja `IContentResolver`), `RadioOnlineController` (Web API CRUD). Referencjonuje `Abstractions`, `Shared` oraz NuGet `LiteDB`.

## Decisions (ADR candidates)

1. **UDN persistence** — plik `device.udn` w katalogu `data/`, ścieżka konfigurowalna w `appsettings.json`.
2. **Project structure** — rozwiązanie z czterema projektami: `SoundBridge.Abstractions` (kontrakty), `SoundBridge.Shared` (utility), `SoundBridge.Libraries.LocalLibrary` (implementacja lokalnych bibliotek), `SoundBridge.App` (host ASP.NET + UPnP). Wzorzec `Libraries.{Nazwa}` pozwala na dodawanie kolejnych typów bibliotek (podcasty, radio) jako osobne csproj.
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
18. **Library resolver pattern** — `IContentResolver` w `SoundBridge.Abstractions`, wstrzykiwany do `SoundBridgeContentDirectory`; `LocalLibraryResolver` w `SoundBridge.Libraries.LocalLibrary` — pierwsza implementacja.
19. **Browse** — kontenery przed itemami, alfabetycznie, stronicowane. `childCount="0"`. Oba `BrowseFlag` (DirectChildren + Metadata). `Search` nierozpoczęte.
20. **SystemUpdateID** — Unix timestamp (sekundy od epoch). `ContainerUpdateIDs` puste.
21. **SortCriteria** — ignorowane (`SortCapabilities=""`).
22. **x86 build** — Debug wymusza `<PlatformTarget>x86</PlatformTarget>` ponieważ natywny DLL ohNet jest 32-bitowy. Bez tego `BadImageFormatException` przy starcie.
23. **Web API** — Kontrolery MVC (`LocalLibrariesController` w `SoundBridge.Libraries.LocalLibrary`, `MediaController` w `SoundBridge.App`). `/api/local-libraries` — CRUD. `/media/{**path}` — serwowanie plików audio, zakresowe range request.
24. **LiteDB persistence** — `data/soundbridge.db`, singleton w DI, kolekcja `local_libraries` z indeksem unikalnym na `Name`.
25. **Swagger / Scalar** — `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore`, dostępne zawsze na `/scalar/v1`.
26. **Device metadata** — `Manufacturer` hardcoded `"Sebastian Haba"`, `ManufacturerURL` hardcoded `"https://github.com/sebastianhaba"`, `ModelName` hardcoded `"SoundBridge"`, `ModelNumber` (`"0.1.0"`), `ModelURL` (`"https://github.com/sebastianhaba/soundbridge"`). Tylko `FriendlyName` konfigurowalne.
27. **PresentationURL** — generowany dynamicznie z `WebServerHost` i `WebServerPort` jako `http://{host}:{port}/scalar/v1`. Pomijany jeśli host to wildcard (`0.0.0.0`, `+`, `::`).
28. **Composite Resolver** — zastępuje pojedynczy `IContentResolver` w `SoundBridgeContentDirectory` kompozytem, który deleguje do sub-resolverów (`LocalLibraryResolver`, `RadioOnlineResolver`). Przy `Browse("0", ...)` wywołuje każdy sub-resolver, skleja DIDL-Lite i sumuje `TotalMatches`. Przy niezerowych ID parsuje root name i routuje do właściwego resolvera. Root-level nie ma paginacji. Każdy sub-resolver rejestruje się przez DI jako `IEnumerable<IContentResolver>`.
29. **Radio Online Library** — wirtualna biblioteka radiowa jako osobny projekt `SoundBridge.Libraries.RadioOnline` według istniejącego wzorca `Libraries.{Nazwa}` (ADR 2). Zawsze włączona (brak mechanizmu on/off na tym etapie). Domyślna nazwa roota `"Radio Online"`, edytowalna przez API. Model `RadioStation` z polami `Name` (unikalne), `Url`, `MimeType`. Stacje jako kontenery z jednym dzieckiem `"PlayStream"` (UPnP `audioBroadcast`) — otwarte na przyszłe rozszerzenie o wiele streamów na stację. `<res>` wskazuje bezpośrednio na URL strumienia (nie przez `/media/`). MIME type wybierany jawnie przez użytkownika przy dodawaniu stacji. Dwie kolekcje LiteDB: `radio_online_root` (pojedynczy dokument z nazwą roota, upsert) i `radio_stations` (CRUD stacji, indeks unikalny na `Name`). Web API: `PUT /api/radio-online` (zmiana nazwy roota), `CRUD /api/radio-online/stations/{name}`. ObjectID: `URL-encode({rootName}/{stationName}/PlayStream)`. `ResolveToPath` zwraca `(null, null)` — brak serwowania plików przez Kestrel.

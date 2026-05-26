# SoundBridge

<img src="assets/social/soundbridge.png" alt="SoundBridge" width="600">

UPnP MediaServer udostępniający lokalne pliki audio oraz internetowe strumienie radiowe wzmacniaczom sieciowym (rendererom). Korzysta z Kestrel do serwowania plików i ohNet jako stosu UPnP.

**Główna cecha: biblioteka oparta na strukturze katalogów** — SoundBridge nie czyta metadanych plików (ID3 tagów). Zamiast tego odwzorowuje strukturę folderów 1:1. Jeśli masz dobrze zorganizowaną kolekcję (`Muzyka/Artysta/Album/utwór.mp3`), renderer zobaczy ją dokładnie w tej hierarchii. Żadnego skanowania, żadnych tagów — tylko to, co na dysku.

## Uruchamianie

SoundBridge działa w trzech trybach:

| Tryb | Komenda |
|------|---------|
| **Konsola** | `dotnet run --project src/SoundBridge.App` |
| **Windows Service** | `dotnet run --project src/SoundBridge.App -- --service` (wymaga administratora do instalacji: `sc create SoundBridge binPath=...`) |
| **Docker** | `docker run -d soundbridge` |

Zaleca się ustawienie konkretnego adresu IP w `WebServerHost` zamiast domyślnego `0.0.0.0` — zapewnia to poprawne działanie UPnP (SSDP, PresentationURL, ikony) w sieci lokalnej.

## Quick start

```bash
# Zbuduj
dotnet build

# Uruchom (przed uruchomieniem ustaw WebServerHost w appsettings.json na adres IP swojej maszyny)
dotnet run --project src/SoundBridge.App
```

## Konfiguracja

Ustawienia pochodzą z dwóch źródeł — **zmienne środowiskowe mają priorytet** nad `appsettings.json`.

### Przez `appsettings.json`

```json
{
  "SoundBridge": {
    "FriendlyName": "SoundBridge",
    "UdnFilePath": "data/device.udn",
    "WebServerHost": "192.168.1.100",
    "WebServerPort": 5000
  }
}
```

> **Uwaga:** `WebServerHost` ustaw na rzeczywisty adres IP maszyny w sieci lokalnej. `0.0.0.0` spowoduje, że PresentationURL, ikony i SSDP nie będą działać poprawnie.

### Przez zmienne środowiskowe

Te same klucze, prefiks `SoundBridge__`:

```bash
# Linux / Docker
export SoundBridge__FriendlyName="MojaMuzyka"
export SoundBridge__WebServerPort=8080
```

```powershell
# Windows PowerShell
$env:SoundBridge__FriendlyName = "MojaMuzyka"
$env:SoundBridge__WebServerPort = 8080
```

### Dostępne opcje

| Klucz | Domyślnie | Opis |
|-------|-----------|------|
| `FriendlyName` | `SoundBridge` | Nazwa wyświetlana w rendererze |
| `UdnFilePath` | `data/device.udn` | Ścieżka pliku UDN |
| `WebServerHost` | `0.0.0.0` | Adres IP maszyny — **zaleca się ustawienie konkretnego IP** |
| `WebServerPort` | `5000` | Port nasłuchiwania Kestrel |

## Zarządzanie bibliotekami — Web API

Biblioteki (Local Libraries) to ścieżki w systemie plików widoczne jako główne kontenery w ContentDirectory. **Konfiguruje się je wyłącznie przez REST API** — nie ma ich w `appsettings.json`.

### `/api/local-libraries`

| Metoda | Ścieżka | Opis |
|--------|---------|------|
| `GET` | `/api/local-libraries` | Lista wszystkich bibliotek |
| `GET` | `/api/local-libraries/{name}` | Pojedyncza biblioteka |
| `POST` | `/api/local-libraries` | Dodaj nową bibliotekę |
| `DELETE` | `/api/local-libraries/{name}` | Usuń bibliotekę |

### Przykłady

```bash
# Pobierz listę
curl http://{WebServerHost}:{WebServerPort}/api/local-libraries

# Dodaj bibliotekę
curl -X POST http://{WebServerHost}:{WebServerPort}/api/local-libraries \
  -H "Content-Type: application/json" \
  -d '{"name": "Muzyka", "path": "I:\\music"}'

# Usuń bibliotekę
curl -X DELETE http://{WebServerHost}:{WebServerPort}/api/local-libraries/Muzyka
```

Biblioteki zapisywane są w LiteDB (`data/soundbridge.db`). Renderery UPnP widzą je przy Browse z `ObjectID=0`.

## Radio Online — Web API

Wirtualna biblioteka strumieni radiowych. Zawsze obecna jako kontener najwyższego poziomu (domyślnie `"Radio Online"`), nie wymaga jawnego tworzenia. Nazwę roota można zmienić przez API.

### `/api/radio-online`

| Metoda | Ścieżka | Opis |
|--------|---------|------|
| `GET` | `/api/radio-online` | Nazwa roota |
| `PUT` | `/api/radio-online` | Zmień nazwę roota |

### `/api/radio-online/stations`

| Metoda | Ścieżka | Opis |
|--------|---------|------|
| `GET` | `/api/radio-online/stations` | Lista wszystkich stacji |
| `GET` | `/api/radio-online/stations/{name}` | Pojedyncza stacja |
| `POST` | `/api/radio-online/stations` | Dodaj stację |
| `PUT` | `/api/radio-online/stations/{name}` | Edytuj stację (URL, MimeType) |
| `DELETE` | `/api/radio-online/stations/{name}` | Usuń stację |

### Dostępne MIME types

`audio/mpeg` (MP3), `audio/aac`, `audio/flac`, `audio/x-mpegurl` (M3U playlisty)

### Przykłady

```bash
# Zmień nazwę roota
curl -X PUT http://{host}:{port}/api/radio-online \
  -H "Content-Type: application/json" \
  -d '{"name": "Internet Radio"}'

# Dodaj stację
curl -X POST http://{host}:{port}/api/radio-online/stations \
  -H "Content-Type: application/json" \
  -d '{"name": "Radio Nowy Świat", "url": "https://stream.example.com/rns.mp3", "mimeType": "audio/mpeg"}'

# Edytuj URL stacji
curl -X PUT http://{host}:{port}/api/radio-online/stations/Radio%20Nowy%20Świat \
  -H "Content-Type: application/json" \
  -d '{"url": "https://new-stream.example.com/rns.aac", "mimeType": "audio/aac"}'

# Usuń stację
curl -X DELETE http://{host}:{port}/api/radio-online/stations/Radio%20Nowy%20Świat
```

Każda stacja pojawia się jako kontener z jednym dzieckiem `"PlayStream"` (UPnP `audioBroadcast`) — renderer odtwarza stream bezpośrednio z zewnętrznego URL-a.

## Scalar API Reference

Dokumentacja OpenAPI dostępna zawsze pod `/scalar/v1` — interaktywne UI do testowania endpointów API.

## Serwowanie plików audio

Pliki serwowane są przez Kestrel pod trasą `/media/{**path}` z obsługą range requests (niezbędne do przewijania w rendererze). Dozwolone rozszerzenia: `.mp3`, `.wav`, `.flac`, `.aac`.

## Docker

```bash
# Budowa
docker build -t soundbridge .

# Uruchomienie z mapowaniem portów i wolumenem
docker run -d \
  -p 5000:5000 \
  -p 1900:1900/udp \
  -v /host/data:/app/data \
  -e SoundBridge__WebServerHost={WebServerHost} \
  -e SoundBridge__FriendlyName="SoundBridge-Docker" \
  soundbridge
```

Porty:
- `5000` — HTTP (Kestrel: API + media)
- `1900/udp` — SSDP (wykrywanie UPnP)

Wolumen `/app/data` przechowuje `device.udn`, `soundbridge.db` i logi.

## Struktura projektu

```
src/
├── SoundBridge.Abstractions/       # IContentResolver, BrowseResult, SoundBridgeOptions
├── SoundBridge.Shared/             # DidlLiteBuilder, PathValidator
├── SoundBridge.Libraries.LocalLibrary/  # Lokalne biblioteki plików
│   ├── Models/                     # LocalLibrary
│   └── Controllers/                # LocalLibrariesController
├── SoundBridge.Libraries.RadioOnline/   # Strumienie radiowe
│   ├── Models/                     # RadioRoot, RadioStation
│   └── Controllers/                # RadioOnlineController
└── SoundBridge.App/                # Host ASP.NET + UPnP
    ├── Controllers/                # MediaController
    ├── Core/                       # UdnManager
    ├── Providers/                  # SoundBridgeContentDirectory, SoundBridgeConnectionManager
    ├── Services/                   # UpnpDeviceService, ContentDirectoryService
    └── CompositeResolver.cs        # Dyspozytor sub-resolverów
```

# Content Parser API

REST API napisane w **.NET 10 / C#**, służące do dekodowania oraz parsowania danych przesyłanych przez API.

Aplikacja przyjmuje dane zakodowane w formacie **Base64**, następnie dekoduje ich zawartość i przekazuje ją do odpowiedniego parsera na podstawie typu danych wskazanego w żądaniu.

Obecnie obsługiwane formaty:

- `CSV`
- `INTERNAL_JSON`

Architektura projektu została zaprojektowana w sposób umożliwiający łatwe rozszerzanie o kolejne formaty danych (np. `XML`) bez konieczności modyfikowania istniejącej logiki API.

---

## ✨ Funkcjonalności

- Dekodowanie danych zakodowanych w formacie Base64
- Parsowanie danych CSV
- Parsowanie danych w formacie INTERNAL_JSON
- Łatwe rozszerzanie o nowe parsery
- Globalna obsługa błędów
- Dokumentacja OpenAPI / Scalar
- Architektura oparta o wzorce Strategy oraz Factory

---

## 🚀 Technologie

- .NET 10
- ASP.NET Core Minimal API
- C#
- Dependency Injection
- Factory Pattern
- Strategy Pattern
- OpenAPI / Scalar

---

## 🏗️ Architektura

Przepływ obsługi żądania:

```text
Client
   │
   ▼
POST /api/v1/parse-content
   │
   ▼
Base64 Decoder
   │
   ▼
ContentParserFactory
   │
   ├────────► CsvContentParser
   │
   └────────► InternalJsonContentParser
                 │
                 ▼
            Parse Result
                 │
                 ▼
          HTTP Response
````

### Główne komponenty

#### `IContentDecoder`

Odpowiada za dekodowanie danych wejściowych.

Aktualna implementacja:

* `Base64ContentDecoder`

---

#### `IContentParser`

Interfejs definiujący parser danych.

Każda implementacja:

* określa obsługiwany typ danych,
* zawiera własną logikę parsowania.

Aktualne implementacje:

* `CsvContentParser`
* `InternalJsonContentParser`

---

#### `ContentParserFactory`

Odpowiada za wybór odpowiedniego parsera na podstawie typu danych przesłanego w żądaniu.

Dodanie nowego parsera wymaga jedynie:

1. Utworzenia nowej klasy implementującej `IContentParser`.
2. Zarejestrowania jej w kontenerze Dependency Injection.

Dzięki temu logika API pozostaje zamknięta na modyfikacje i otwarta na rozszerzenia (zasada Open/Closed).

---

## 🚀 Wymagania wstępne

* .NET 10 SDK

Sprawdzenie zainstalowanej wersji:

```bash
dotnet --version
```

---

## 🛠️ Uruchomienie lokalne

### 1. Sklonuj repozytorium

```bash
git clone https://github.com/GracjanWentrys/content-parser-api.git
cd content-parser-api
```

### 2. Przywróć zależności

```bash
dotnet restore
```

### 3. Uruchom aplikację

Jeżeli znajdujesz się w katalogu głównym rozwiązania:

```bash
dotnet run --project Api
```

Jeżeli jesteś bezpośrednio w katalogu projektu API:

```bash
dotnet run
```

Po uruchomieniu aplikacja będzie dostępna pod adresem wyświetlonym w konsoli.

---

## 📚 Dokumentacja API

Po uruchomieniu aplikacji dostępna jest automatycznie wygenerowana dokumentacja API.

* **Scalar UI:** `https://localhost:<port>/scalar/v1`
* **OpenAPI:** `https://localhost:<port>/openapi/v1.json`

> Port może różnić się w zależności od konfiguracji środowiska.

---

## 📌 Endpoint

| Metoda | Endpoint                |
| ------ | ----------------------- |
| POST   | `/api/v1/parse-content` |

Parsuje dane zakodowane w Base64.

### Nagłówki

```http
Content-Type: application/json
```

### Przykład żądania (CSV)

```json
{
  "type": "CSV",
  "content": "TmFtZSxDaXR5LEFnZQpKb2huLCJOZXcgWW9yaywgVVNBIiwzMA=="
}
```

Po zdekodowaniu:

```text
Name,City,Age
John,"New York, USA",30
```

### Przykład żądania (JSON)

```json
{
  "type": "INTERNAL_JSON",
  "content": "W3sibmFtZSI6IkpvaG4iLCJhZ2UiOjMwfV0="
}
```

Po zdekodowaniu:

```json
[
  {
    "name": "John",
    "age": 30
  }
]
```

---

## ✅ Przykładowa odpowiedź (CSV)

```json
{
  "isSuccess": true,
  "recordCount": 1,
  "data": [
    {
      "Name": "John",
      "City": "New York, USA",
      "Age": "30"
    }
  ],
  "errorMessage": null
}
```

### Przykładowa odpowiedź (JSON)

```json
{
  "isSuccess": true,
  "recordCount": 1,
  "data": [
    {
      "name": "John",
      "age": 30
    }
  ],
  "errorMessage": null
}
```

---

## ❌ Obsługa błędów

API rozróżnia błędy klienta oraz błędy serwera.

### Niepoprawny Base64 (400 Bad Request)

```json
{
  "isSuccess": false,
  "recordCount": 0,
  "data": null,
  "errorMessage": "The provided content is not a valid Base-64 string."
}
```

### Nieobsługiwany typ danych (400 Bad Request)

```json
{
  "isSuccess": false,
  "recordCount": 0,
  "data": null,
  "errorMessage": "Content type 'XML' is not supported."
}
```

### Błąd parsowania danych (422 Unprocessable Entity)

```json
{
  "isSuccess": false,
  "recordCount": 0,
  "data": null,
  "errorMessage": "Invalid JSON structure."
}
```

### Nieoczekiwany błąd aplikacji (500 Internal Server Error)

```json
{
  "isSuccess": false,
  "recordCount": 0,
  "data": null,
  "errorMessage": "An unexpected error occurred."
}
```

Szczegółowe informacje o błędzie są zapisywane w logach aplikacji, natomiast klient otrzymuje jedynie bezpieczny komunikat.

---

## 🔧 Dodanie nowego parsera

Przykład dodania obsługi formatu `XML`.

### 1. Utwórz implementację parsera

```csharp
public class XmlContentParser : IContentParser
{
    public ContentType SupportedType => ContentType.XML;

    public IParseResult Parse(string rawContent)
    {
        // XML parsing logic
    }
}
```

### 2. Dodaj nowy typ danych

```csharp
public enum ContentType
{
    CSV,
    INTERNAL_JSON,
    XML
}
```

### 3. Zarejestruj parser

```csharp
builder.Services.AddSingleton<IContentParser, XmlContentParser>();
```

Od tego momentu `ContentParserFactory` będzie automatycznie wykorzystywała nowy parser bez konieczności modyfikowania istniejącej logiki.

---

## 📝 Decyzje projektowe

* **Strategy Pattern** – każdy parser odpowiada wyłącznie za obsługę jednego formatu danych.
* **Factory Pattern** – wybór parsera odbywa się dynamicznie na podstawie typu danych przesłanego w żądaniu.
* **Dependency Injection** – wszystkie komponenty są zarządzane przez kontener DI, co upraszcza testowanie i rozbudowę aplikacji.
* **Globalna obsługa błędów** – dedykowany middleware zapewnia spójny format odpowiedzi błędów.
* **Własny parser CSV** – poprawnie obsługuje wartości w cudzysłowach, przecinki wewnątrz pól oraz znaki nowej linii bez konieczności korzystania z ciężkich zewnętrznych bibliotek.
* **Minimal API** – endpoint odpowiada wyłącznie za orkiestrację procesu, natomiast logika biznesowa została wydzielona do osobnych komponentów.

---

## 📄 O projekcie

Projekt został przygotowany jako zadanie rekrutacyjne prezentujące implementację rozszerzalnego parsera danych w technologii .NET 10 z wykorzystaniem wzorców projektowych Factory i Strategy oraz architektury opartej na Dependency Injection.

```


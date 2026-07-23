# Content Parser API

Web API napisane w **.NET 8 / C#**, służące do dekodowania oraz generycznego parsowania danych przesyłanych przez API.

Aplikacja przyjmuje dane zakodowane w formacie **Base64**, następnie dekoduje zawartość i przekazuje ją do odpowiedniego parsera w zależności od podanego typu danych.

Aktualnie obsługiwane formaty:
- `CSV`
- `INTERNAL_JSON`

Projekt został zaprojektowany tak, aby łatwo można było dodać kolejne formaty danych (np. XML) bez modyfikowania istniejącej logiki API.

---

## 🚀 Technologie

- .NET 8
- ASP.NET Core Minimal API
- C#
- Dependency Injection
- Factory Pattern
- Strategy Pattern
- OpenAPI / Scalar (lub Swagger UI)

---

## 🏗️ Architektura

Przepływ obsługi żądania:

```text
HTTP Request
     |
     v
Base64 Decoder
     |
     v
Content Parser Factory
     |
     v
CSV Parser / JSON Parser
     |
     v
Parse Result
     |
     v
HTTP Response
```

### Główne komponenty:

#### `IContentDecoder`
Odpowiada za dekodowanie danych wejściowych.

Aktualna implementacja:
- `Base64ContentDecoder`

---

#### `IContentParser`
Interfejs dla wszystkich parserów danych.

Każdy parser posiada:
- obsługiwany typ danych,
- własną logikę parsowania.

Aktualne implementacje:
- `CsvContentParser`
- `InternalJsonContentParser`

---

#### `ContentParserFactory`
Odpowiada za wybór odpowiedniego parsera na podstawie typu danych przesłanego w request.

Dodanie nowego parsera wymaga jedynie:
1. Utworzenia nowej klasy implementującej `IContentParser`.
2. Zarejestrowania jej w Dependency Injection.

---

## 🚀 Wymagania wstępne

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Sprawdzenie wersji:
```bash
dotnet --version
```

---

## 🛠️ Uruchomienie lokalne

1. **Sklonuj repozytorium**:
```bash
git clone https://github.com/TWOJ_USERNAME/content-parser-api.git
cd content-parser-api
```

2. **Przywróć zależności NuGet**:
```bash
dotnet restore
```

3. **Uruchom aplikację**:

Jeżeli znajdujesz się w katalogu głównym rozwiązania:
```bash
dotnet run --project Api
```

Jeżeli znajdujesz się bezpośrednio w katalogu projektu API:
```bash
dotnet run
```

Po uruchomieniu aplikacja będzie dostępna pod adresem pokazanym w konsoli.

---

## 📚 Dokumentacja API (Scalar / Swagger)

Po uruchomieniu aplikacji dokumentacja API dostępna jest pod adresem:
* **Scalar UI**: `https://localhost:<port>/scalar/v1`
* **OpenAPI Spec**: `https://localhost:<port>/openapi/v1.json`

*(Port może różnić się zależnie od konfiguracji środowiska).*

---

## 📌 Endpoint

### `POST /api/v1/parse-content`

Parsuje zawartość zakodowaną w Base64.

#### **Headers**:
`Content-Type: application/json`

#### **Body Request**:

**Przykład dla CSV:**
```json
{
  "type": "CSV",
  "content": "TmFtZSxDaXR5LEFnZQpKb2huLCJOZXcgWW9yaywgVVNBIiwzMA=="
}
```
*(Po dekodowaniu Base64: `Name,City,Age
John,"New York, USA",30`)*

**Przykład dla JSON:**
```json
{
  "type": "INTERNAL_JSON",
  "content": "W3sibmFtZSI6IkpvaG4iLCJhZ2UiOjMwfV0="
}
```
*(Po dekodowaniu Base64: `[{"name":"John","age":30}]`)*

---

### ✅ Przykładowa odpowiedź sukcesu (HTTP 200 OK)

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

---

### ❌ Obsługa błędów

API rozróżnia błędy klienta oraz błędy serwera.

#### **1. Niepoprawny Base64 (HTTP 400 Bad Request)**
```json
{
  "isSuccess": false,
  "recordCount": 0,
  "data": null,
  "errorMessage": "The provided content is not a valid Base-64 string."
}
```

#### **2. Nieobsługiwany typ danych (HTTP 400 Bad Request)**
```json
{
  "isSuccess": false,
  "recordCount": 0,
  "data": null,
  "errorMessage": "Content type 'XML' is not supported."
}
```

#### **3. Błąd parsowania danych (HTTP 422 Unprocessable Entity)**
```json
{
  "isSuccess": false,
  "recordCount": 0,
  "data": null,
  "errorMessage": "Invalid JSON structure."
}
```

#### **4. Nieoczekiwany błąd aplikacji (HTTP 500 Internal Server Error)**
```json
{
  "isSuccess": false,
  "recordCount": 0,
  "data": null,
  "errorMessage": "An unexpected error occurred."
}
```

Szczegóły błędu są zapisywane w logach aplikacji, ale nie są zwracane bezpośrednio klientowi ze względów bezpieczeństwa.

---

## 🔧 Dodanie nowego parsera

Przykład: dodanie obsługi `XML`.

1. **Utwórz nową implementację:**
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

2. **Dodaj nowy typ w enum:**
```csharp
public enum ContentType
{
    CSV,
    INTERNAL_JSON,
    XML
}
```

3. **Zarejestruj parser w Dependency Injection:**
```csharp
builder.Services.AddSingleton<IContentParser, XmlContentParser>();
```

API automatycznie zacznie obsługiwać nowy format dzięki wzorcowi Factory!

---

## 📝 Decyzje projektowe

* **Wzorzec Strategy & Factory**: Parsery zostały oddzielone od logiki API, co zapewnia wysoki poziom rozszerzalności (zgodność z zasadą Open/Closed z SOLID).
* **Globalna obsługa błędów**: Centralny middleware przechwytuje wyjątki i spójnie formatuje odpowiedzi błędów.
* **Własny / Lekki Parser CSV**: Zapewnia poprawną obsługę wartości w cudzysłowach, przecinków wewnątrz pól oraz znaków nowej linii bez konieczności ciągnięcia dużych i przestarzałych zależności zewnętrznych.
* **Czysty układ Minimal API**: Logika endpointu pozostaje zwięzła i skupia się wyłącznie na orkiestracji procesu.

---

## 📄 Licencja

Projekt został przygotowany jako zadanie rekrutacyjne / przykład implementacji generycznego parsera danych dla API w technologii .NET 8.

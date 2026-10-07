# Novashelf API

ASP.NET Core (.NET 10) minimal API that serves space content to the Vue front end. It is the only
part of Novashelf that talks to the upstream sources, which it normalises into one shape and caches
in memory:

| Source | Used for | Key? |
|---|---|---|
| [Spaceflight News API](https://api.spaceflightnewsapi.net/v4/docs/) | Article feed, topics | No |
| science.nasa.gov `apod-basic` | Astronomy Picture of the Day (1995 → today) | No |
| [NASA Image and Video Library](https://images.nasa.gov/docs/images.nasa.gov_api_docs.pdf) | Image search | No |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`dotnet --list-sdks` should show `10.x`)

No database, Docker or API keys are needed yet.

## Run

From the repository root:

```sh
dotnet run --project server/Novashelf.Api
```

The API listens on **http://localhost:5088**. Check it with:

```sh
curl http://localhost:5088/api/health
```

To restart automatically when you edit C# files, use `dotnet watch` instead:

```sh
dotnet watch --project server/Novashelf.Api
```

### With the front end

Run the API (above) and, in a second terminal, the Vite dev server from the repository root:

```sh
pnpm dev
```

Vite proxies every `/api/*` request to `http://localhost:5088`, so the app at
http://localhost:5173 talks to your local API.

## Test

```sh
dotnet test server
```

## Endpoints

| Method & path | Description |
|---|---|
| `GET /api/health` | Liveness check |
| `GET /api/topics` | Topic list (`slug`, `name`) |
| `GET /api/feed?topic=&page=` | Article feed, 20 per page; `topic` is a slug from `/api/topics` |
| `GET /api/content/{id}` | One item by id (`sfn-…`, `apod-yyyy-MM-dd`, `img-…`) |
| `GET /api/apod?date=yyyy-MM-dd` | Picture of the day; latest when `date` is omitted |
| `GET /api/images?q=&page=` | NASA image search, 24 per page |

Errors use [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457). Invalid input returns `400`,
unknown content `404`, and an unreachable upstream source `502`.

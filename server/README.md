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

A Postgres database ([Neon](https://neon.com)) is optional: without one the API keeps extracted
articles in memory only. No Docker or API keys are needed.

### Configuration

| Setting | Purpose |
|---|---|
| `ConnectionStrings:Default` | Postgres connection (Npgsql `Host=…;Database=…;Username=…;Password=…;SSL Mode=Require` format). Stores extracted articles so they survive restarts. Use Neon's pooled host. |
| `ConnectionStrings:Migrations` | Optional direct (non-pooler) connection used to apply migrations; defaults to `Default`. |
| `Database:MigrateOnStartup` | Apply pending migrations when the API starts (default `true`). |
| `Images:SigningKey` | Secret used to sign `/api/img` URLs. Optional in development (a random key is generated per run, so image URLs from before a restart stop working until the page reloads). **Set it in production**, e.g. `dotnet user-secrets set "Images:SigningKey" "<long random string>"` locally or an app secret in Azure.

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

### Database

Keep connection strings out of git with user-secrets:

```sh
cd server/Novashelf.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=<pooler host>;Database=neondb;Username=…;Password=…;SSL Mode=Require"
dotnet user-secrets set "ConnectionStrings:Migrations" "Host=<direct host>;Database=neondb;Username=…;Password=…;SSL Mode=Require"
```

Migrations are applied on startup. To add one after changing the model (from the repo root):

```sh
dotnet tool restore
dotnet ef migrations add <Name> --project server/Novashelf.Api --output-dir Data/Migrations
```

Deploy the API in the same region as the database: every query is a network round trip.

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
| `GET /api/content/{id}/read` | Reader view: item, clean article HTML, byline, reading time, `extracted` |
| `GET /api/img?src=&sig=&w=` | Resized WebP copy of a signed image URL (as returned in `imageUrl`); `w` snaps to 320–2048 |

Errors use [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457). Invalid input returns `400`,
unknown content `404`, and an unreachable upstream source `502`.

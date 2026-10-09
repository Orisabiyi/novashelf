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
articles in memory only and accounts are switched off (account endpoints answer `503`). No Docker
or API keys are needed.

### Configuration

| Setting | Purpose |
|---|---|
| `ConnectionStrings:Default` | Postgres connection (Npgsql `Host=…;Database=…;Username=…;Password=…;SSL Mode=Require` format). Stores extracted articles so they survive restarts. Use Neon's pooled host. |
| `ConnectionStrings:Migrations` | Optional direct (non-pooler) connection used to apply migrations; defaults to `Default`. |
| `Database:MigrateOnStartup` | Apply pending migrations when the API starts (default `true`). |
| `Images:SigningKey` | Secret used to sign `/api/img` URLs. Optional in development (a random key is generated per run, so image URLs from before a restart stop working until the page reloads). **Set it in production**, e.g. `dotnet user-secrets set "Images:SigningKey" "<long random string>"` locally or an app secret in Azure. |
| `Proxy:Hops` | Number of proxies in front of the API whose `X-Forwarded-For` entries are trusted (default `2`: Vercel, then Azure's ingress). Used outside Development only, so the per-IP sign-in limit sees each visitor's own address. |

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

Keep connection strings out of git with user-secrets. The easiest way is the helper, which asks
for the `postgresql://…` URL from Neon without echoing it and sets both strings below:

```sh
./server/scripts/set-database.sh
```

Or set them by hand:

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
| `GET /api/learn` | Learning paths and their lessons |
| `GET /api/learn/{track}/{lesson}` | One lesson, with its text and sources |

### Accounts

Signed in with an HttpOnly cookie. Sign-in, sign-up, password changes and account deletion are
limited to 10 requests a minute per address, and an account locks for 5 minutes after 5 wrong
passwords.

| Method & path | Description |
|---|---|
| `POST /api/auth/register` | Create an account (`email`, `password`, `firstName`, `ageConfirmed`) and sign in |
| `POST /api/auth/login` | Sign in (`email`, `password`, `remember`) |
| `POST /api/auth/logout` | Sign out |
| `GET /api/me` | The signed-in reader, or `401` |
| `PATCH /api/me` | Change the first name |
| `POST /api/me/password` | Change the password (`currentPassword`, `newPassword`) |
| `DELETE /api/me` | Delete the account and everything saved with it (`password`) |
| `GET/POST/DELETE /api/me/history`, `DELETE /api/me/history/{id}` | Reading history |
| `GET/PUT /api/me/topics` | Followed topics; `GET /api/feed?topic=following` mixes them |
| `GET/PUT /api/me/preferences` | Reader settings (`fontSize`, `width`, `theme`); `204` until first saved |
| `GET /api/me/lessons`, `PUT/DELETE /api/me/lessons/{track}/{lesson}` | Finished lessons |

Errors use [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457). Invalid input returns `400`,
unknown content `404`, and an unreachable upstream source `502`.

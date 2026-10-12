# Novashelf

A calm place to read about space. Novashelf gathers spaceflight news, NASA's Astronomy Picture of
the Day and the NASA Image Library, and shows them in a clean, distraction-free reader. New to
space? The Learn section has short learning paths that start from the basics.

Everything is free to read without an account. Signing up adds:

- **Reading history**, with read markers on the feed and a "Continue reading" shelf
- **Followed topics** and a Following feed made from them
- **Reader settings** (text size, line width, theme) that follow you between devices
- **Lesson progress** in Learn

## How it fits together

```
novashelf/
  src/                     Vue 3 + Vite + Tailwind CSS front end
  server/Novashelf.Api/    ASP.NET Core (.NET 10) API
  server/Novashelf.Api.Tests/
```

The browser only ever talks to `/api` on the same site. The API is the only part that contacts
Spaceflight News and NASA. It normalises their content into one shape, extracts readable article
text, resizes images, and stores accounts and extracted articles in Postgres
([Neon](https://neon.com)).

| Content | Source |
|---|---|
| Articles | [Spaceflight News API](https://api.spaceflightnewsapi.net/v4/docs/) |
| Picture of the Day | science.nasa.gov (APOD, 1995 to today) |
| Images | [NASA Image and Video Library](https://images.nasa.gov) |
| Learn lessons | NASA Science pages and Simple English Wikipedia (CC BY-SA, credited on each lesson) |

## Run it locally

You need [Node.js](https://nodejs.org) 20.19+ with [pnpm](https://pnpm.io), and the
[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

1. Start the API (it listens on http://localhost:5088):

   ```sh
   dotnet run --project server/Novashelf.Api
   ```

2. In a second terminal, start the site:

   ```sh
   pnpm install
   pnpm dev
   ```

3. Open http://localhost:5173. Vite passes every `/api` request to the API.

Without a database, reading works but accounts are switched off. To connect one, see
[server/README.md](server/README.md#database).

## Tests and builds

```sh
dotnet test server   # API tests
pnpm build           # production build of the site, into dist/
```

## More

- [server/README.md](server/README.md): API configuration, endpoints and the database

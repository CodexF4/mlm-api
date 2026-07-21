# Deploying mlm-api to Render

The API runs as a Docker web service on [Render](https://render.com), backed by a
Render PostgreSQL instance. The Angular UI (Netlify) reaches it through a same-origin
`/api` proxy, so the auth cookie stays first-party.

## 1. Create the PostgreSQL database
1. Render dashboard → **New → PostgreSQL** (Free plan).
2. After it provisions, note the **Internal Database URL** (`postgres://…`). Use the
   *internal* URL so API↔DB traffic stays inside Render.

> ⚠️ Free Postgres has limited storage/connections and expires after a set period.
> Upgrade before storing anything you need to keep.

## 2. Create the API web service
1. Render dashboard → **New → Web Service** → connect this repo.
2. Runtime: **Docker** (Render uses the `Dockerfile` in the repo root).
3. Instance type: **Free**.
4. Health check path: **`/health`**.

## 3. Environment variables (Render → service → Environment)
| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DATABASE_URL` | the Internal Database URL from step 1 |
| `Jwt__Key` | a strong random secret (≥ 32 bytes) — generate a fresh one |
| `Jwt__Issuer` | `mlm-api` (optional; already in appsettings) |
| `Jwt__Audience` | `mlm-ui` (optional; already in appsettings) |
| `Authentication__Google__ClientId` | Google Web Client ID (optional override; already in appsettings — see Google sign-in below) |

`PORT` is injected by Render automatically — the app reads it and binds to
`0.0.0.0:$PORT`. Do **not** set `ASPNETCORE_URLS`.

Generate a key, e.g.:
```bash
openssl rand -base64 48
```

## 4. Deploy
Push to the connected branch. On boot the app:
- parses `DATABASE_URL` into an Npgsql connection string (SSL preferred),
- runs `Database.Migrate()` to apply pending EF migrations (creates the Identity +
  referral schema on first deploy),
- starts listening on `$PORT`.

Note the public service URL (e.g. `https://mlm-api.onrender.com`) — you'll point the
Netlify `/api` proxy at it (see the UI repo's `DEPLOY.md`).

## Google sign-in
The API verifies Google ID tokens on `POST /auth/google`; the audience it checks is
`Authentication:Google:ClientId`.

- The current Web Client ID is already set in `appsettings.json`
  (`565975617810-…apps.googleusercontent.com`) and is **not secret** — it's fine in the
  repo. Override per-environment with the env var `Authentication__Google__ClientId` if
  needed.
- It **must be the same** Client ID the UI uses (`mlm-ui/src/app/auth/google.config.ts`).
- In the [Google Cloud console](https://console.cloud.google.com/apis/credentials) for
  this OAuth client, add your public site to **Authorized JavaScript origins**:
  - `http://localhost:4200` (local dev)
  - your Netlify site URL (e.g. `https://<site>.netlify.app`) — and any custom domain.
  No redirect URIs are needed (the app uses the Google Identity Services ID-token flow).

## Notes
- **Cold starts:** free web services sleep after ~15 min idle; the first request then
  takes ~30–60s.
- **TLS:** Render terminates TLS at its edge, so the app sees plain http. It trusts
  `X-Forwarded-Proto` (via `UseForwardedHeaders`) so `Request.IsHttps` is correct and
  the auth cookie is issued with `Secure`. `UseHttpsRedirection` is dev-only.
- **CORS:** unused in production because the browser talks only to the Netlify origin
  (same-origin `/api` proxy). Configurable via `Cors:AllowedOrigins` if you ever expose
  the API cross-origin.
- **Secrets hygiene:** `appsettings.Development.json` (dev password + placeholder JWT
  key) is excluded from the image via `.dockerignore`. Rotate those values; never rely
  on them in production.

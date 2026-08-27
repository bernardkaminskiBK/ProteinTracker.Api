# Protein Tracker Web

React and TypeScript frontend for the Protein Tracker API, built with Vite.

## Requirements

- Node.js `^20.19.0` or `>=22.12.0`
- npm 10 or newer recommended
- Protein Tracker API running locally

## Configuration

Copy `.env.example` to `.env.local` when local overrides are needed:

```bash
cp .env.example .env.local
```

- `VITE_API_BASE_URL` is the API prefix used by the browser. It defaults to `/api`.
- `VITE_API_PROXY_TARGET` is the Vite development proxy target. It defaults to `http://localhost:5132`.

The development proxy lets the frontend call the existing API without requiring a backend CORS change. A production deployment should serve both applications under one origin, configure an equivalent reverse proxy, or set `VITE_API_BASE_URL` to an API origin that permits the frontend origin.

## Commands

```bash
npm install
npm run dev
npm run lint
npm run build
npm run preview
```

The development server normally starts at `http://localhost:5173`.

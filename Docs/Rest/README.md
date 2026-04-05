# REST Docs

This directory contains the REST collections and smoke-test examples for local API work.

## Quick Links

- PowerShell smoke tests: [PowerShell-Examples.md](PowerShell-Examples.md)
- Bruno collection: `Docs/Rest/bruno`
- Postman collection: `Docs/Rest/workoutlog.postman_collection.json`

## Local Base URLs

- Docker HTTPS: `https://localhost:8081`
- Docker HTTP: `http://localhost:8080`
- Rider / launch profile HTTPS: `https://localhost:6001`
- Rider / launch profile HTTP: `http://localhost:6000`
- OpenAPI JSON: `/openapi/v1.json` when the API is running in `Development`, for example through Rider

## Notes

- Use Bruno or Postman for full endpoint coverage.
- Use the PowerShell document for quick local smoke tests without importing a collection.
- For authenticated endpoints, store the returned `accessToken` after login, register, or refresh.
- Refresh and logout flows rely on the HTTP-only refresh cookie, so PowerShell examples use a `WebRequestSession`.
- `debugResetToken` is only returned when `ASPNETCORE_ENVIRONMENT=Development` and `AUTH_PASSWORD_RESET_INCLUDE_DEBUG_TOKEN=true`.

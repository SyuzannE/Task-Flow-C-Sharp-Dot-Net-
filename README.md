# TaskFlow

![CI](https://github.com/YOUR-USER/TaskFlow/actions/workflows/ci.yml/badge.svg)

A small but production-shaped **.NET 10** Web API demonstrating clean architecture, minimal APIs, EF Core, automated tests, CI and Docker.

## Architecture

```
Api  ──►  Infrastructure  ──►  Application  ──►  Domain
(HTTP)    (EF Core/SQLite)     (use cases)       (entities, rules)
```

| Project | Responsibility |
|---|---|
| `TaskFlow.Domain` | Entities and invariants, no dependencies |
| `TaskFlow.Application` | Use cases (`TodoService`), repository abstractions |
| `TaskFlow.Infrastructure` | EF Core `DbContext`, repository implementation |
| `TaskFlow.Api` | Minimal API endpoints, ProblemDetails, health checks, OpenAPI |
| `TaskFlow.Tests` | xUnit unit tests + `WebApplicationFactory` integration tests |

Highlights: central package management, `TimeProvider` for testable time, RFC 9457 problem responses, analyzers and `.editorconfig` enforced in CI, non-root Docker image, Dependabot.

## Getting started

```bash
dotnet run --project src/TaskFlow.Api
curl -X POST http://localhost:5000/api/todos -H 'Content-Type: application/json' -d '{"title":"Hello"}'
```

Use the port printed on startup. In Development the OpenAPI document is served at `/openapi/v1.json`.

### Tests

```bash
dotnet test
```

### Docker

```bash
docker build -t taskflow .
docker run -p 8080:8080 -v taskflow-data:/data taskflow
```

## API

| Method | Route | Description |
|---|---|---|
| GET | `/api/todos` | List todos |
| GET | `/api/todos/{id}` | Get one |
| POST | `/api/todos` | Create `{ "title": "..." }` |
| POST | `/api/todos/{id}/complete` | Mark complete |
| DELETE | `/api/todos/{id}` | Delete |
| GET | `/health` | Health check |

## License

MIT

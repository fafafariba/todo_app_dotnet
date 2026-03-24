# Todo App

A full-stack todo task management application.

> **Note:** This project was originally architected in [TypeScript](https://https://github.com/fafafariba/todo_app_typescript) (Node.js + Express + Prisma) and ported to .NET using [Claude Code](https://claude.ai/claude-code). The TypeScript version served as a prototype to validate architecture decisions before translating to the target stack.

## Tech Stack

- **Backend:** .NET 8, ASP.NET Core, Entity Framework Core, SQLite
- **Frontend:** React, TypeScript, Vite

---

## Prerequisites

- **.NET 8 SDK** — [download here](https://dotnet.microsoft.com/download/dotnet/8.0). Verify with `dotnet --version` (should show `8.x.x`).
- **Node.js 20** — for the React frontend. Verify with `node --version`.

## Setup

```bash
# Restore .NET packages
dotnet restore src/Api/Api.csproj

# Install frontend dependencies
npm install --prefix client

# Run the API (database is auto-created on first run)
dotnet run --project src/Api

# In a separate terminal, run the frontend
npm run dev --prefix client
```

API runs on `http://localhost:3000`
Frontend runs on `http://localhost:5173`

## Tests

```bash
# Backend tests (auth + todo integration tests)
dotnet test

# Frontend tests (component tests)
npm test --prefix client
```

---

## API

| Method | Route                   | Description         |
| ------ | ----------------------- | ------------------- |
| POST   | `/api/v1/auth/register` | Register a new user |
| POST   | `/api/v1/auth/login`    | Log in              |
| GET    | `/api/v1/todos`         | List todos          |
| POST   | `/api/v1/todos`         | Create a todo       |
| PATCH  | `/api/v1/todos/:id`     | Update a todo       |
| DELETE | `/api/v1/todos/:id`     | Soft delete a todo  |

---

## Architecture Decisions

### ORM: Entity Framework Core

EF Core is the standard ORM for .NET. It provides:

- **Code-first schema** — entity classes define the database schema, and EF Core generates the SQL. No hand-written migrations needed for the initial setup.
- **Global query filters** — soft-delete filtering (`WHERE DeletedAt IS NULL`) is applied automatically to every query via `HasQueryFilter`, so it can't be accidentally forgotten.
- **Auto-timestamps** — `CreatedAt` and `UpdatedAt` are set automatically in `SaveChangesAsync` by inspecting the change tracker.

### Database: SQLite

SQLite is a good fit for a self-contained MVP: no separate DB process to run, easy to inspect, and zero setup for anyone cloning the repo. The file is stored on disk so data persists between restarts.

For production at scale, this would be swapped for PostgreSQL by changing the EF Core provider — the rest of the code stays the same.

### Soft Deletes

Todos and users are never hard-deleted. Instead, a `DeletedAt` timestamp is set. EF Core's global query filters automatically exclude soft-deleted rows from all queries. This preserves data for potential undo/restore features and audit history.

### Completed State

Rather than a boolean `completed` flag, todos track a `CompletedAt` timestamp. This gives you the completion time for free and is more useful for sorting, filtering, and displaying history.

### Service Layer

The app uses a controller → service → EF Core architecture. The service layer encapsulates business logic and data access. As the app grows, this is where concerns like notifications, authorization rules, and side effects would live.

### Partial Updates and Nullable Fields

PATCH requests need to distinguish between "field was not sent" (don't change it) and "field was sent as null" (clear it). C# record deserialization loses this distinction — both cases result in `null`. To solve this, the controller accepts a raw `JsonElement` and builds a set of explicitly sent field names. The service uses this set to decide whether to update nullable fields like `CompletedAt` and `DueDate`. This avoids the complexity of JSON Patch while keeping a plain JSON request body.

### Update Granularity

The service currently exposes a single `UpdateAsync` method that handles all todo field changes — title, description, priority, due date, and completion state. This is sufficient for an MVP. As business complexity grows, this would be split into more specific operations (e.g. `CompleteTodoAsync`, `ChangePriorityAsync`) to support field-level validation, side effects like notifications, and more granular authorization rules.

### Validation: FluentValidation

Request validation uses FluentValidation rather than DataAnnotations. FluentValidation supports complex validation rules more cleanly than attribute-based validation, and keeps validation logic separate from the DTOs.

### Error Handling

Unexpected errors are handled by a global `IExceptionHandler` implementation rather than per-controller try/catch blocks. This avoids repetition across every action method and gives one place to change logging behaviour (e.g. swapping `Console.Error` for Serilog). Controllers focus on business logic and let errors bubble up naturally.

### API Versioning

Routes are prefixed with `/api/v1/`. If breaking changes are needed in the future, a `/api/v2/` prefix can be introduced without affecting existing clients.

### Token Storage

The frontend stores the JWT in localStorage for simplicity. localStorage is theoretically vulnerable to XSS (malicious scripts can read it), but React mitigates this by escaping output by default — you'd have to use `dangerouslySetInnerHTML` or load untrusted third-party scripts (analytics, ad trackers) to create an opening. For apps without third-party scripts, the risk is low. The more secure production pattern is httpOnly cookies, which the browser never exposes to JavaScript, paired with a `SameSite` cookie flag to prevent CSRF (where an attacker's site tricks the browser into making requests on the user's behalf).

---

## Scalability Considerations

- **Database indexes** — `Todos` has an index on `UserId` for fast per-user queries. At scale, composite indexes would be added for common query patterns: `(UserId, CreatedAt)` for the default sort, `(UserId, CompletedAt)` for filtering by completion status, `(UserId, Priority)` for priority-based views.
- **Check constraints** — SQLite doesn't enforce enum values at the database level. In production (PostgreSQL), check constraints like `CHECK (Priority IN ('LOW', 'MEDIUM', 'HIGH'))` would be added to prevent invalid data from being written, regardless of whether it comes through the API or direct database access.
- **Pagination** — the list endpoint currently returns all todos for a user. At scale, cursor-based pagination would be needed.

---

## How to Be Even More Production Ready

- **Email verification** — require users to verify their email address before allowing account creation. Send a verification link on registration; only activate the account once confirmed. This prevents spam accounts and ensures the user owns the email address.
- **Structured logging** — replace `Console.Error` with [Serilog](https://serilog.net/), which outputs structured JSON logs that can be ingested by a log aggregator (Datadog, Splunk, CloudWatch). Key considerations: log levels (debug/info/warn/error), sensitive data redaction (never log passwords or tokens), and correlation IDs to trace a request across multiple log lines.
- **Authentication** — currently uses a single JWT with a 1-day expiry. This is acceptable for an MVP but in production the standard pattern is short-lived access tokens (15–60 min) paired with long-lived refresh tokens (7–30 days). If an access token is stolen, the attacker's window is limited. Refresh tokens can be revoked (stored in DB, deleted on logout), which single JWTs cannot.
- **Auth error responses** — currently returns `401` with distinct messages for "wrong password" vs "email not found". In production, consider returning a generic `401 Invalid email or password` for both to avoid leaking whether an email address exists in the system — useful against enumeration attacks.
- **Pagination** on the list endpoint
- **Rate limiting** on auth endpoints to prevent brute force attacks
- **Input validation** — currently the backend validates all input with FluentValidation, and the frontend relies on HTML attributes (`required`, `type="email"`, `minLength`). A production app would add explicit client-side validation (e.g. with a library like Zod or React Hook Form) to catch malformed data before it hits the network — better UX and fewer unnecessary round trips. Backend validation remains essential for robustness, since requests can come from sources other than the UI (API clients, scripts, browser dev tools).
- **Security hardening** — EF Core uses parameterized queries by default, which protects against SQL injection. Additional production considerations: input sanitization to prevent XSS in stored data, CORS restricted to known origins (currently allows all), Content Security Policy headers, and regular dependency auditing for known vulnerabilities (`dotnet list package --vulnerable`).
- **More rigorous testing** — currently includes backend integration tests (WebApplicationFactory + SQLite in-memory) and frontend component tests (Vitest + Testing Library). Missing from this are end-to-end tests (Playwright or Cypress) that exercise the full stack, and a factory library for declarative test data setup.
- **Response serialization** — currently returning DTO records mapped from EF Core entities. A more robust approach would use AutoMapper or a dedicated serialization layer to ensure the API contract is fully decoupled from the database model.
- **Docker** setup for consistent environments
- **CI/CD** pipeline

---

## Further Improvements

- **Trash / restore** — soft deletes are already in place (`DeletedAt` timestamp), but the UI has no way to view or restore deleted todos. A production version would add a "Trash" view that lists soft-deleted items with the option to restore (set `DeletedAt` back to `null`) or permanently delete them. The API would need a `GET /api/v1/todos/trash` endpoint and a `PATCH /api/v1/todos/:id/restore` endpoint.
- **User account management** — allow users to update their email and password from a settings/profile page. Would require email re-verification on change and current password confirmation before updating credentials.
- **Sorting** — allow users to sort todos by priority, due date, or creation date. Could be a dropdown in the UI that passes a `sortBy` and `order` query parameter to the list endpoint.
- **Richer error logging** — introduce a custom exception class (e.g. `AppException`) that accepts a context dictionary alongside the message. Services could throw `new AppException("Todo update failed", new { todoId, userId })`, and the global exception handler would log the attached context. This makes debugging production issues much easier without scattering log calls throughout the codebase.

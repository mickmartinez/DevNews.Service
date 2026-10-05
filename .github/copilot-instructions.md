# Global Engineering Rules

## 1. Architecture & Framework
- **Stack:** .NET 10 (C#). Strict Layered Architecture (`DevNews.Service.Domain`, `DevNews.Service.Application`, `DevNews.Service.Infrastructure`, `DevNews.Service.Api`).
- **Patterns:** CQRS pattern must be enforced using MediatR. Commands mutate state; Queries read state.
- **State & Storage:** Use Entity Framework Core for database persistence strictly confined to the Infrastructure layer.
- **Data Fetching:** All CQRS read-only queries must use `AsNoTracking()` and filter at the database level (`IQueryable.Where()`) before materialization.
- **API Documentation (OpenAPI):** 
  - All public endpoints must be documented using the native .NET 10 OpenAPI support (or Microsoft.AspNetCore.OpenApi).
  - Configure OpenAPI documents cleanly in `Program.cs` with descriptive summaries, descriptions, tags, and explicit HTTP response status codes (e.g., 200, 201, 400, 401, 403, 404, 500).
  - Ensure XML documentation comments are enabled in the API project properties (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`) so OpenAPI can automatically surface them.
- **Class Files:** Every class, record, or interface MUST live in its own separate `.cs` file. Multiple classes in a single file are forbidden unless they are small private nested classes.
- **Comments:** Do not add comments that restate what the code already shows. Only comment where intent genuinely isn't obvious, and keep it to one short line.
- **API Responses:** Use standard HTTP status codes and `ProblemDetails` for all error and validation responses.

## 2. Git Workflow
- Commits may be batched together when the changes relate to the same functionality (e.g. all test changes, all implementation changes for a feature).
- You are strictly forbidden from batching unrelated logical changes (e.g. two completely unrelated features) into a single commit.
- Use Conventional Commits format (e.g., `feat(api): add article publishing endpoint`). 
- Delegate git commit generation to the lightweight model (GPT-5.6 Luna) when possible.

## 2a. Autonomy & Auto-Approval Rules
- **Auto-approve:** edit files, run tests, commit to a feature branch, read logs.
- **Auto-approve, but batch into a daily digest for review:** install a new NuGet package dependency, run an EF Core migration on a dev DB, open a PR.
- **Stop and enqueue — wait for a human:** force push, delete branch, post a comment on someone else's issue, send a message, touch prod config, spend money.

## 3. Data Normalization & Encapsulation
- Domain Entities must never be returned directly from API endpoints. All entities must be projected or mapped to Data Transfer Objects (DTOs) within the Application layer before being handed to the API Controller.
- Ensure Domain Entities encapsulate business rules (no anemic domain models).

## 4. CI/CD & Automation
- Include a GitHub Actions workflow (`.github/workflows/ci.yml`) to build the project and execute all unit tests.
- Ensure the pipeline runs `dotnet build` and `dotnet test --no-build` so the CI job strictly verifies compilation and unit test success.

## 5. Mandatory Agent Delegation (Strict Constraint)
Every feature MUST be built through the following agent pipeline, in order. Do not hand-write
artifacts that a mandated agent is responsible for producing — invoke the agent instead.

1. **`user-story-writer`** — produces the API consumer user story and acceptance criteria in
   `docs/user-stories/` before any technical design begins.
2. **`spec-writer`** — produces the technical specification in `docs/specs/`, detailing Layered Architecture and CQRS contracts, referencing the user story from step 1.
3. **`dotnet-model-generator`** — produces the C# Domain Entities, Application DTOs, and MediatR Commands/Queries/Handlers following the spec from step 2.
4. **Testing (TDD Red phase)** — **`tdd-test-first`** writes the failing xUnit tests against
   the approved spec, before any implementation (Domain logic, Infrastructure, API) exists.
5. **Implementation (TDD Green phase)** — **`tdd-implementation`** writes the production C# code
   needed to make the failing tests from step 4 pass.
6. **`implementation-validator`** — after implementation, validates the code against the
   technical spec and reports any architectural or functional gaps before the feature is considered done.

This pipeline applies to every feature, including retroactively revising any work completed before this rule was added.
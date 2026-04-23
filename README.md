# NorthWindTraders

Backend technical assessment solution built with ASP.NET Core and SQL Server Northwind.

## Overview

This solution implements customer-focused read APIs over the Northwind database with:

- clean layering (API, Application, Domain, Infrastructure)
- paginated customer overview and customer detail endpoints
- database-side order aggregations (totals, distinct product types)
- standardized global error responses
- automated tests for key query behavior

Primary endpoints:

- `GET /customers`
- `GET /customers/{id}`

---

## Architecture

Solution projects:

- `NorthWindTraders`  
  API host (controllers, DTO contracts, middleware, DI composition, Swagger)
- `NorthWindTraders.Application`  
  use-case contracts, query handlers, pagination primitives (`PageRequest`, `PagedResult<T>`)
- `NorthWindTraders.Domain`  
  persistence-agnostic domain types (IDs, core concepts, projections)
- `NorthWindTraders.Infrastructure`  
  EF Core DbContext, entities, SQL-backed data access implementations
- `NorthWindTraders.Application.Tests`  
  xUnit + FluentAssertions + EF InMemory unit tests

Dependency direction:

- API -> Application, Infrastructure
- Infrastructure -> Application (implements interfaces)
- Application -> Domain
- Domain -> (none)

This keeps controllers thin and persistence concerns out of the API/Application boundary.

---

## API Behavior

### `GET /customers`

Returns paged customer overview:

- customer id
- company name
- total order count (all historical orders)

Supports:

- `search` (case-insensitive, partial match)
- `page`
- `pageSize`

Pagination parameters are normalized via `PageRequest.Normalize(...)`.

### `GET /customers/{id}`

Returns:

- customer details
- paginated order summaries

Each order summary includes:

- `totalOrderValue = sum(UnitPrice * Quantity * (1 - Discount)) + Freight`
- `distinctProductTypeCount`
- `freight`

All calculations are translated into the database query (no navigation graph loading and no in-memory aggregation of full datasets).

---

## Standardized Error Model

Global exception middleware returns:

- `timestamp`
- `status`
- `errorCode`
- `message`
- `traceId`

Optional:

- `errors` dictionary (reserved for future validation error details)

Current examples:

- `NOT_FOUND` (customer missing)
- `INTERNAL_ERROR` (unexpected failures)

Design is ready for future:

- validation errors (`VALIDATION_FAILED`)
- authentication/authorization errors (`UNAUTHORIZED`, `FORBIDDEN`)

---

## Design Decisions

- **Thin controllers**: controllers orchestrate request binding + response mapping only.
- **Application-first contracts**: API depends on query handlers/interfaces, not EF.
- **Projection-based data access**: avoids loading navigation properties and reduces payload.
- **Pagination normalization**: centralized bounds and skip/take behavior.
- **Unified error envelope**: consistent client experience and easier observability.

---

## Tradeoffs Made

- **EF Core InMemory fallback** in API host when no connection string is set helps local startup, but behavior can differ from SQL Server translation details.
- **Offset pagination** (`Skip/Take`) is simple and acceptable for assessment scope; deep-page performance can degrade for very large datasets.
- **Current tests focus on query/aggregation correctness**, not full API integration via `WebApplicationFactory`.

---

## Scalability Considerations

Implemented:

- `AsNoTracking()` for read-only queries
- narrow projections instead of entity graph materialization
- server-side filtering, ordering, counting, and aggregation
- page size normalization (`PageRequest`)

Recommended DB indexes (if not already present):

- `Orders(CustomerID)`
- `[Order Details](OrderID, ProductID)`

Potential next-level improvements:

- keyset pagination for heavy deep paging
- compiled queries for hot paths
- read replicas + read-only connection routing

---

## Local Run (macOS)

### Prerequisites

- .NET 8 SDK
- Docker runtime on macOS (Docker CLI + Colima)

### 1) Start container runtime

```bash
colima start
```

### 2) Start SQL Server-compatible container

```bash
docker run -d --name northwind-sql \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=Northwind_123Strong!" \
  -e "MSSQL_PID=Developer" \
  -p 1433:1433 \
  mcr.microsoft.com/azure-sql-edge:latest
```

### 3) Download and load Northwind schema/data

```bash
mkdir -p setup/northwind
curl -L "https://raw.githubusercontent.com/microsoft/sql-server-samples/master/samples/databases/northwind-pubs/instnwnd.sql" -o setup/northwind/instnwnd.sql

docker run --rm --network host mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -b -S 127.0.0.1,1433 -U sa -P 'Northwind_123Strong!' \
  -Q "IF DB_ID('Northwind') IS NULL CREATE DATABASE Northwind;"

docker run --rm --network host -v "$PWD/setup/northwind:/scripts" mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -b -S 127.0.0.1,1433 -d Northwind -U sa -P 'Northwind_123Strong!' \
  -i /scripts/instnwnd.sql
```

### 4) Configure connection string

`NorthWindTraders/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Northwind": "Server=localhost,1433;Database=Northwind;User Id=sa;Password=Northwind_123Strong!;TrustServerCertificate=True;Encrypt=False"
  }
}
```

### 5) Run API

```bash
dotnet run --project NorthWindTraders/NorthWindTraders.csproj
```

Swagger UI is available in Development environment.

---

## Example Requests

```bash
curl "http://localhost:5000/customers?page=1&pageSize=5&search=alf"
curl "http://localhost:5000/customers/ALFKI?orderPage=1&orderPageSize=5"
```

---

## Running Tests

```bash
dotnet test NorthWindTraders.Application.Tests/NorthWindTraders.Application.Tests.csproj
```

Current tests cover:

- customer overview search behavior (partial + case-insensitive)
- customer overview pagination
- customer detail order total + distinct product type calculations
- customer detail order pagination

---

## Future Improvements

- Add API integration tests (`WebApplicationFactory`) for controller + middleware behavior
- Introduce FluentValidation and map validation failures into `errors` field
- Add authn/authz (JWT + policy-based authorization)
- Add OpenAPI examples/response schema docs for errors
- Add migrations strategy and startup health checks (`/healthz`)
- Introduce keyset pagination option for large ordered datasets
- Add caching for frequently requested read paths

---

## Prompt Strategy

The solution was built iteratively using a prompt sequence that follows a testable implementation flow:

- Prompt 1 -> Architecture foundation
- Prompt 2 -> Reusable pagination model
- Prompt 3 -> Customer overview failing tests (search + pagination)
- Prompt 4 -> Customer overview implementation (green)
- Prompt 5 -> Customer detail aggregation red tests
- Prompt 6 -> Customer detail implementation (green)
- Prompt 7 -> Controllers and API wiring
- Prompt 8 -> Standardized error handling middleware
- Prompt 9 -> API/runtime setup and smoke verification
- Prompt 10 -> Documentation and final README

This strategy keeps scope controlled while preserving traceability from requirement -> test -> implementation.

---

## Prompts Used

> The prompts below are the condensed step prompts used to drive each implementation stage.

### Prompt 1 - Architecture

Design a clean architecture solution for a Northwind backend service. Include project layout, layer responsibilities, DTO strategy, query strategy, pagination strategy, error handling, test strategy, performance/scalability considerations, and use minimal APIs or controllers where appropriate. One very imporant note which is left in the assigment is "A simple, focused, well-reasoned solution will impress us more than a complex one" - Keep that in mind !

### Prompt 2 - Pagination model

Create reusable pagination infrastructure with `PageRequest` and `PagedResult<T>` for the Application layer. Include `pageNumber`, `pageSize`, `totalCount`, `totalPages`, and `items`. Keep it framework-agnostic (no ASP.NET Core dependencies).

### Prompt 3 - Customer overview tests first

Write failing unit tests first for `CustomerOverviewQueryHandler` using xUnit + FluentAssertions + EF InMemory:

- partial case-insensitive search (`alf` -> Alfreds Futterkiste)
- case-insensitive search (`ANNA` -> Anna's Food Market)
- pagination behavior

### Prompt 4 - Customer overview implementation

Implement `CustomerOverviewQueryHandler` and data access using projections only, case-insensitive partial search across required fields, order count per customer, and pagination optimized for scalability.

### Prompt 5 - Customer detail aggregation tests first

Write failing unit tests first for `CustomerDetailQueryHandler` order summary aggregation:

- total value = `sum(UnitPrice * Quantity * (1 - Discount)) + Freight`
- distinct product type count per order
- pagination on order history

### Prompt 6 - Customer detail implementation

Implement `CustomerDetailQueryHandler` and data access so that all aggregation logic is translated to SQL (not in-memory), with projection-based queries, customer header + paginated order summaries, and scalability-focused query shaping.

### Prompt 7 - Controllers

Generate ASP.NET Core controllers for:

- `GET /customers`
- `GET /customers/{id}`

Keep controllers thin, use Application layer handlers only, normalize paging via `PageRequest`, and return DTO responses only (no EF usage in controllers).

### Prompt 8 - Error handling

Design and implement a production-ready standardized API error model and global exception middleware with response fields:

- `timestamp`
- `status`
- `errorCode`
- `message`
- `traceId`

Keep the model extensible for validation and authorization errors and integrate middleware cleanly in `Program.cs`.

### Prompt 9 - Setup and smoke verification

Set up local runtime on macOS (Docker/Colima + SQL container + Northwind script load), configure connection strings, and verify endpoints with smoke calls.

### Prompt 10 - README

Generate a professional `README.md` covering architecture, local run, database setup, design decisions, tradeoffs, scalability, and future improvements.

---

## Reference

Northwind sample database source:

- [Microsoft Learn: Downloading sample databases](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/downloading-sample-databases)
- [SQL Server Samples: northwind-pubs](https://github.com/microsoft/sql-server-samples/tree/master/samples/databases/northwind-pubs)

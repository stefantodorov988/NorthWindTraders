# Northwind Customer API — Clean Architecture Plan (.NET)

This document describes a backend service over the **Microsoft Northwind** database using **ASP.NET Core Minimal APIs**, aligned with the functional and non-functional requirements and clarifications (partial/case-insensitive name filter across name fields, full historical order counts, order totals including line discounts and freight, “products” = distinct product types, paginated order history, standardized errors, auth-ready seams, scalability beyond sample data).

---

## 1. Solution and project layout

Recommended solution shape (one deployable API, clear boundaries):

```text
Northwind.Assessment.sln
  src/
    Northwind.Api/                 # Host: Minimal API endpoints, DI composition, middleware
    Northwind.Application/         # Use cases, queries, handlers, pagination contracts, validation
    Northwind.Domain/              # Entities/value objects (optional if thin domain)
    Northwind.Contracts/           # (Optional) Public API DTOs + shared error shapes
    Northwind.Infrastructure/      # EF Core, DbContext, migrations, query implementations
  tests/
    Northwind.Api.Tests/           # WebApplicationFactory + integration tests (≥1 scenario)
    Northwind.Application.Tests/   # (Optional) unit tests for pure logic
```

**Dependency rule:** `Api` → `Application` + `Infrastructure` (composition root). `Application` → `Domain` only; **must not** reference `Infrastructure` or `Api`. `Infrastructure` implements interfaces (ports) defined in `Application`.

---

## 2. Layer responsibilities

| Layer | Responsibility |
|--------|----------------|
| **Api** | Routes, model binding, HTTP validation, status codes, OpenAPI, correlation IDs, exception → RFC 7807 mapping, registration of auth middleware (policies for later). No business rules. |
| **Application** | Use cases: list customers with order counts; get customer detail with paginated orders. Defines **ports** (`ICustomerReadRepository`, etc.), **queries**, **pagination in/out**, **read models**. |
| **Domain** | Customer/order concepts if modeling rich behavior; for read-heavy scope, may remain thin. |
| **Infrastructure** | EF Core `DbContext`, configurations, projections, optional compiled/raw SQL, indexes, migrations. |
| **Tests** | Prove at least one critical path end-to-end (see [Test strategy](#8-test-strategy)). |

**Auth extensibility:** Register `AddAuthentication` / `AddAuthorization` with JWT bearer (or stub) and **policy-based** checks. Prefer an `ICurrentUser` port in `Application`, implemented at the edge (`Api`/`Infrastructure`), so handlers stay testable without `HttpContext`.

---

## 3. DTO strategy

**Principle:** HTTP DTOs ≠ persistence models ≠ application read models.

- **Request DTOs (Api):** e.g. list customers: `page`, `pageSize`, `search`; order history: `page`, `pageSize`, optional sort.
- **Response DTOs (Api or Contracts):** stable JSON; do not expose EF navigation graphs.
- **Application read models:** `CustomerOverviewItem`, `CustomerDetail`, `OrderSummaryRow` — flat, query-shaped types.
- **Mapping:** at the Api boundary (manual or a mapper without leaking into Application).

**Northwind-oriented shapes**

- **Overview:** customer identifiers/display fields + **`orderCount`** (all historical orders; no status/date filter unless added later).
- **Order summary (per order):** `orderId`, dates as needed, **`orderTotal`**, **`distinctProductTypeCount`**, **`freight`**, line-level discount data on line items if required by the API contract.

**Order total (per clarifications)**

- Lines: \(\sum (\text{UnitPrice} \times \text{Quantity} \times (1 - \text{Discount}))\) per `OrderID`.
- **Include freight** once per order in the displayed total (line discounts yes; freight yes).

**“Number of products”**

- **Distinct product types** per order: e.g. `COUNT(DISTINCT ProductID)` over `Order Details` for that order.

---

## 4. Query strategy

**Default:** EF Core with **`Select` projections** to DTOs/read models; avoid loading full entities for lists.

**Customer overview (list + order counts)**

- One round-trip pattern: `Customers` **left join** `(SELECT CustomerId, COUNT(*) FROM Orders GROUP BY CustomerId)` (or equivalent LINQ that translates efficiently).

**Name filter (partial, case-insensitive, all name fields)**

- Combine with `OR` across agreed columns (minimum **`CompanyName`**, **`ContactName`**; align “all name fields” with stakeholders—e.g. whether **`ContactTitle`** counts as a name field).
- Use SQL Server’s typical CI collation with `Contains`/`Like`, or `EF.Functions.Collate` if case sensitivity must be forced.

**Customer detail + paginated order history**

- **Page orders in the database** (never load full history into memory).
- Per page, compute per-order aggregates via join or grouped subquery on `Order Details`: line total sum, distinct product count; attach `Orders.Freight`.

**Scale options (later)**

- Compiled queries; Dapper behind the same repository interfaces; read-only connection (`ApplicationIntent=ReadOnly`) for replicas.

---

## 5. Pagination strategy

**Envelope (consistent for all lists):**

```json
{
  "items": [],
  "page": 2,
  "pageSize": 25,
  "totalCount": 12345
}
```

**Rules**

- Enforce a **maximum `pageSize`** (e.g. 100).
- **`totalCount`:** separate count query with **identical filters** (exact totals; ensure indexes support filter + join patterns).
- **Order history default sort:** e.g. `OrderDate DESC`, `OrderId DESC` for stable pages.

**Deep pagination at very large scale:** document keyset pagination as a future improvement; keep sort keys stable if migrating.

---

## 6. Error handling strategy

- **RFC 7807 Problem Details** (`application/problem+json`) as the single client-facing error shape.
- **Api layer:** global exception handler or middleware maps `NotFoundException` → 404, validation → 400, unexpected → 500 with safe `detail`; log full exceptions server-side.
- Include **`traceId`** / **correlation id** on every error payload.
- **Application:** prefer typed results or domain-specific exceptions mapped in Api—not generic exceptions for control flow.

---

## 7. Minimal API surface

**Example routes**

- `GET /customers?page=&pageSize=&search=`
- `GET /customers/{customerId}`
- `GET /customers/{customerId}/orders?page=&pageSize=` — paginated order summaries (keeps detail responses bounded).

**Organization**

- `Program.cs`: composition only.
- `Endpoints/CustomerEndpoints.cs`: `MapCustomerEndpoints(this WebApplication app)`.
- Validation: endpoint filters or FluentValidation.

**OpenAPI:** `AddEndpointsApiExplorer` + Swashbuckle where appropriate for the assessment environment.

---

## 8. Test strategy

**Minimum:** **one integration test** using `WebApplicationFactory<Program>` against **Testcontainers (SQL Server)** or a dedicated test database.

**High-value scenario:** seed or use known Northwind rows; call `GET /customers/{id}/orders`; assert **`orderTotal`** (lines after discount + freight), **`distinctProductTypeCount`**, **`freight`**, and pagination metadata.

**Optional:** unit tests for a pure “order total calculator” if extracted for testability.

---

## 9. Performance and scalability

**Indexing (conceptual)**

- `Orders(CustomerID)`
- `Order Details(OrderID, ProductID)` for aggregates and distinct counts
- If text search dominates at scale, plan full-text or dedicated search columns (phase 2). `LIKE '%term%'` is acceptable for sample sizes but document trade-offs.

**API**

- Always project narrow columns; avoid N+1 (target constant query count per request).

**Caching**

- Not required initially; prefer correct indexes and efficient queries first.

**Observability**

- Structured logging; optional metrics (duration, DB command count).

---

## 10. Requirement traceability

| Requirement | Primary location |
|-------------|------------------|
| Customers + order counts + pagination + name filter | Application query + Infrastructure + `GET /customers` |
| All historical orders in count | Count without status/date filters |
| Totals incl. line discounts + freight; product types | Grouped `Order Details` + `Orders.Freight`; distinct `ProductID` |
| Paginated order history | Nested orders route (or equivalent) |
| Standardized errors | Api Problem Details |
| Auth later | Policies + `ICurrentUser` port |
| Clean architecture | Dependency direction + ports/adapters |
| Automated test | Api integration test project |

---

## 11. Implementation order

1. Infrastructure: `DbContext`, configurations, read repositories, list/count and paged order queries.  
2. Application: handlers, pagination envelope, validation.  
3. Api: minimal endpoints, Problem Details, OpenAPI, optional auth wiring.  
4. Tests: integration test for totals + pagination + filter behavior.  
5. README (if required by submission): connection string, migrations, how to run tests.

---

## Document history

- Created from the technical assessment architecture discussion and Q&A clarifications (Veselin).

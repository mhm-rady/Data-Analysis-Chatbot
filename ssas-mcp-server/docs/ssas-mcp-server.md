## ssas-mcp-server — Documentation

This document describes the ssas-mcp-server solution: structure, technologies, subprojects and a reference of the public classes and members. It's written to help humans and agentic code generators understand the repository and its public API surface.

## Table of Contents

- Project overview
- Technology & packages
- Repository structure
  - SsasMcpServer.Core
  - SsasMcpServer.Models
  - SsasMcpServer.Services
  - SsasMcpServer.Tools
- Public types and members (by project)
  - Models (Configuration, Metadata, Query, Exceptions)
  - Services (Interfaces)
- How to run (quick)
- Contract summary for agentic code generators
- Notes & next steps

## Project overview

ssas-mcp-server is a .NET solution that provides a small Microservice Control Plane (MCP) for querying and exposing metadata from a SQL Server Analysis Services (SSAS) Tabular model. The solution is split into multiple projects to separate concerns: models, services that implement SSAS access and DAX building, a small host runner in `Core`, and tooling.

## Technology & packages

- Target framework: .NET 9.0 (all projects target `net9.0`).
- Key NuGet packages (from project files):
  - Microsoft.Extensions.Hosting (Core)
  - Serilog.Extensions.Hosting, Serilog.Sinks.Console, Serilog.Sinks.File (Core logging)
  - ModelContextProtocol (Core)
  - System.Text.Json (Core + Models)
  - Microsoft.AnalysisServices.AdomdClient, Microsoft.AnalysisServices (Services) — ADOMD and Tabular server APIs
  - Microsoft.Extensions.Caching.Memory, Configuration, Options (Services DI & config)

If you need exact package versions, check the `*.csproj` files in each project.

## Repository structure

Top-level projects in the solution (brief):

- `SsasMcpServer.Core/` — executable host that wires up DI, configuration, runs a sample query using the registered services. Contains `Program.cs` and a `Mocks` folder for mock implementations.
- `SsasMcpServer.Models/` — POCO model classes used across the solution: configuration objects, metadata types, query request/result classes and specialized exceptions.
- `SsasMcpServer.Services/` — service registration and the public service interfaces along with concrete implementations (SSAS connection, metadata extraction, DAX builder, query execution).
- `SsasMcpServer.Tools/` — helper tooling project (contains shared tools; minimal in current snapshot).

### SsasMcpServer.Core

Purpose: host runner and demo. Key files:

- `Program.cs` — builds a generic host with default configuration sources, calls `services.AddSsasServices(configuration)`, resolves `IQueryService`, executes a sample `QueryRequest`, prints JSON result.
- `Mocks/MockQueryService.cs` — a mock implementation of `IQueryService` used for demo/testing without a real SSAS backend.

### SsasMcpServer.Models

Purpose: strongly-typed models for configuration, metadata, queries and exceptions. Contains:

- `Configuration/`
  - `McpServerOptions` — server-level settings.
    - public const string SectionName = "McpServer"
    - Name : string
    - Version : string
    - MaxResultRows : int
    - DefaultCulture : string
    - SupportedCultures : List<string>

  - `SsasConnectionOptions` — SSAS connection settings.
    - public const string SectionName = "SsasConnection"
    - Server : string (required)
    - Database : string (required)
    - Timeout : int
    - UseWindowsAuth : bool
    - Username : string?
    - Password : string?
    - BuildConnectionString() : string — helper that constructs an ADOMD/Tabular connection string based on options.

- `Metadata/`
  - `CultureInfo` — Culture, Name?, Description?
  - `AttributeMetadata` — TechnicalName, DisplayName, Description, Cultures (List<CultureInfo>), DataType, IsKey, IsHidden, FormatString
  - `HierarchyLevelMetadata` — TechnicalName, DisplayName, SourceAttribute, Ordinal
  - `HierarchyMetadata` — TechnicalName, DisplayName, Description, Cultures, Levels (List<HierarchyLevelMetadata>), IsHidden
  - `DimensionMetadata` — TechnicalName, DisplayName, Description, Cultures, Attributes, Hierarchies, IsHidden
  - `MeasureMetadata` — TechnicalName, DisplayName, Description, Cultures, FormatString, Expression, IsHidden, DisplayFolder
  - `KpiMetadata` — TechnicalName, DisplayName, Description, Cultures, ValueExpression, GoalExpression, StatusExpression, TrendExpression, DisplayFolder
  - `TabularModelMetadata` — DatabaseName, ModelDescription?, SupportedCultures, Dimensions, Measures, Kpis, LastProcessed, CompatibilityLevel

- `Query/`
  - `QueryRequest` — Columns (List<string>), Measures (List<string>), Filters (List<QueryFilter>), Sorts (List<QuerySort>), TopN?, MaxRows (int), Culture?
  - `QueryFilter` — Column (string), Operator (FilterOperator enum), Value (object)
    - `FilterOperator` enum values: Equals, NotEquals, GreaterThan, GreaterThanOrEquals, LessThan, LessThanOrEquals, Contains, StartsWith, EndsWith, In, NotIn
  - `QuerySort` — Column (string), Direction (SortDirection enum)
    - `SortDirection` enum: Ascending, Descending
  - `QueryResult` — ColumnNames (List<string>), Rows (List<Dictionary<string, object?>>), TotalRows (int), IsTruncated (bool), DaxQuery (string?), ExecutionTime (TimeSpan)

- `Exceptions/`
  - `SsasException` : Exception — base for other exceptions
  - `MetadataNotFoundException` : SsasException — constructed with objectName
  - `QueryExecutionException` : SsasException

### SsasMcpServer.Services

Purpose: public service interfaces and the concrete implementations that interact with SSAS and build/execute DAX queries. Important items:

- `ServiceCollectionExtensions.cs` — extension method `AddSsasServices(this IServiceCollection services, IConfiguration configuration)` registers configuration, memory cache and singleton services:
  - Configures `SsasConnectionOptions` and `McpServerOptions` from configuration sections
  - Registers `ISsasConnectionService` -> `SsasConnectionService`
  - Registers `ICultureService` -> `CultureService`
  - Registers `IMetadataService` -> `MetadataService`
  - Registers `IDaxQueryBuilder` -> `DaxQueryBuilder`
  - Registers `IQueryService` -> `QueryService`

- `Interfaces/` — public service contracts (all are asynchronous where appropriate):
  - `ICultureService`
    - string GetLocalizedName(string technicalName, List<Models.Metadata.CultureInfo> cultures, string? culture)
    - string? GetLocalizedDescription(List<Models.Metadata.CultureInfo> cultures, string? culture)
    - bool IsCultureSupported(string culture)
    - string GetDefaultCulture()
    - string NormalizeCultureCode(string culture)

  - `IDaxQueryBuilder`
    - string BuildQuery(QueryRequest request)
    - string BuildFilterExpression(QueryFilter filter)
    - string BuildOrderByClause(List<QuerySort> sorts)
    - string EscapeDaxIdentifier(string identifier)
    - string FormatDaxValue(object value, FilterOperator op)

  - `IMetadataService`
    - Task<TabularModelMetadata> GetModelMetadataAsync(string? culture = null, CancellationToken cancellationToken = default)
    - Task<List<DimensionMetadata>> GetDimensionsAsync(string? culture = null, CancellationToken cancellationToken = default)
    - Task<DimensionMetadata> GetDimensionAsync(string dimensionName, string? culture = null, CancellationToken cancellationToken = default)
    - Task<List<MeasureMetadata>> GetMeasuresAsync(string? culture = null, CancellationToken cancellationToken = default)
    - Task<List<MeasureMetadata>> GetMeasuresByNamesAsync(List<string> measureNames, string? culture = null, CancellationToken cancellationToken = default)
    - Task<List<KpiMetadata>> GetKpisAsync(string? culture = null, CancellationToken cancellationToken = default)
    - Task<List<HierarchyMetadata>> GetAllHierarchiesAsync(string? culture = null, CancellationToken cancellationToken = default)
    - Task<List<string>> GetSupportedCulturesAsync(CancellationToken cancellationToken = default)
    - Task RefreshMetadataAsync(CancellationToken cancellationToken = default)

  - `IQueryService`
    - Task<QueryResult> ExecuteQueryAsync(QueryRequest request, CancellationToken cancellationToken = default)
    - Task<string> GenerateDaxQueryAsync(QueryRequest request, CancellationToken cancellationToken = default)
    - Task<(bool IsValid, List<string> Errors)> ValidateQueryRequestAsync(QueryRequest request, CancellationToken cancellationToken = default)
    - Task<QueryResult> ExecuteDaxQueryAsync(string daxQuery, int maxRows = 1000, CancellationToken cancellationToken = default)

  - `ISsasConnectionService : IDisposable`
    - Task<AdomdConnection> GetAdomdConnectionAsync(CancellationToken cancellationToken = default)
    - Task<Server> GetTabularServerAsync(CancellationToken cancellationToken = default)
    - Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    - string GetDatabaseName()

Note: Concrete implementations live in `Services/Implementations/` (e.g., `QueryService`, `MetadataService`, `SsasConnectionService`, `DaxQueryBuilder`, `CultureService`). Those implementations are responsible for talking to ADOMD, Tabular objects and for caching metadata.

### SsasMcpServer.Tools

Purpose: small tooling support library. Contains references and helpers; not central to the API surface.

## Tools

This project exposes a set of MCP tools for inspecting model metadata and running DAX queries. The tools map to the public service methods in `IMetadataService` and `IQueryService` and accept/return the POCO models defined in `SsasMcpServer.Models` (notably the `QueryRequest` and `QueryResult` shapes).

Available tools (name → purpose):

- `mcp_ssas-mcp-serv_get_model_metadata` — Retrieve the complete tabular model metadata (database name, supported cultures, dimensions, measures, KPIs, last processed timestamp, compatibility level).
- `mcp_ssas-mcp-serv_get_dimensions` — List all dimensions with their attributes and hierarchies.
- `mcp_ssas-mcp-serv_get_dimension` — Get a single dimension's detailed metadata. Parameters: `dimensionName` (string), optional `culture` (string).
- `mcp_ssas-mcp-serv_get_measures` — List all measures, their DAX expressions and format strings.
- `mcp_ssas-mcp-serv_get_kp_is` — List configured KPIs.
- `mcp_ssas-mcp-serv_get_hierarchies` — Retrieve all hierarchies and levels across dimensions.
- `mcp_ssas-mcp-serv_generate_dax_query` — Generate DAX text from a `QueryRequest` without executing it.
- `mcp_ssas-mcp-serv_validate_query` — Validate a `QueryRequest` against model metadata (useful before execution).
- `mcp_ssas-mcp-serv_execute_query` — Execute a query built from `QueryRequest` and return a `QueryResult` (rows, column names, DAX used, execution time).

Notes and usage guidance:

- The tools accept model types from `SsasMcpServer.Models`. Construct a `QueryRequest` with `Columns`, `Measures`, `Filters`, `Sorts`, `TopN`, `MaxRows`, and optional `Culture`.
- Filters use the `FilterOperator` enum (Equals, Contains, In, etc.). Use `IDaxQueryBuilder` conventions (identifier escaping) when composing raw DAX strings.
- For culture-sensitive names or descriptions, pass `culture` = `en-US` (or any supported culture reported by the model metadata).
- The `execute` tool will honor `MaxRows` and return `QueryResult.IsTruncated` when results are truncated.

Small example `QueryRequest` (JSON form):

```json
{
  "Columns": ["Date[Calendar Year]"],
  "Measures": ["Internet Sales[Internet Total Sales]"],
  "Filters": [ { "Column": "Date[Calendar Year]", "Operator": "Equals", "Value": 2014 } ],
  "Sorts": [ { "Column": "Date[Calendar Year]", "Direction": "Ascending" } ],
  "MaxRows": 1000,
  "Culture": "en-US"
}
```

Typical flow:

1. Call `mcp_ssas-mcp-serv_get_model_metadata` to inspect available dimensions/measures and supported cultures.
2. Construct a `QueryRequest` (using the exact column/measure names from metadata).
3. Optionally call `mcp_ssas-mcp-serv_generate_dax_query` to preview the DAX text.
4. Call `mcp_ssas-mcp-serv_validate_query` to catch issues early.
5. Call `mcp_ssas-mcp-serv_execute_query` to run the query and receive a `QueryResult`.

If you expose these tools over HTTP (recommended for integration tests), map each tool to a single endpoint that accepts/returns JSON using the same model contracts.

## How to run (quick)

1. Configure `appsettings.json` or `appsettings.Development.json` with the `SsasConnection` section and optionally `McpServer` section.
2. Build and run `SsasMcpServer.Core` (this project uses `Host.CreateDefaultBuilder` and `AddSsasServices` to register services). `Program.cs` contains a small example that constructs a `QueryRequest` and prints serialized JSON results.

Example (already wired in `Program.cs`):
- Program resolves `IQueryService` from DI and calls `ExecuteQueryAsync(QueryRequest)`; it prints a JSON representation of the returned `QueryResult`.

## Contract summary for agentic code generators

When generating code or tests that integrate with this project, treat the following as the definitive contracts:

- Configuration: `SsasConnectionOptions.SectionName` = "SsasConnection" and `McpServerOptions.SectionName` = "McpServer". Use the builder `BuildConnectionString()` when constructing Adomd connections from options.
- Core entrypoint expects DI registration via `AddSsasServices(IConfiguration)`.
- Use `IQueryService.ExecuteQueryAsync(QueryRequest)` to run queries. `QueryRequest` is expressive but conservative: Columns and Measures must contain valid DAX column/measure identifiers (the project’s `IDaxQueryBuilder` provides escaping/formatting helpers).

Input/Output shapes:
- QueryRequest => QueryResult
  - QueryRequest: columns, measures, filters, sorts, topN, maxRows, culture
  - QueryResult: column names, rows (list of dictionaries keyed by column name), total rows, DAX used, execution time

Error modes:
- `SsasException` base class used for domain errors. Implementations throw `QueryExecutionException` for runtime query failures and `MetadataNotFoundException` when metadata lookups fail.

Edge cases to handle in generators/tests:
- Empty columns/measures — builder may throw or return empty results; validate via `IQueryService.ValidateQueryRequestAsync` before execution.
- Large result sets — `QueryResult.IsTruncated` and `QueryResult.TotalRows` indicate truncation; honor `MaxRows`.
- Culture normalization — prefer `ICultureService.NormalizeCultureCode` and `GetDefaultCulture`.
- Connection auth: `SsasConnectionOptions.UseWindowsAuth` toggles Windows integrated auth vs username/password.

## Notes & next steps

- For total accuracy of implementation details, consult the concrete implementation files in `SsasMcpServer.Services/Implementations/`.
- Suggested small improvements (low-risk):
  - Add XML doc comments to implementation methods to improve discoverability.
  - Add unit tests for `IDaxQueryBuilder` to validate escaping/formatting logic.
  - Add an OpenAPI or lightweight HTTP layer in `Core` to expose `IQueryService` over REST for integration testing.

## Where to look in the codebase

- DI registration: `SsasMcpServer.Services/ServiceCollectionExtensions.cs`
- Host/demo: `SsasMcpServer.Core/Program.cs`
- Models: `SsasMcpServer.Models/` (Configuration, Metadata, Query)
- Service contracts: `SsasMcpServer.Services/Interfaces/`
- Implementations: `SsasMcpServer.Services/Implementations/`

---

This documentation was generated from the source files included in the repository. For any generated code or tests, prefer using the interfaces in `SsasMcpServer.Services.Interfaces` and the models in `SsasMcpServer.Models` as the stable contract.

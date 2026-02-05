# GitHub Copilot Instructions for Kepware-ConfigAPI-SDK-dotnet

## Project Architecture

- **Multi-project Solution:**
  - `Kepware.Api`: .NET SDK for Kepware Configuration REST API.
  - `KepwareSync.Service`: CLI/service for bidirectional sync between Kepware servers and local filesystems (supports GitOps workflows, overwrite YAMLs, and environment variable substitution).
  - `Kepware.Api.Sample`: Example usage of the SDK.
  - `Kepware.Api.Test`, `Kepware.Api.TestIntg`: Unit and integration tests.
- **Data Flow:**
  - API client (`KepwareApiClient`) interacts with Kepware REST endpoints using DTOs inheriting `BaseEntity` and decorated with `EndpointAttribute`.
  - Sync service uses configuration files (`appsettings.json`, YAML overwrites) and environment variables for flexible deployment.

## General Guidelines
You are a senior .NET developer working on `Kepware-ConfigAPI-SDK-dotnet`, a .NET SDK for interacting with the Kepware Configuration API. Your code should be efficient, maintainable, and compatible with modern .NET standards.

## Target Frameworks
- The SDK must be compatible with **.NET 8** and **.NET 9**.
- Ensure **Nullable Reference Types** are enabled (`#nullable enable`).
- Use **AOT Compilation with Trimming** where applicable, particularly for serialization.
- Ensure **Invariant Globalization** is enabled.
- Treat **all warnings as errors** (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).

## Developer Workflow
- **Build:** Use `dotnet build` for all projects. Treat warnings as errors.
- **Test:** Run unit tests with `dotnet test Kepware.Api.Test` and integration tests with `dotnet test Kepware.Api.TestIntg`. Code coverage via `coverlet.collector`.
- **Run Sync Service:** CLI: `Kepware.SyncService [command] [options]` (see README for options). Docker: Use provided `Dockerfile` and `docker-compose.yml` for containerized deployment.
- **Configuration:** Prefer environment variables for secrets and deployment config. Use YAML overwrite files for dynamic config (supports env var substitution).

## Code Style and Structure
- Write concise, idiomatic **C#** code following .NET best practices.
- Use **LINQ and functional programming patterns** where appropriate.
- Prefer **immutable data structures** and **records** where applicable.
- Organize code into **proper namespaces** and **structured folders**.
- Follow standard **SOLID principles** and **Dependency Injection**.
- Follow current implementation style in this repo

## Naming Conventions
- Use **PascalCase** for classes, methods, and public members.
- Use **camelCase** for local variables and private fields.
- Use **UPPERCASE** for constants.
- Prefix **interfaces with 'I'** (e.g., `IConfigApiClient`).

## API Design
- Follow **RESTful API design** principles.
- Use **HttpClientFactory** for managing API calls.
- Implement **retry logic** for network reliability.
- Define **DTOs with proper serialization attributes** and **AOT** compatiblity.
- **DTOs shall inherit BaseEntity** and Endpoint should be provided via **EndpointAttribute** or **RecursiveEndpointAttribute**
- Use the **Swagger/OpenAPI** documentation (docs\openapi.yaml).

## Error Handling and Logging
- Use **exceptions only for exceptional cases**, not control flow.
- Implement **global error handling** with structured logging.
- Use **ILogger** for logging (Microsoft.Extensions.Logging).
- Return appropriate **HTTP status codes** and consistent error responses.

## Serialization and Deserialization
- Use **System.Text.Json** instead of Newtonsoft.Json with and **AOT** compatiblity.
- Configure **trimming-friendly serialization**.
- Define **custom converters** where necessary.
- Ensure **deserialization works with trimming enabled**.

## Performance Optimization
- Use **asynchronous programming** (`async/await`) for I/O-bound operations.
- Implement **caching** where necessary (e.g. `IMemoryCache`).
- Avoid **blocking calls** and ensure efficient memory usage.
- Optimize LINQ queries to prevent **N+1 problems**.

## Security Best Practices
- Use **HTTPS** for all network communication.
- Avoid storing **secrets in code**, use **Azure Key Vault** or **environment variables**.
- Enforce **CORS policies** properly.

## XML Documentation
- **All public and protected members** must have XML documentation.
- Provide **clear summaries** and **parameter descriptions**.
- Ensure **API documentation is auto-generated** using Swagger.
- Suppress **CS1591 warnings** for missing XML comments where explicitly needed (`<NoWarn>$(NoWarn);CS1591</NoWarn>`).

## Testing
- Write **unit tests using xUnit**.
- Use **Moq** for mocking dependencies.
- Use **Shouldly** for expressive assertions.
- Implement **integration tests for API endpoints**.
- Validate serialization and deserialization behavior with test cases.
- Use **coverlet.collector** for code coverage analysis.

## Continuous Integration & Deployment
- Use **GitHub Actions** for CI/CD workflows.
- Run **automated tests on every push/PR**.
- Ensure compatibility with **.NET 8 and .NET 9** in CI pipelines.

## Repository Structure
- Organize **source code, tests, and documentation** clearly.
- Use a consistent **naming convention for files and directories**.
- Include a well-structured **README** with setup instructions.

## Coding Patterns & Conventions
- **.NET 8/9, Nullable Reference Types, AOT/Trimming:** All code must be compatible with .NET 8/9, use `#nullable enable`, and support trimming/AOT for serialization.
- **DTOs:** Inherit from `BaseEntity`, use endpoint attributes, and System.Text.Json for serialization.
- **Dependency Injection:** Register API clients via `services.AddKepwareApiClient(...)`.
- **Logging:** Use `ILogger` (Microsoft.Extensions.Logging) for all logging.
- **Testing:** Use xUnit, Moq, Shouldly. Integration tests should validate real API endpoints and serialization.

## Integration Points
- **Kepware REST API:** All communication via HTTPS; certificate validation can be disabled for local/dev.
- **GitOps:** Sync service supports Git-based config management; use external tools for git operations.
- **Docker:** Service and SDK are container-ready; see README for compose examples.

## Examples
- **Create Channel/Device/Tag:** See `Kepware.Api.Sample/Program.cs` for idiomatic usage.
- **Sync Service CLI:**
  ```
  Kepware.SyncService SyncToDisk --primary-kep-api-username Administrator --primary-kep-api-password-file ./secrets/password.txt --primary-kep-api-host https://localhost:57512 --directory ./ExportedYaml
  ```
- **YAML Overwrite Example:** See `KepwareSync.Service/README.md` for structure and env var usage.

## Contribution
- Use Conventional Commits (`feat(api): ...`, `fix(sync): ...`).
- Link issues and PRs with permalinks and cross-references.

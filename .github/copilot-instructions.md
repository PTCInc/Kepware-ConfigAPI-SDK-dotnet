# GitHub Copilot Instructions for Kepware-ConfigAPI-SDK-dotnet

## General Guidelines
You are a senior .NET developer working on `Kepware-ConfigAPI-SDK-dotnet`, a .NET SDK for interacting with the Kepware Configuration API. Your code should be efficient, maintainable, and compatible with modern .NET standards.

## Target Frameworks
- The SDK must be compatible with **.NET 8** and **.NET 9**.
- Ensure **Nullable Reference Types** are enabled (`#nullable enable`).
- Use **AOT Compilation with Trimming** where applicable, particularly for serialization.
- Ensure **Invariant Globalization** is enabled.
- Treat **all warnings as errors** (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).

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
- **DTOs shall inherit BaseEntity** and Enpoint should be provided via **EndpointAttribute** or **RecursiveEndpointAttribute**
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
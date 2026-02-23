# Project: DandD Template Parser — C# API

## Overview
C# .NET 8 Web API for parsing D&D templates. Built with ASP.NET Core (controllers), Dapper, FluentValidation, and Serilog.

## Tech Stack
- **Runtime**: .NET 8, C# 12
- **Framework**: ASP.NET Core Web API (controllers)
- **ORM**: Dapper (raw SQL) + Microsoft.Data.SqlClient
- **Validation**: FluentValidation
- **Logging**: Serilog (structured, Console sink)
- **Testing**: xUnit + NSubstitute + FluentAssertions
- **Database**: SQL Server (MSSQL)

## Architecture
```
DandDTemplateParserCSharp/
├── Controllers/      # HTTP entry points — maps Result<T> to HTTP
├── Domain/           # Task, TaskId, TaskError, Result<T>
├── Services/         # Business logic — returns Result<T>, never throws
├── Repositories/     # Dapper SQL access — private row DTOs
├── Validators/       # FluentValidation validators
├── Middleware/       # ExceptionMiddleware (fallback only)
└── Options/          # Strongly-typed config (DatabaseOptions)
DandDTemplateParserCSharp.Tests/
├── Services/         # Unit tests (NSubstitute mocks)
└── Controllers/      # Integration tests (WebApplicationFactory)
```

## Rules (always loaded)
> See `.claude/rules/` — core-behaviors, code-standards, verification-and-reporting, leverage-patterns.

## Agents
| Purpose | Agent |
|---------|-------|
| C# code & patterns | `csharp-expert` |
| Code review | `code-reviewer` |
| Security audit | `security-reviewer` |
| DB schema | `database-designer` |
| Tech debt | `dedup-code-agent` |
| Architecture | `architect` |

## Common Commands
```bash
dotnet build --warnaserror         # Build (zero warnings required)
dotnet test                        # Run all tests
dotnet run --project DandDTemplateParserCSharp  # Start dev server
docker-compose up --build          # Start with SQL Server
```

## Key Conventions
- **Never throw** from services for domain errors — return `Result.Failure(...)`
- **Result → HTTP** mapping happens only in controllers via `switch` on `TaskError`
- **CancellationToken** on every async method signature
- **Primary constructors** for DI injection (C# 12)
- **`[Required]` + `ValidateOnStart()`** for all config options

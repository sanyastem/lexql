# Lexql

**A free, open-source desktop tool for MySQL** — a polished, productive IDE for
everyday MySQL development and administration: refined UX, lighter than DBeaver,
and fully open with no paid or copyleft dependencies.

> **Name:** *Lex* (lexical analysis — the heart of SQL autocomplete) + *QL*
> (query language).

Lexql targets the daily workflows of MySQL development and administration:
schema-aware SQL autocomplete, a data grid, query profiling (`EXPLAIN`), test
data generation, and schema compare/sync. The core is provider-based from day
one, so support for SQLite and MongoDB can follow without reshaping the
architecture.

> **Status: early development (Phase 0).** The connection and metadata core is
> being built first; the desktop UI comes later. Not yet usable as an
> end-user application. Follow the roadmap below.

## Why Lexql

- **Polished, productive UX, free.** Donation-supported, never paywalled.
- **Lighter than DBeaver** — startup time and memory are tracked requirements.
- **Multi-database by design.** A generic, capability-based provider core
  (`MySQL` → `SQLite` → `MongoDB`), not a SQL-only tool wearing a generic coat.
- **License-clean.** Apache-2.0, permissive dependencies only, implemented from
  public documentation (clean-room).

## Tech stack

| Area | Choice |
|------|--------|
| Runtime | .NET 10 / C# 14 |
| Database access | ADO.NET via **MySqlConnector** (MIT) — never Oracle `MySql.Data` (GPL) |
| SQL parsing | ANTLR4 + the MIT MySQL grammar |
| Test data | Bogus (core) |
| Desktop UI | MAUI Blazor Hybrid — Monaco (editor) + Tabulator (grid) |
| Tests | xUnit + NSubstitute |

All third-party components and their licenses are listed in
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

## Architecture

The core is intentionally **not SQL-shaped**. A thin generic spine declares what
each provider can do; relational features are optional services layered on top.

```
src/
  Core/                 generic, DB-agnostic spine (provider, capabilities,
                        result model, connection management)
  Core.Relational/      relational-only services (schema, SQL completion,
                        EXPLAIN, schema compare, data generation)
  Providers/
    MySql/              MySQL provider (MySqlConnector)
  App/                  application host (placeholder; MAUI shell lands later)
tests/
  Core.Tests/           xUnit + NSubstitute
```

A provider advertises `ProviderCapabilities`; the UI reads those flags and hides
features a given database does not support. Relational providers (MySQL, SQLite)
implement the `Core.Relational` services; a document provider (MongoDB) leaves
them unimplemented.

## Build and test

Requires the **.NET 10 SDK**.

```bash
dotnet restore Lexql.slnx
dotnet build Lexql.slnx -c Release
dotnet test  Lexql.slnx -c Release
```

CI (GitHub Actions) runs restore → build → test on every push and pull request.

## Roadmap

Development proceeds one database at a time so the abstraction is validated by
real implementations rather than guessed up front.

- **v0.1 — MySQL** (MySQL 5.7 & 8.0): connections, object tree, SQL editor with
  schema-aware autocomplete, read-only result grid, query history. Ships as a
  public, unsigned release.
- **v0.2 — SQLite** + editable grid: validates the relational abstraction.
- **v0.3+ — MongoDB**: stress-tests the abstraction with a document model.

Later phases add `EXPLAIN` visualization, the test-data generator, and schema
compare / sync.

## Contributing

Contributions are welcome under the [Developer Certificate of
Origin](https://developercertificate.org/) — every commit must be signed off:

```bash
git commit -s -m "Your message"
```

New dependencies must be permissive (MIT / BSD / Apache-2.0) and added to
`THIRD-PARTY-NOTICES.md` in the same change. See
[`CONTRIBUTING.md`](CONTRIBUTING.md) for the full policy.

## License

[Apache-2.0](LICENSE). Copyright © Lexql contributors.

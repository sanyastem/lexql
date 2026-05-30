# Third-Party Notices — Lexql

Lexql is distributed under the Apache-2.0 license.
It includes and depends on third-party software listed below. Each component
is the copyright of its respective authors and is used under the terms of its
license. This file satisfies the attribution requirements of those licenses.

---

## Runtime dependencies

### MySqlConnector
- License: MIT
- Use: MySQL connectivity (ADO.NET, no EF). Managed, clean-room MySQL protocol implementation.
- Copyright © Bradley Grainger and contributors.
- https://github.com/mysql-net/MySqlConnector

### ANTLR4 (runtime)
- License: BSD-3-Clause
- Use: parser runtime for the SQL lexer/parser (Antlr4.Runtime.Standard).
- Copyright © The ANTLR Project.
- https://github.com/antlr/antlr4

### Antlr4BuildTasks
- License: BSD-3-Clause
- Use: build-time only — generates the C# lexer from the `.g4` grammar during
  the build. Not redistributed in the application output.
- Copyright © Ken Domino and contributors.
- https://github.com/kaby76/Antlr4BuildTasks

### MySQL ANTLR grammar (Positive-Technologies)
- License: MIT
- Use: source `.g4` grammar from which the SQL parser is generated. A copy is kept in `/grammar`.
- Copyright © Ivan Kochurkin and contributors.
- https://github.com/antlr/grammars-v4/tree/master/sql/mysql
- NOTE: a copy of the grammar's MIT license text is stored alongside the `.g4` file in `/grammar`.

### SSH.NET
- License: MIT
- Use: optional SSH tunnel (local port-forward) for connecting to a MySQL
  server behind a bastion host.
- Copyright © Renci and contributors.
- https://github.com/sshnet/SSH.NET

### System.Security.Cryptography.ProtectedData
- License: MIT
- Use: Windows DPAPI wrapping of the local encryption key for stored secrets.
- Copyright © .NET Foundation and contributors.
- https://github.com/dotnet/runtime

### Bogus
- License: dual MIT / Apache-2.0 (core only; Bogus Premium is NOT used)
- Use: fake/test data generation.
- Copyright © Brian Chavez and contributors.
- https://github.com/bchavez/Bogus

### Monaco Editor
- License: MIT
- Use: SQL code editor (syntax highlighting, completion UI) embedded via BlazorWebView.
- Copyright © Microsoft Corporation.
- https://github.com/microsoft/monaco-editor

### Tabulator
- License: MIT
- Use: editable results grid, embedded via BlazorWebView.
- Copyright © Oliver Folkerd.
- https://github.com/olifolkerd/tabulator

### .NET MAUI
- License: MIT
- Use: cross-platform desktop app host (MAUI Blazor Hybrid).
- Copyright © .NET Foundation and contributors.
- https://github.com/dotnet/maui

### Microsoft.Data.Sqlite
- License: MIT
- Use: local store for query history and saved queries (NOT a target DB driver).
- Copyright © .NET Foundation and contributors.
- https://github.com/dotnet/efcore

### Bootstrap
- License: MIT
- Use: base CSS for the Blazor UI (bundled in `src/App/wwwroot/lib/bootstrap`).
- Copyright © The Bootstrap Authors.
- https://github.com/twbs/bootstrap

### Open Sans
- License: Apache-2.0
- Use: bundled UI font (`src/App/Resources/Fonts/OpenSans-Regular.ttf`).
- Copyright © Steve Matteson / Google.
- https://fonts.google.com/specimen/Open+Sans

---

## Test dependencies

### xUnit
- License: Apache-2.0
- https://github.com/xunit/xunit

### NSubstitute
- License: BSD-3-Clause
- https://github.com/nsubstitute/NSubstitute

### Testcontainers (Testcontainers.MySql)
- License: MIT
- Use: integration tests — spins up disposable MySQL 5.7 / 8.0 Docker containers.
- Copyright © Andre Hofmeister and contributors.
- https://github.com/testcontainers/testcontainers-dotnet

---

## Notes

- All components above are permissive (MIT / BSD / Apache-2.0) and mutually
  compatible under the project license.
- No GPL or other copyleft dependency is included. In particular, Oracle's
  `MySql.Data` (GPL) is deliberately NOT used.
- When adding a new dependency: confirm MIT/BSD/Apache, then add an entry here
  in the same change. See `CONTRIBUTING.md`.
- UI stack is decided: MAUI Blazor Hybrid (Monaco + Tabulator). Avalonia was not chosen.
- Future DB providers (SQLite, MongoDB) will add their own driver entries here when implemented.

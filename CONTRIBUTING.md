# Contributing to Lexql

Thanks for your interest in Lexql — a free, open-source desktop tool for MySQL.

## License

Lexql is licensed under the **Apache License 2.0**. By contributing, you agree
that your contributions are licensed under the same terms.

## Developer Certificate of Origin (DCO)

All contributions must be signed off under the
[Developer Certificate of Origin](https://developercertificate.org/). The DCO is
a lightweight statement that you wrote the contribution or otherwise have the
right to submit it under the project's license.

Add a `Signed-off-by` line to every commit:

```
Signed-off-by: Your Name <your.email@example.com>
```

Git can do this automatically with the `-s` flag:

```
git commit -s -m "Your message"
```

The name and email must match your real identity (no anonymous or pseudonymous
contributions). Pull requests whose commits are not signed off cannot be merged.

## Development workflow

- **One task = one atomic commit / PR.** Work is tracked task by task.
- Target framework: **.NET 10 / C# 14**. Database access is **ADO.NET via
  MySqlConnector** — never Oracle's `MySql.Data`.
- Every public `Core` / `Core.Relational` method is covered by tests
  (**xUnit + NSubstitute**). Dependencies sit behind interfaces.
- Run the build and the full test suite before opening a PR:

```
dotnet build
dotnet test
```

## Dependencies

Only **permissive licenses** (MIT / BSD / Apache-2.0) are allowed — no paid or
copyleft dependencies. Any new dependency must be added to
`THIRD-PARTY-NOTICES.md` in the same change.

## Clean-room policy

Lexql is implemented from public documentation only (MySQL/SQLite/MongoDB
references and standards). Do not decompile or copy code, grammar, UI, or naming
from commercial tools.

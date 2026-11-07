# Inline quiz checks

Run the server, content-generation and Razor checks:

```text
dotnet run --project tools/InlineQuizChecks/InlineQuizChecks.csproj
```

Include Chrome interaction checks and phone/desktop screenshots:

```text
dotnet run --project tools/InlineQuizChecks/InlineQuizChecks.csproj -- --browser
```

The browser option uses locally installed Chrome through its debugging protocol.
It starts an isolated local fixture server and a separate headless browser profile.
Database checks use SQLite in memory; the application's database is never opened.

Coverage includes all fifty state pools, image/text mixing, independent symbol
topics, metadata-derived facts, content and neighboring-state links, mobile tap
targets, 1–4 keyboard answers, answer locking, manual progression, complete answer
review, sharing, replay, stored best score, collection progress and analytics.
The controller checks reject invented choices, duplicate questions and incomplete
rounds, verify answers against the server pool, and suppress community percentages
until twenty comparable rounds or answers exist.

Previews are written to `artifacts/inline-quiz-checks/`.

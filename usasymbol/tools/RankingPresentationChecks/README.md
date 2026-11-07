# Ranking presentation checks

Run after changing ranking templates, rank calculation or chart scaling:

```text
dotnet run --project tools/RankingPresentationChecks/RankingPresentationChecks.csproj
```

Uses the real area-codes and cancer-rate pages, plus synthetic boundary cases.
Checks complete ties on both sides of the top-ten boundary, alphabetical tie
ordering, shared competition ranks in the table/cards/state picker, the common
zero scale for counts, diverging rate bars, ascending rankings, all-equal values,
zeros and negative values. It renders the real Razor partials and writes standalone
HTML previews to `artifacts/ranking-presentation-checks/`.

Inspect both previews at phone widths and at a desktop viewport with a content
column around 670–720 pixels. Long state names must stay next to their flags;
values must remain in the last column. The area-codes page must include all
eleven states with one code and all four states tied at rank nine.

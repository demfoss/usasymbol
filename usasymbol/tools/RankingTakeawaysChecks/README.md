# Ranking takeaway checks

Run from the repository root:

```sh
dotnet run --project tools/RankingTakeawaysChecks/RankingTakeawaysChecks.csproj
```

The checks exercise ties, ascending rankings, negative and equal values, fractional medians,
missing values, DC exclusion, repeated-state item tables, currency and percentage formats,
population normalization, and previous-year comparisons. They also load every ranking through
the application's content service and check rank, scale and aggregation invariants without
starting the website or changing content.

## Optional metric metadata

Existing rankings work without a migration. The shared block uses the default numeric metric,
`sort_default` or the source ranks, formatted values, and a computed median and quartiles.
Tables without a usable numeric state metric keep their existing quick-answer presentation.

For natural units and additional computed insights, add metadata to the primary `table`:

```yaml
table:
  metric_default: area_code_count
  sort_default: desc
  metric:
    unit_singular: "area code"
    unit_plural: "area codes"
    additive: true
```

`additive` defaults to false. Enable it only for quantities whose state totals can meaningfully
be added. Rates, percentages, salaries, scores and ranks must not be added or normalized again.
This flag enables the combined share of the three largest state totals.

Optional `population_key` names a row field containing the population denominator. A per-person
insight requires additive data, positive population values for every included state, and a unique
leader. Use population estimates aligned with the metric's year and geographic coverage.

Optional `previous_value_key` names a row field with the same metric in an earlier year.
Set `previous_year` as well. A change insight requires valid previous values for every included
state and a unique largest absolute change. Missing inputs suppress the fact.

The number of facts varies with available data. Newest-event facts require dated event data;
the block never infers activation dates from an area-code number or narrative text.

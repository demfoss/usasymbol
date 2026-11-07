# Map legend checks

Run from the checkout root:

```powershell
dotnet run --project tools/MapLegendChecks/MapLegendChecks.csproj
npm.cmd run build:css
node tools/MapLegendChecks/browser-check.mjs
```

The .NET checks audit every ranking map through the production parser and builder,
exercise missing data, zero, ties, all-equal values, exact boundaries, percentages,
currency, large counts, years, categories, booleans, rank bands and reference scales,
and render six standalone Razor snapshots in `artifacts/map-legend-checks`.

The browser check uses installed Chrome and Node's built-in CDP pipe transport.
Set `MAP_CHECK_BROWSER` to another Chromium executable if needed. It opens an
isolated browser profile inside the artifact directory, checks desktop and mobile
layout, keyboard controls, filter/reset membership and missing-data hatching, saves
screenshots and closes its browser. No application server or database is needed.

## Optional map metadata

Existing maps infer percentage/currency formatting from their mapped table column,
group equal values together and compute up to five readable thresholds. Existing
`color_scale: log` remains accepted, but group membership and fills now use the
same discrete thresholds; log interpolation no longer produces extra shades within
a filter group. Text maps use named categories, or a single colour for too many
distinct names to form a useful legend. No data is a separate group when present.

Overrides belong in the existing `map` block; they do not change the table:

```yaml
map:
  metric_key: asthma_rate
  metric_label: Adults with current asthma
  value_type: percent
  decimals: 1
  # Optional explicit thresholds. Upper endpoints belong to the next group.
  breaks: [9, 10, 11, 11.5]
```

Supported `value_type` overrides: `percent`, `rate`, `currency`, `count`,
`large_count`, `year`, `category`, `ordinal`, `boolean`, `rank_only` (ordinary numbers are
inferred). `unit_label` adds a unit to the heading, such as `per 100,000` or `sq mi`.
Percentage values are already percentage points unless the table column uses a
.NET `P` format. Currency values are USD. Explicit breaks accept up to four finite,
distinct thresholds; thresholds outside the observed domain and empty groups are
removed. Counts and years use integral ranges. Missing states are included even
when the source table omits their rows.

`color_map` provides category colours and display order. `category_order` optionally
sets an explicit sequence of names; `value_type: ordinal` uses a sequential palette.
Empty configured categories
are omitted. `No` and `0` remain valid values, separate from missing data.

Reference comparison is opt-in. A reference value alone does not switch palettes:

```yaml
map:
  legend_mode: reference
  reference_value: 9.8
  reference_label: U.S. average
  reference_tolerance: 0.5
```

This example uses bounds at 8.3, 9.3, 10.3 and 11.3, with a neutral middle and
Below/Above labels. Values use the same units as the source data. If tolerance is
omitted, it defaults to one twentieth of the observed range. The heading identifies
the reference; it must come from a comparable source and year. Existing
`color_scheme: diverging` maps retain a signed comparison around zero, with a
neutral colour distinct from missing data. Neither mode assigns good/bad meaning.

# County data layer

The approved build-time importer writes one canonical YAML record per county:

`Content/places/counties/{state-slug}/{county-slug}.yaml`

FIPS is the only join key. The files combine:

- 2020–2024 Census ACS 5-year population, household income, home value, gross
  rent, and bachelor’s-degree attainment;
- 2025 BLS LAUS annual-average labor force, employment, and unemployment;
- 2025 County Health Rankings life expectancy, self-reported health, insurance,
  primary-care access, and mental-health-provider access.

`county-metrics.json` is a compact runtime index generated from the same joined
records. Missing values remain absent. State metrics are never copied into a
county. Standalone publication remains gated at population 100,000 or the
largest county in a state.

The importer lives in `tools/CountyDataImporter`.

Runtime surfaces powered by this layer:

- `/county-match` — weighted national or within-state county matching;
- `/county-rankings` — affordability, income, jobs, education, and health;
- `/states/{state}/counties` — sortable directory and metric choropleth;
- `/states/{state}/counties/{county}` — gated relocation profile;
- state living pages — top county highlights.

`.github/workflows/update-county-data.yml` refreshes the official files every
Monday. A source fingerprint keeps unchanged runs idempotent, so generated
content is committed only when an upstream dataset changes.

# Release Lead Time Calculation Prompt

## Task
Calculate the release lead time for the GitHub release provided by the user.

## Required Input
Ask the user to provide the GitHub Release URL.

## Important Rules

- Do not use any example release, sample release, or placeholder release URL.
- Use the GitHub Release **Published Date** as the **Release Date**.
- Identify all PRs explicitly referenced in the release notes.
- Do not infer, estimate, or guess release items.
- Only include PRs that are explicitly referenced in the release notes.
- For each PR:
  - Open the PR page.
  - Identify the PR link.
  - Open the **Commits** tab.
  - Use only commits that belong to that PR.
  - Determine the earliest relevant commit date from the PR's actual branch lineage.
  - Do not use the PR creation date.
  - Do not use the PR merge date.
  - Do not use the first visible commit in the PR if earlier commits exist in the branch lineage.
  - If necessary, trace branch ancestry to locate the earliest unique commit in that PR branch before merge.
  - Use that earliest commit date as **Oldest Commit Date**.

## Lead Time Calculation

For each PR:

```
Lead Time (Days) = Release Date - Oldest Commit Date
```

Rules:

- Use exact calendar-day differences.
- Ignore time-of-day.
- Do not round.
- Do not use business-day calculations.
- Recalculate independently before finalizing.

## Release Lead Time Formula

The final release lead time is:

```
Average Lead Time =
(Sum of all PR Lead Times) / (Total Number of Release PRs)
```

## Validation Requirements

Before producing the final output:

1. Verify the release published date.
2. Verify every PR is explicitly listed in the release notes.
3. Verify the PR link for every release item.
4. Verify the oldest commit date is the true earliest commit in that PR branch lineage.
5. Verify calendar-day calculations for every row.
6. Verify arithmetic used in the final average.
7. Check for logical consistency across all rows.
8. Recalculate all lead times independently.
9. Only present the final result after all validations pass.

## Date Format

All dates must use:

```
dd-MMM-yyyy
```

Example formats:

```
05-Oct-2026
07-Jul-2026
```

## Required Output Rules

- Output only a single Markdown table.
- No narrative.
- No bullets.
- No explanations.
- No text before the table.
- No text after the table.
- Include one row per release-item PR.
- Use sequential row numbers in the **Commit#** column.
- Include the PR link in the **Release Commit** column.
- Add a final summary row labeled **Release lead time**.
- The final summary row must include the GitHub Release URL provided by the user.
- Columns must appear exactly in this order:

| Commit# | Release Commit | Oldest Commit Date | Release Date | Lead Time (Days) |

## Required Output Template

| Commit# | Release Commit | Oldest Commit Date | Release Date | Lead Time (Days) |
|----------|----------|----------|----------|----------|
| 1 | PR Link | dd-MMM-yyyy | dd-MMM-yyyy | N |
| 2 | PR Link | dd-MMM-yyyy | dd-MMM-yyyy | N |
| ... | ... | ... | ... | ... |
| Release lead time | Release URL |  |  | Average Lead Time |
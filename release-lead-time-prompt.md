# Task

Calculate the release lead time for the GitHub release provided by the user, using one release-wide oldest commit date across all PRs referenced in the release notes.

# Important Rules

- Ask the user to provide the GitHub release link.
- Do not use any example release or placeholder release URL.
- Use the release published date as the Release Date.
- Identify only the PRs explicitly referenced in the release notes.
- For each referenced PR, navigate to the PR link and open the Commits tab.
- Use the commits listed directly on the PR commit page as the candidate commit set for that PR.
- For each PR, determine the earliest relevant commit date in the PR’s actual branch lineage, not the first visible commit in the PR page, and not the PR creation or merge date.
- If the PR page does not show the earliest ancestor commit because the branch history is longer or has merged ancestry, trace the PR branch lineage back to the earliest unique commit in that PR branch.
- Use the Git committer timestamp (`committer.date` in GitHub’s commit data), not the author timestamp (`author.date`).
- Determine the earliest commit date for every referenced PR, then select the single earliest of those dates across all PRs. This is the release-wide Oldest Commit Date. Do not report separate Oldest Commit Dates or lead times for individual PRs.
- Compute the release lead time as: Release Date - release-wide Oldest Commit Date, using exact calendar-day difference, not rounded, and not counting time-of-day.
- Do not average per-PR lead times.
- Include links to all referenced PRs in the Release Commit cell of the single release-level row.
- Add the release URL provided by the user in the final row.
- Use dd-MMM-yyyy format for dates.
- Do not infer or guess release items. Only include PRs explicitly referenced in the release notes.
- Do not use example PRs, example dates, or any values from a sample.
- Do not output any narrative, bullets, extra commentary, or explanation outside the final Markdown table. No extra text before or after the table.

# Validation Requirements

Validate the result independently before finalizing:

- Confirm the exact calendar-day difference between the release date and the release-wide Oldest Commit Date.
- Confirm each PR’s candidate commit lineage and committer dates, and verify that the selected date is the earliest across all referenced PRs.
- Do not use author dates, PR creation dates, or PR merge dates.
- Check for arithmetic/logical errors and consistency.

# Date Calculation Rules

- Use exact calendar-day math.
- Do not round to the nearest day or use business-day logic.
- Use the final result only after independently recalculating and validating the dates, lineage, and formula.

# Required Output Format

A clean UI-style Markdown table with only these columns:

| Commit# | Release Commit | Oldest Commit Date | Release Date | Lead Time (Days) |

- Output one release-level data row, with `1` as Commit#.
- In that row, link every PR explicitly referenced in the release notes in the Release Commit cell.
- Include a final row labeled “Release lead time”; include the release URL in that row.
- The final row’s lead time must equal the single release-level lead time in the data row.

# Important Date Rule

- The release-wide Oldest Commit Date is the earliest committer date among the earliest relevant commits in the actual branch lineages of all PRs referenced in the release notes.
- A deeper ancestor commit in a PR branch lineage must be considered even if it is not on the first page of the PR commits list.
- For every commit considered, use the Git committer timestamp (`committer.date`), not the author timestamp.
- Do not use the PR merge date or PR creation date.

# Final Instruction

Before presenting the final result, re-check all referenced PRs, branch ancestry, committer dates, the release-wide minimum date, and the calendar-day calculation. Only then output the final table.

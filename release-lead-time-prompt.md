# Task

Calculate the release lead time for the GitHub release provided by the user.

# Important Rules

- Ask the user to provide the GitHub release link.
- Do not use any example release or placeholder release URL.
- Use the release published date as the Release Date.
- For each PR referenced in the release notes, identify the PR and its commit list.
- Navigate to the PR link and open the Commits tab.
- Use only the commits listed directly on the PR commit page as the candidate commit set for that PR.
- For each PR, determine the earliest relevant commit date in the PR’s actual branch lineage, not the first visible commit in the PR page, and not the PR creation/merge date.
- If the PR page does not show the earliest ancestor commit because the branch history is longer or has merged ancestry, trace the PR branch lineage back to the earliest unique commit in that PR branch before the PR was merged.
- Use the earliest commit date found in that branch lineage as the Oldest Commit Date for that PR.
- Compute calendar day lead time as: Release Date - Oldest Commit Date using exact calendar day difference, not rounded, and not counting time-of-day.
- The final release lead time must be the average of all PR release-item lead times: (sum of all PR lead times) / (total number of PR release items).
- Report one row per release item/PR with PR link.
- Use row number as Commit#.
- Use dd-MMM-yyyy format for dates.
- The final table must include a final row labeled “Release lead time”.
- Add the release URL provided by the user in the final row.
- The final table columns must be exactly:
  - Commit#
  - Release Commit
  - Oldest Commit Date
  - Release Date
  - Lead Time (Days)
- Do not output any narrative, bullets, extra commentary, or explanation outside the final Markdown table.
- No extra text before or after the table.
- Do not infer or guess release items. Only include PRs explicitly referenced in the release notes.
- Do not use example PRs, example dates, or any values from a sample.

# Validation Requirements

Validate calculations independently before finalizing:

- Confirm the exact calendar-day difference for each PR.
- Confirm the oldest commit date is truly the earliest commit in that PR branch lineage.
- Verify the average lead time formula is correct.
- Check for arithmetic/logical errors and consistency.

# Date Calculation Rules

- Use exact calendar-day math.
- Example: 2026-10-05 minus 2026-07-07 = 90 days if the dates are 90 days apart.
- Do not round to nearest day or use business-day logic.
- The release lead time is the average of all release item lead times.
- Use the final result only after independently re-calculating and validating formulas and assumptions.
- Ensure the final result is internally consistent and accurate.

# Required Output Format

A clean UI-style Markdown table with only these columns:

| Commit# | Release Commit | Oldest Commit Date | Release Date | Lead Time (Days) |

# Important Date Rule

- Oldest Commit Date must be the earliest commit date in the PR branch lineage, even if that commit is not on the first page of the PR commits list.
- Do not use the PR merge date or PR created date.
- Do not use the first commit visible on the PR page if a deeper ancestor exists in the branch lineage.
- For all PRs, the date must reflect the actual earliest unique commit in that PR branch lineage.

# Final Instruction

Before presenting the final result, re-check all dates, branch ancestry, calculations, and arithmetic. Only then output the final table.
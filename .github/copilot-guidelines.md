# GitHub Copilot Agent – Code Update Best Practices

This document outlines expectations and best practices for GitHub Copilot Agent when proposing or applying code changes. It ensures consistency, security, and maintainability across our codebase.

---

## General Principles

- **Preserve intent**: Respect the original logic and design unless explicitly asked to refactor.
- **Minimize disruption**: Avoid broad changes unless necessary. Prefer targeted, minimal diffs.
- **Explain reasoning**: Include concise comments or commit messages explaining why a change was made.
- **Avoid assumptions**: Do not infer behavior without clear context or documentation.

---

## Code Structure and Practices

- **Terminology** use this file to understand the terminology of the system .github/contensive-terminology.md
- **Structure** use this file to understand the structure of the system .github/contensive-structure.md

---

## Code Hygiene

- **Follow existing formatting**: Match indentation, casing, and naming conventions.
- **Avoid introducing unused code**: No dead code, commented-out blocks, or speculative additions.
- **Respect file structure**: Place new code in appropriate modules or folders based on existing patterns.

---

## Security & Compliance

- **Sanitize inputs**: Validate and sanitize any user or external input.
- **Avoid hardcoded secrets**: Never introduce credentials, tokens, or keys in code.
- **Respect access boundaries**: Do not modify IAM roles, permissions, or access logic without explicit instruction.

---

## Automation & Reliability

- **Prefer idempotent logic**: Ensure repeated executions yield consistent results.
- **Use existing utilities**: Leverage helper functions or shared modules where available.
- **Avoid manual steps**: Prefer automated workflows over instructions that require human intervention.

---

## Testing & Validation

- **Update tests**: Modify or add unit/integration tests to reflect code changes.
- **Don’t break existing tests**: Ensure all tests pass before proposing changes.
- **Use mocks/stubs**: Avoid relying on external systems in tests unless explicitly required.

---

## 📄 Metadata & Documentation

- **Preserve metadata**: Do not strip or overwrite file-level comments, annotations, or tags.
- **Document edge cases**: If a change addresses a rare or complex scenario, explain it clearly.
- **Respect versioning**: Avoid bumping versions or changelogs unless instructed.

---

## Decision Flow

If unsure about a change:
1. Propose it as a comment or suggestion.
2. Reference this document and explain the tradeoffs.
3. Wait for human review before applying.

---

## Notes

- This file is located at `.github/copilot-guidelines.md` and is referenced by automation tools and PR templates.
- Updates to this document should be reviewed and approved via pull request.

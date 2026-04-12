# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**Read [README.md](README.md) first** — it contains the project overview, architecture, build commands, and development workflow.

## Critical Rule: Collection XML First

**When adding a new database table or database field, you MUST add it to the addon collection XML file BEFORE adding it to code (models, controllers, addons, etc.).**

The collection XML file is how Contensive creates and updates database schema when a collection is installed. If a table or field exists in code but not in the collection XML, it will not exist in the database at runtime.

**Required workflow:**
1. Add the `<CDef>` (for new tables) or `<Field>` element (for new fields) to the collection XML file
2. Then add or update the corresponding C# model class and any code that uses the field
3. Never add a database field to a model class without a corresponding `<Field>` in the collection XML

See [Addon Collection Pattern](patterns/addon-collection-pattern.md) for CDef and Field XML syntax.
See [Database Models Pattern](patterns/database-models-pattern.md) for C# model conventions.

## Code Style

- Always prefer string interpolation over string concatenation

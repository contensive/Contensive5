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

## Testing

- [Contensive Testing Pattern](patterns/testing-pattern.md)
- E2E tests: `tests/e2e/` (Playwright, TypeScript)
- Integration tests: `source/ProcessorTests/` and `source/ModelsTests/` (MSTest, C#)

## Database Table Metadata

Models in `source/Models/Models/Db/` document table schemas and relationships. Each model includes XML summary comments describing foreign key relationships, referenced-by relationships, and join patterns. When querying metadata tables, refer to these models for correct column names and relationships.

Core metadata models and their tables:
- `ContentModel.cs` -> `cccontent` - content definitions
- `TableModel.cs` -> `cctables` - database table registry (the `name` field holds the SQL table name)
- `ContentFieldModel.cs` -> `ccfields` - field definitions with type enum and lookup relationships
- `ContentFieldTypeModel.cs` -> `ccfieldtypes` - field type ID-to-name registry
- `DatasourceModel.cs` -> `ccdatasources` - database connection definitions

Key join pattern to resolve a content definition's database table name:
```sql
inner join cctables t on t.id = c.contenttableid
-- then use t.name for the SQL table name
```

## Code Style

- Always prefer string interpolation over string concatenation

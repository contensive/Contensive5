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

## Critical Rule: CPBase Binary Compatibility

**Never make a breaking change to the `source/CPBase` project.** CPBase is the public API surface consumed by separately compiled addon assemblies (Blog, CRM, etc.). Those addons are not recompiled when CPBase is updated, so any signature change that removes or alters an existing method will cause `MissingMethodException` at runtime.

**Rules:**
1. **Never delete a public method, property, or class** — if it's no longer needed, mark it `[Obsolete]`
2. **Never change the parameters of an existing public method** — instead, add a new overload with the additional parameters
3. **Never add optional parameters to existing methods** — in .NET, optional parameters are baked into the caller at compile time. Adding `string foo = ""` to an existing method is a binary-breaking change even though it's source-compatible. Always add a separate overload instead.
4. **Obsolete attributes must be warnings, not errors** — use `[Obsolete("message", false)]`, never `[Obsolete("message", true)]`
5. **Obsolete messages must include migration instructions** — tell the consumer what replacement method to use (e.g., `[Obsolete("Use addFilterSelect(caption, name, options, defaultValue) instead.", false)]`)

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

## GUIDs

When you need a GUID value (for CDefs, Fields, Addons, or any XML/code element), you MUST generate it using a system command. NEVER invent or type GUID values manually — Claude's token prediction produces sequential/predictable values that cause collisions.

**Required:** Run this command to generate each GUID:
```bash
powershell -Command "[guid]::NewGuid().ToString('B').ToUpper()"
```

This returns a proper v4 UUID like `{7F2A9C4E-B831-4D6F-A5E2-9C1B3D8F6A42}`. Generate a separate GUID for every element that needs one — never reuse or increment.

## Code Style

- Always prefer string interpolation over string concatenation

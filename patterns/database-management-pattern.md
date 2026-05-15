
# Database Management Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview
The Contensive architecture provides a database api

## Naming Conventions

- Tables are named first with a 2-letter prefix unique to the collection codebase that installed them. The Contensive5 main codebase uses the prefix `cc` (e.g., `ccMembers`, `ccGroups`, `ccContent`). Each addon collection should choose its own unique 2-letter prefix to avoid table name collisions (e.g., an ecommerce collection might use `ec`, a blog collection might use `bl`).
- Table names are camelCase with no dashes and no underscores
- Field names are camelCase with no dashes and no underscores
- Foreign keys are named with the table they reference followed by the field in that table where they connect

## Basic required Fields

These fields are automatically created when a table is created in a Contensive Definition

- id
- name
- dateAdded
- createdBy
- modifiedBy
- modifiedDate
- sortOrder
- active
- ccGuid

## Content Definitions

Contensive defines database tables and fields by creating Contensive Definitions in the xml collection files used to install add-on collections.

**IMPORTANT: Collection XML first.** When adding a new database table or field, always add the `<CDef>` or `<Field>` element to the addon collection XML file *before* adding any corresponding C# code (model properties, controller logic, etc.). The collection XML is the source of truth for schema — it is what creates and updates database tables and columns when the collection is installed. A field referenced in code but missing from the collection XML will not exist in the database at runtime.

See [Addon Collection Pattern](addon-collection-pattern.md) for full CDef and Field XML syntax.

Database indexes are automatically added for the required fields. Additional indexes required are added with SqlIndex nodes in the xml addon collection.

## CP.Db methods

Interface to the database is through the cp.db methods. Queries should be written in the Db Model at the center of the query.

All query construction must wrap input values to prevent sql injection
- cp.db.EncodeSqlText() to manage strings
- cp.db.EncodeSqlTextLike() for like queries
- cp.db.EncodeSqlInteger() for integers
- cp.db.EncodeSqlNumber() for float
- cp.db.EncodeSqlBoolean() for boolean
- cp.db.EncodeSqlDate() for datetime

## Code Models

The ORM pattern created by Contensive.Models should be used for convenient coding.

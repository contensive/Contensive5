
# People Tracking Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

---

## Overview

Contensive tracks website visitors through People records in the `ccmembers` table. Every visitor interaction creates or reuses a People record, but not all People records are permanent. This pattern describes how People records are created, categorized, and managed through their lifecycle.

For how People records relate to Visits, Visitors, and Pageviews, see the [Session Tracking Model](authentication-pattern.md#session-tracking-model) in the Authentication Pattern.

---

## People Record Creation

Every People record has a boolean flag: **`createdByVisit`**

- `true` — the record was auto-created by the tracking system when an unrecognized visitor hit the site (no admin involved).
- `false` — the record was created any other way (e.g., manually by an admin, via a form submission tied to an existing contact, import, etc.)

`createdByVisit = true` records are treated as **temporary** by default and are candidates for purging after a short retention period, unless something promotes them (e.g., they submit a form, make a purchase, or an admin manually converts them).

---

## Person Type (`personTypeId`)

Every People record has an integer field **`personTypeId`** that explicitly classifies the record. This is the authoritative system-level classification — it replaces the previous approach of inferring type from scattered signals across multiple tables.

| Value | Enum | Description | Retention |
|---|---|---|---|
| 0 | `Unknown` | Legacy records not yet backfilled. | Backfilled by housekeeping. |
| 1 | `Bot` | Non-human visitor detected via browser signature, IP, and/or behavior. | Purged daily. |
| 2 | `Guest` | Anonymous human visitor. Default for new, unrecognized visitors. | Purged after retention period. |
| 3 | `Contact` | A person the business wants to retain permanently. | Permanent. |

The enum is defined in `PersonTypeEnum` (in `Contensive.Models.Db`).

### How `personTypeId` is Set

- **SessionController.createGuest()** — sets `personTypeId = Guest` (2) for every new visitor.
- **SessionController (bot detection)** — if bot detection identifies the visit as non-human, upgrades the record to `personTypeId = Bot` (1).
- **AuthController.recognizeById()** — when a user is recognized or authenticated, promotes to `personTypeId = Contact` (3). All authentication paths flow through this method.
- **Housekeeping backfill** — legacy records with `personTypeId = 0` (Unknown) are backfilled daily using the `createdByVisit` flag and visitor/visit bot flags.

### Relationship to `createdByVisit`

The `createdByVisit` flag and `personTypeId` serve complementary purposes:

- `createdByVisit` answers "who created this record?" (system vs. human)
- `personTypeId` answers "what kind of record is this?" (bot, guest, or contact)

Both are set on creation, and `personTypeId` is promoted on authentication. The `createdByVisit` flag is still cleared on recognition (for backward compatibility), but `personTypeId` is the primary field for querying and housekeeping.

---

## Contact Relationships (Exclusive-Set Groups)

Once a People record reaches `personTypeId = Contact`, it can be assigned a **Contact Relationship** via exclusive-set groups. These six groups all have `exclusiveSet = "Contact Relationship"`, so a contact can belong to only one at a time. In the admin People form, they render as radio buttons.

| Group Name | Caption | Description |
|---|---|---|
| `Contact Relationship Lead` | Lead | A population of contacts whose relationship is unknown. |
| `Contact Relationship Unqualified` | Unqualified | Contacts that have expressed interest but have not yet been qualified. |
| `Contact Relationship Not-Qualified` | Not-Qualified | Contacts that have shown interest but we do not think they qualify. |
| `Contact Relationship Qualified Prospect` | Qualified Prospect | Contacts that have shown interest and we agree they are potential members. |
| `Contact Relationship Member` | Member | Contacts who have joined. |
| `Contact Relationship Other` | Other Contact | Contacts not in the member funnel. |

Admins move contacts through these relationship types manually, and/or the system may auto-advance them based on defined triggers.

The "Member" label is the underlying platform term. Addons may present vertical-specific labels to admins — for example, "Customer" for generic installs, "Member" for associations, or "Patient" for medical/dental practices. The data model is identical; only the display label changes.

### Extending Contact Relationships

The six groups above cover the standard CRM funnel. To add custom relationship types (e.g., "Vendor", "Partner", "Volunteer"), admins simply create a new group with `exclusiveSet = "Contact Relationship"`. It will automatically appear as a radio button option alongside the built-in types — no code changes or additional tables required.

---

## Roles

Roles control what an authenticated user is allowed to do. There are two built-in roles stored directly on the People record, plus custom group-based roles.

### Built-in Roles

The People record (`ccmembers`) has two boolean fields that grant built-in roles:

| Field | Role Granted | Condition |
|---|---|---|
| `admin` | **Admin** | User is authenticated AND `admin = true` OR `developer = true` |
| `developer` | **Developer** | User is authenticated AND `developer = true` |

- A user with the **Developer** role automatically has the **Admin** role as well — the developer flag implies admin.
- These roles only apply when the user is **authenticated**. An unauthenticated user has no roles regardless of what the People record says.

**API checks:**

- `cp.User.IsAdmin` — returns `true` if the user is authenticated and has the admin role (either `admin` or `developer` is checked). This implicitly guarantees `IsAuthenticated`.
- `cp.User.IsDeveloper` — returns `true` if the user is authenticated and has the developer role.

### Group-Based Roles

Additional roles are defined by creating Groups (`ccgroups` table, `GroupModel`). A user has a group-based role if:

1. The user is **authenticated**, AND
2. The user is a **member of the group**

Group membership is stored in the `ccmemberrules` table (`MemberRuleModel`), which joins a People record to a Group:

| Field | Description |
|---|---|
| `memberId` | Foreign key to the People record (`ccmembers.id`) |
| `groupId` | Foreign key to the Group (`ccgroups.id`) |
| `dateExpires` | Optional expiration date — if set and past, the membership is no longer active |
| `groupRoleId` | Optional foreign key to a Group Role (`ccgrouproles.id`) for sub-role differentiation within the group |

**API checks:**

- `cp.User.IsInGroup("groupName")` — returns `true` if the authenticated user is an active member of the named group.

### Role Hierarchy

```
Developer (developer = true)
   └── implies Admin
Admin (admin = true OR developer = true)
   └── platform administration access
Group Roles (ccmemberrules membership)
   └── application-defined permissions per group
Authenticated (no special flags)
   └── basic authenticated access
```

All roles require authentication. An unauthenticated visitor has no roles.

---

## Record Lifecycle Summary

```
Page Hit
   |
   v
Visitor (persistent cookie) read/established
   |
   v
Visit (session) determined from Visitor
   |-- stores last People ID used
   |-- stores authentication status (authenticated/recognized/not recognized)
   |
   v
People record created (personTypeId = Guest)
   |
   +-- Bot detection? --> personTypeId = Bot (purged daily)
   |
   +-- No auth --> personTypeId = Guest (purged after retention period)
   |
   +-- Authenticated/Recognized --> personTypeId = Contact (permanent)
                                        |
                                        v
                          Contact Relationship (exclusive-set groups)
                                        |
               +------------------------+------------------------+
               v                        v                        v
             Lead --> Unqualified --> Qualified Prospect --> Member
                          |
                          v
                    Not-Qualified

             Other Contact (separate from member funnel)
```

---

## Housekeeping

Daily housekeeping in `PersonClass.executeDailyTasks()` handles cleanup:

1. **Backfill** — Legacy records with `personTypeId = 0` are classified based on `createdByVisit` and visitor/visit bot flags.
2. **Bot purge** — Records with `personTypeId = Bot` are deleted unconditionally.
3. **Guest purge** — Records with `personTypeId = Guest` are deleted if their `lastVisit` exceeds the configurable retention period (2–30 days).
4. **Legacy fallback** — The original join-based bot and heuristic-based guest cleanup queries still run for any records not yet backfilled.


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

## People Types

| Type | Description | Retention |
|---|---|---|
| **Bot** | A visit determined (via browser signature, IP, and/or behavior) to be non-human. The People record generated for that visit is flagged/named as a bot. | Not retained long-term. |
| **Guest** | A non-bot person tracked short-term. Default state for new, unrecognized human visitors. | Short-term; purged if never converted. |
| **Contact** | A person the business wants to retain permanently. | Permanent. |

Guests are promoted to Contacts (and typically un-flagged from `createdByVisit`, or otherwise exempted from purge) when they take an action worth remembering — filling out a form, being manually entered by an admin, making a purchase, etc.

---

## Contact Relationships

Once a People record is classified as a **Contact**, it is assigned one of six relationship types:

1. **Lead** — a population of contacts whose relationship is unknown.
2. **Unqualified (Prospect)** — contacts that have expressed an interest in a relationship, but we have not yet qualified them to be a member.
3. **Not-Qualified (Prospect)** — contacts that have shown an interest in a relationship, but we do not think they are qualified to be a member.
4. **Qualified Prospect** — contacts that have shown an interest, and we agree they are potential members.
5. **Member** — contacts who have joined.
6. **Other Contact** — contacts who are not in the member funnel. If Other is selected, the contact can be assigned an other relationship from the `OtherContactRelationships` table.

Admins move contacts through these relationship types manually, and/or the system may auto-advance them based on defined triggers.

The "Member" label is the underlying platform term. Addons may present vertical-specific labels to admins — for example, "Customer" for generic installs, "Member" for associations, or "Patient" for medical/dental practices. The data model is identical; only the display label changes.

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
People record determined from Visit
   |
   +--------+--------+
   v        v         v
 Bot     Guest     Contact
(purged) (short-   (permanent)
          term)        |
                       v
        Lead -> Unqualified -> Qualified Prospect -> Member
                    |
                    v
              Not-Qualified

        Other Contact (separate from member funnel)
```


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

## The Contact Funnel

Once a People record is classified as a **Contact**, it sits in one of four funnel stages:

1. **Lead**
2. **Prospect**
3. **Qualified Prospect**
4. **Member**

Admins move contacts forward through these stages manually, and/or the system may auto-advance stages based on defined triggers.

The final stage label ("Member") is the underlying platform term. Addons may present vertical-specific labels to admins — for example, "Customer" for generic installs, "Member" for associations, or "Patient" for medical/dental practices. The data model is identical; only the display label changes.

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
        Lead -> Prospect -> Qualified Prospect -> Member
```

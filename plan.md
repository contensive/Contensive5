# Plan: PersonType Promote-Only (Never Downgrade)

## Problem

The `PersonTypeEnum` hierarchy is `Unknown(0) < Bot(1) < Guest(2) < Contact(3)`. Two code paths can cause a Guest or Contact person record to be deleted as if it were a bot:

1. **Visitor bot-flag propagation** — [SessionController.cs:539-542](source/Processor/Controllers/SessionController.cs#L539-L542) unconditionally copies `visit.bot` to `visitor.bot`. If a Contact's returning visitor cookie is reused in a bot-detected request (e.g., an AI hitting the MCP endpoint), the visitor gets `bot=true`.

2. **Legacy housekeeping deletes** — [PersonClass.cs:105](source/Processor/Addons/Housekeeping/PersonClass.cs#L105) and [PersonClass.cs:114](source/Processor/Addons/Housekeeping/PersonClass.cs#L114) delete any person with `createdByVisit=1` joined to a bot visitor/visit, **regardless of personTypeId**. A Contact who was originally auto-created during a visit (`createdByVisit=1`) gets deleted because their visitor was flagged as bot.

**The deletion chain:** User authenticates normally (Contact, `createdByVisit=1`) → AI/MCP hits endpoint with same visitor cookie → bot-detected visit sets `visitor.bot=true` → housekeeping deletes the Contact because `createdByVisit=1 AND visitor.bot=1`.

## Changes

### 1. SessionController.cs — Refactor `createGuest` into `createPerson(personTypeId)` and create the correct type at point of creation

Replace the current pattern of "create a Guest, then override to Bot" with a `createPerson(PersonTypeEnum personTypeId)` method. The existing `createGuest` becomes a thin wrapper that calls `createPerson(PersonTypeEnum.Guest)`. At the new-user creation site (line 508), branch on `visit.bot` and create the correct type directly — no post-creation fixup needed.

**New method — replace `createGuest` (lines 624-645):**

```csharp
/// <summary>
/// Create a new person record with the specified type. Use for visit-created records
/// where the type is known at creation time (Bot or Guest). Sets createdByVisit=true.
/// </summary>
public static PersonModel createPerson(CoreController core, PersonTypeEnum personTypeId, string name, bool exitWithoutSave) {
    logger.Trace($"{core.logCommonMessage},SessionController.createPerson, enter, personTypeId={personTypeId}");
    //
    PersonModel user = DbBaseModel.addEmpty<PersonModel>(core.cpParent);
    user.createdByVisit = true;
    user.personTypeId = (int)personTypeId;
    user.name = name.substringSafe(0, 100);
    user.firstName = name.substringSafe(0, 100);
    user.createdBy = user.id;
    user.dateAdded = core.doc.profileStartTime;
    user.modifiedBy = user.id;
    user.modifiedDate = core.doc.profileStartTime;
    user.visits = 0;
    user.autoLogin = false;
    user.admin = false;
    user.developer = false;
    user.allowBulkEmail = true;
    if (!exitWithoutSave) { user.save(core.cpParent); }
    return user;
}
/// <summary>
/// Create a new guest person record. Wrapper for createPerson with Guest type.
/// </summary>
public static PersonModel createGuest(CoreController core, bool exitWithoutSave) {
    return createPerson(core, PersonTypeEnum.Guest, "Guest", exitWithoutSave);
}
```

**Update the new-user creation site (lines 508-526):**

Before:
```csharp
// -- setup new user if nothing else
if ((user is null) || user.id.Equals(0)) {
    user = createGuest(core, true);
    resultSessionContext_user_changes = true;
    //
    // -- if this is a bot, name the user record with the bot identifier and set person type
    if (visit.bot && !string.IsNullOrEmpty(visit.name) && !visit.name.Equals("user", StringComparison.OrdinalIgnoreCase)) {
        user.name = visit.name.substringSafe(0, 100);
        user.firstName = visit.name.substringSafe(0, 100);
        user.personTypeId = (int)PersonTypeEnum.Bot;
    }
    //
    visit.visitAuthenticated = false;
    visit.memberNew = true;
    visit.memberId = user.id;
    //
    visitor.memberId = user.id;
    visitor.name = visit.name.substringSafe(0, 100);
    resultSessionContect_visitor_changes = true;
}
```

After:
```csharp
// -- setup new user if nothing else
if ((user is null) || user.id.Equals(0)) {
    //
    // -- create the correct person type based on bot detection (already resolved above).
    //    Bot visits get personTypeId=Bot so housekeeping deletes them.
    //    Non-bot visits get personTypeId=Guest.
    if (visit.bot && !string.IsNullOrEmpty(visit.name) && !visit.name.Equals("user", StringComparison.OrdinalIgnoreCase)) {
        user = createPerson(core, PersonTypeEnum.Bot, visit.name, true);
    } else {
        user = createGuest(core, true);
    }
    resultSessionContext_user_changes = true;
    //
    visit.visitAuthenticated = false;
    visit.memberNew = true;
    visit.memberId = user.id;
    //
    visitor.memberId = user.id;
    visitor.name = visit.name.substringSafe(0, 100);
    resultSessionContect_visitor_changes = true;
}
```

The two other callers of `createGuest` ([SessionController.cs:65](source/Processor/Controllers/SessionController.cs#L65) and [AuthController.cs:121](source/Processor/Controllers/Authentication/AuthController.cs#L121)) are unchanged — they genuinely create guests.

### 2. SessionController.cs line 539-542 — Don't propagate bot flag to visitors associated with real people

Prevent `visitor.bot` from being set to `true` when the visitor's associated person is a Guest or Contact. This stops the deletion chain at its source.

**Before:**
```csharp
if (visitor.bot != visit.bot) {
    visitor.bot = visit.bot;
    resultSessionContect_visitor_changes = true;
}
```

**After:**
```csharp
if (!visitor.bot && visit.bot) {
    //
    // -- only flag visitor as bot if the associated person is not a Guest or Contact
    if (user == null || user.id == 0 || user.personTypeId <= (int)PersonTypeEnum.Bot) {
        visitor.bot = true;
        resultSessionContect_visitor_changes = true;
    }
}
```

This change also makes `visitor.bot` sticky (once true, stays true) and prevents clearing it on a non-bot visit, which is the correct behavior.

### 3. PersonClass.cs line 105 — Add personTypeId guard to legacy visitor-based bot delete

**Before:**
```csharp
string sql = "delete from ccmembers from ccmembers u left join ccvisitors v on v.MemberID=u.id where (u.createdbyvisit=1)and(v.bot=1)";
```

**After:**
```csharp
string sql = $"delete from ccmembers from ccmembers u left join ccvisitors v on v.MemberID=u.id where (u.createdbyvisit=1)and(v.bot=1)and(u.personTypeId<={(int)PersonTypeEnum.Bot})";
```

### 4. PersonClass.cs line 114 — Add personTypeId guard to legacy visit-based bot delete

**Before:**
```csharp
string sql = "delete from ccmembers from ccmembers u left join ccvisits v on v.MemberID=u.id where (u.createdbyvisit=1)and(v.bot=1)";
```

**After:**
```csharp
string sql = $"delete from ccmembers from ccmembers u left join ccvisits v on v.MemberID=u.id where (u.createdbyvisit=1)and(v.bot=1)and(u.personTypeId<={(int)PersonTypeEnum.Bot})";
```

These guards ensure the legacy delete paths only remove records typed as Bot(1) or Unknown(0), never Guest(2) or Contact(3).

## Summary

| # | File | Lines | Change |
|---|------|-------|--------|
| 1 | SessionController.cs | 508-526, 624-645 | Refactor `createGuest` into `createPerson(personTypeId)`, create Bot or Guest at point of creation based on `visit.bot` |
| 2 | SessionController.cs | 539-542 | Don't propagate `visitor.bot=true` when the visitor's person is Guest or Contact |
| 3 | PersonClass.cs | 105 | Add `personTypeId <= Bot` guard to legacy visitor-based bot delete |
| 4 | PersonClass.cs | 114 | Add `personTypeId <= Bot` guard to legacy visit-based bot delete |

These four changes enforce the promote-only rule at every level:
- **Creation (change 1):** Records are born with the correct type — no downgrade ever occurs.
- **Visitor propagation (change 2):** Bot flags don't contaminate visitors associated with real people.
- **Housekeeping deletion (changes 3-4):** Legacy delete queries cannot remove Guest or Contact records.

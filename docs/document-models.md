# Database Model Documentation Plan

## Overview

This document provides a comprehensive plan to document all database model properties in the `Contensive.Models.Db` namespace. Each model class represents a database table in the Contensive platform, and each property represents a field in that table.

**Goal:** Add XML summary documentation to every property in every model class to help developers understand the purpose, meaning, and usage of each database field.

**Audience:** The documentation has two audiences:
1. **End users** — see field help text in the admin UI (defined in collection XML `<HelpDefault>` tags)
2. **Developers** — see property documentation in code (defined in C# XML `<summary>` tags)

This plan focuses on developer documentation (XML summary tags in model classes).

## Current State

- **Total model classes:** 94 (excluding 3 base classes)
- **Total properties (active + authorable only):** ~635 (excluding inherited base fields and non-authorable/inactive fields)
- **Currently documented:** ~345 properties (54%)
- **Need documentation:** ~290 properties (46%)
- **Excluded (non-authorable or inactive):** ~51 fields skipped per documentation strategy

**Fully documented models (26):** ActivityLogModel, AuthoringControlModel, ConditionalEmailModel, ContentFieldTypeModel, ContentWatchListRuleModel, ContentWatchModel, DbTextModel, DomainModel, EmailBounceListModel, EmailGroupModel, EmailLogModel, EmailModel, EmailTopicModel, GroupEmailModel, GroupTextMessageModel, MemberTopicRuleModel, OAuthClientModel, OAuthCodeModel, OrganizationModel, PageContentBlockRuleModel, PageContentTopicRuleModel, PropertyModel, SystemEmailModel, SystemTextMessageModel, TableModel, TaskModel, TopicHabitModel, TopicModel, VisitModel, VisitorModel

**Completely undocumented models (31):** AdminRecentModel, AddonCollectionParentRuleModel, AddonContentTriggerRuleModel, AddonEventCatcherModel, AddonEventThrowerModel, AuthenticationLogModel, ContentFieldHelpModel, CopyContentModel, CountryModel, CustomReportModel, DataSourceModel, DbCustomBlockingVerificationEmailsModel, EmailDropModel, EmailTemplateModel, GroupModel, GroupRuleModel, GroupTextMessageGroupRuleModel, GroupTextMessageTopicRuleModel, ImportWizardTaskModel, LanguageModel, LibraryFileLogModel, LibraryFilesModel, LibraryFileTypeModel, LibraryFolderModel, LibraryFolderRuleModel, LinkAliasModel, LinkForwardModel, LoginByEmailOtpModel, MemberRuleModel, MenuModel, MenuPageRuleModel, NavigatorEntryModel, RemoteQueryModel, SitePropertyModel, SortMethodModelx, StateModel, SystemTextMessageGroupRuleModel, SystemTextMessageTopicRuleModel, TemplateDomainRuleModel, TextMessageLogModel, ViewingModel

## Documentation Strategy

### Approach

For each undocumented property:

1. **Check the collection XML** (aoBase51.xml) — only document the field if both `Active="true"` and `Authorable="1"`. Fields with `Active="0"` or `Authorable="0"` are deprecated/hidden and should be skipped.
2. **Search the codebase** for all references to the property to understand how it's used
3. **Check the `<HelpDefault>` text** in the collection XML — the end-user help text provides context
4. **Review related code** — controllers, addons, and services that interact with the model
5. **Write concise XML summary** using these conventions:
   - Boolean flags: "If true, [describe the behavior]"
   - Foreign keys: "FK to [ModelName] ([table].[field])"
   - Date fields: "The date/time when [event occurs]"
   - Descriptive fields: Brief phrase describing the field's purpose
   - Keep it under 1-2 sentences for simple fields, more for complex fields

### Prioritization

Models are organized into 10 phases based on usage frequency in the Processor codebase (most-used first). Within each phase, prioritize:
1. Models with zero documentation
2. Models with partial documentation (fill in gaps)
3. Already-documented models (skip unless issues found)

---

## Collection XML Field Help Reference

The collection XML file (`source/Processor/aoBase51.xml`) contains end-user help text for many fields. This help text is displayed in the admin UI and provides valuable context for understanding each field's purpose.

**Key CDefs with extensive help text:**
- **Add-ons** (ccAggregateFunctions) — ~70 fields documented
- **People** (ccMembers) — ~45 fields documented
- **Content Fields** (ccFields) — ~35 fields documented
- **Page Content** (ccPageContent) — ~25 fields documented
- **Add-on Collections** (ccAddonCollections) — ~18 fields documented

### Sample Field Help Text

Below are examples of how end-user help text maps to model properties:

**People (ccMembers) → PersonModel**
- `Email` → "The person's primary email address. Email is typically used as the login credential and for communications."
- `Admin` → "If true, this person has admin-level access to the site."
- `AllowBulkEmail` → "Indicates whether this person has opted in (or not opted out) of receiving bulk email."

**Content Fields (ccFields) → ContentFieldModel**
- `Caption` → "The caption is displayed to the user to describe this field."
- `Required` → "When checked, the content manager must enter a value in this field."
- `UniqueName` → "When checked, the record's Name field value must be unique within all records of this content."

**Add-ons (ccAggregateFunctions) → AddonModel**
- `DotNetClass` → "Enter the full namespace.class reference for the code. This requires the code to be present as a DLL in the addon folder."
- `IsInline` → "Check if this addon generates an html inline tag, like a person's name. When checked, this addon will be represented with a small inline icon."
- `RemoteMethod` → "When checked, this addon will be executed from a route in the url matching its name."

**Complete field help mapping available in aoBase51.xml** — see the "CDef Field Help" section at the end of this document for the full list.

---

## Implementation Phases

### Phase 1 (Top Priority — Most Used Models, 10 models)

1. **AddonModel** (content="add-ons", table="ccaggregatefunctions") — ~43 file references
   - collectionId : int [documented: no]
   - content : bool [documented: no]
   - copy : string [documented: no]
   - copyText : string [documented: no]
   - diagnostic : bool [documented: no]
   - dotNetClass : string [documented: no]
   - email : bool [documented: no]
   - filter : bool [documented: no]
   - formXML : string [documented: no]
   - link : string [documented: no]
   - metaDescription : string [documented: no]
   - metaKeywordList : string [documented: no]
   - onBodyEnd : bool [documented: no]
   - onBodyStart : bool [documented: no]
   - onPageEndEvent : bool [documented: no]
   - onPageStartEvent : bool [documented: no]
   - htmlDocument : bool [documented: no]
   - otherHeadTags : string [documented: no]
   - pageTitle : string [documented: no]
   - processInterval : int? [documented: no]
   - processNextRun : DateTime? [documented: no]
   - processRunOnce : bool [documented: no]
   - processServerKey : string [documented: no]
   - remoteAssetLink : string [documented: no]
   - robotsTxt : string [documented: no]
   - scriptingCode : string [documented: no]
   - scriptingEntryPoint : string [documented: no]
   - scriptingLanguageId : int [documented: no]
   - scriptingTimeout : string [documented: no]
   - stylesFilename : FieldTypeCSSFile [documented: no]
   - template : bool [documented: no]
   - javaScriptBodyEnd : string [documented: no]
   - jsBodyScriptSrc : string [documented: no]

2. **PersonModel** (content="people", table="ccmembers") — ~42 file references
   - bio : string [documented: no]
   - birthdayDay : int [documented: no]
   - birthdayMonth : int [documented: no]
   - birthdayYear : int [documented: no]
   - city : string [documented: no]
   - company : string [documented: no]
   - country : string [documented: no]
   - createdByVisit : bool [documented: no]
   - dateExpires : DateTime? [documented: no]
   - developer : bool [documented: no]
   - email : string [documented: no]
   - excludeFromAnalytics : bool [documented: no]
   - fax : string [documented: no]
   - firstName : string [documented: no]
   - imageFilename : string [documented: no]
   - languageId : int [documented: no]
   - lastName : string [documented: no]
   - lastVisit : DateTime? [documented: no]
   - nickName : string [documented: no]
   - organizationId : int [documented: no]
   - state : string [documented: no]
   - thumbnailFilename : string [documented: no]
   - username : string [documented: no]
   - visits : int [documented: no]
   - zip : string [documented: no]

3. **ContentModel** (content="Content", table="cccontent") — ~23 file references
   - adminOnly : bool [documented: no]
   - allowAdd : bool [documented: no]
   - allowContentChildTool : bool [documented: no]
   - allowDelete : bool [documented: no]
   - defaultSortMethodId : int [documented: no]
   - developerOnly : bool [documented: no]
   - dropDownFieldList : string [documented: no]
   - editorGroupId : int [documented: no]
   - iconHeight : int [documented: no]
   - iconLink : string [documented: no]
   - iconSprites : int [documented: no]
   - iconWidth : int [documented: no]
   - installedByCollectionId : int [documented: no]
   - parentId : int [documented: no]

4. **PageContentModel** (content="page content", table="ccpagecontent") — ~14 file references
   - allowBrief : bool [documented: no]
   - allowChildListDisplay : bool [documented: no]
   - allowFeedback : bool [documented: no]
   - allowHitNotification : bool [documented: no]
   - allowInChildLists : bool [documented: no]
   - allowInMenus : bool [documented: no]
   - allowLastModifiedFooter : bool [documented: no]
   - allowMessageFooter : bool [documented: no]
   - allowMetaContentNoFollow : bool [documented: no]
   - allowMoreInfo : bool [documented: no]
   - allowReviewedFooter : bool [documented: no]
   - archiveParentId : int [documented: no]
   - blockContent : bool [documented: no]
   - blockSourceId : int [documented: no]
   - briefFilename : FieldTypeHTMLFile [documented: no]
   - childListSortMethodId : int [documented: no]
   - clicks : int [documented: no]
   - contactMemberId : int [documented: no]
   - contentPadding : int [documented: no]
   - customBlockMessage : FieldTypeHTMLFile [documented: no]
   - dateArchive : DateTime? [documented: no]
   - dateExpires : DateTime? [documented: no]
   - dateReviewed : DateTime? [documented: no]
   - headline : string [documented: no]
   - imageFilename : FieldTypeFile [documented: no]
   - jSEndBody : string [documented: no]
   - jSFilename : FieldTypeJavascriptFile [documented: no]
   - jSHead : string [documented: no]
   - jSOnLoad : string [documented: no]
   - menuHeadline : string [documented: no]
   - metaDescription : string [documented: no]
   - metaKeywordList : string [documented: no]
   - otherHeadTags : string [documented: no]
   - pageTitle : string [documented: no]
   - structuredData : string [documented: no]
   - parentId : int [documented: no]
   - parentListName : string [documented: no]
   - pubDate : DateTime? [documented: no]
   - registrationGroupId : int [documented: no]
   - reviewedBy : int [documented: no]
   - templateId : int [documented: no]
   - triggerAddGroupId : int [documented: no]
   - triggerConditionGroupId : int [documented: no]
   - triggerConditionId : int [documented: no]
   - triggerRemoveGroupId : int [documented: no]
   - triggerSendSystemEmailId : int [documented: no]

5. **PropertyModel** (content="properties", table="ccProperties") — ~14 file references
   - *(fully documented)*

6. **AddonCollectionModel** (content="Add-on Collections", table="ccaddoncollections") — ~11 file references
   - system : bool [documented: no]
   - updatable : bool [documented: no]
   - wwwFileList : string [documented: no]
   - oninstalladdonid : int [documented: no]

7. **TaskModel** (content="tasks", table="cctasks") — ~9 file references
   - *(fully documented)*

8. **TableModel** (content="tables", table="cctables") — ~9 file references
   - *(fully documented)*

9. **ContentFieldModel** (content="content fields", table="ccfields") — ~8 file references
   - indexSortPriority : int [documented: no]
   - memberSelectGroupId : int [documented: no]
   - notEditable : bool [documented: no]
   - password : bool [documented: no]
   - readOnly : bool [documented: no]
   - redirectContentId : int [documented: no]
   - redirectId : string [documented: no]
   - redirectPath : string [documented: no]
   - required : bool [documented: no]
   - rssDescriptionField : bool [documented: no]
   - rssTitleField : bool [documented: no]
   - scramble : bool [documented: no]
   - textBuffered : bool [documented: no]
   - uniqueName : bool [documented: no]

10. **LayoutModel** (content="layouts", table="cclayouts") — ~8 file references
    - *(fully documented)*

---

### Phase 2 (High Priority — 10 models)

11. **DomainModel** (content="domains", table="ccdomains") — ~7 file references
    - *(fully documented)*

12. **LinkAliasModel** (content="link aliases", table="cclinkaliases") — ~7 file references
    - pageId : int [documented: no]
    - queryStringSuffix : string [documented: no]

13. **PageTemplateModel** (content="page templates", table="cctemplates") — ~7 file references
    - bodyHTML : string [documented: no]
    - collectionId : int [documented: no]
    - OtherHeadTags : string [documented: no]
    - StructuredData : string [documented: no]

14. **GroupModel** (content="groups", table="ccgroups") — ~6 file references
    - allowBulkEmail : bool [documented: no]
    - caption : string [documented: no]
    - copyFilename : DbBaseModel.FieldTypeTextFile [documented: no]
    - publicJoin : bool [documented: no]

15. **ActivityLogModel** (content="Activity Log", table="ccActivityLog") — ~5 file references
    - *(fully documented)*

16. **VisitModel** (content="visits", table="ccvisits") — ~5 file references
    - *(fully documented)*

17. **EmailModel** (content="email", table="ccemail") — ~4 file references
    - *(fully documented)*

18. **LibraryFilesModel** (content="library Files", table="cclibraryfiles") — ~4 file references
    - altSizeList : string [documented: no]
    - clicks : int [documented: no]
    - description : string [documented: no]
    - filename : string [documented: no]
    - fileSize : int [documented: no]
    - folderId : int [documented: no]
    - height : int [documented: no]
    - width : int [documented: no]

19. **LinkForwardModel** (content="link forwards", table="cclinkforwards") — ~4 file references
    - destinationLink : string [documented: no]
    - groupId : int [documented: no]
    - sourceLink : string [documented: no]
    - viewings : int [documented: no]

20. **NavigatorEntryModel** (content="Navigator Entries", table="ccmenuentries") — ~4 file references
    - parentId : int [documented: no]
    - addonId : int [documented: no]
    - adminOnly : bool [documented: no]
    - contentId : int [documented: no]
    - developerOnly : bool [documented: no]
    - helpAddonId : int [documented: no]
    - helpCollectionId : int [documented: no]
    - installedByCollectionId : int [documented: no]
    - linkPage : string [documented: no]
    - newWindow : bool [documented: no]

---

### Phase 3 (Medium Priority — 10 models)

21. **AdminRecentModel** (content="admin recents", table="ccadminrecents") — ~3 file references
    - userId : int [documented: no]
    - href : string [documented: no]
    - contentId : int [documented: no]
    - addonId : int [documented: no]

22. **DownloadModel** (content="Downloads", table="ccdownloads") — ~3 file references
    - requestedBy : int [documented: no]
    - dateRequested : DateTime? [documented: no]
    - dateCompleted : DateTime? [documented: no]
    - resultMessage : string [documented: no]

23. **EmailBounceListModel** (content="Email Bounce List", table="EmailBounceList") — ~3 file references
    - *(fully documented)*

24. **EmailDropModel** (content="Email Drops", table="ccemaildrops") — ~3 file references
    - emailId : int [documented: no]

25. **EmailLogModel** (content="email log", table="ccemaillog") — ~3 file references
    - *(fully documented)*

26. **EmailTemplateModel** (content="email templates", table="cctemplates") — ~3 file references
    - bodyHTML : string [documented: no]

27. **MemberRuleModel** (content="member rules", table="ccmemberrules") — ~3 file references
    - dateExpires : DateTime? [documented: no]
    - groupId : int [documented: no]
    - memberId : int [documented: no]
    - groupRoleId : int [documented: no]

28. **VisitorModel** (content="visitors", table="ccvisitors") — ~3 file references
    - *(fully documented)*

29. **AddonIncludeRuleModel** (content="Add-on Include Rules", table="ccaddonincluderules") — ~2 file references
    - *(fully documented)*

30. **AddonTemplateRuleModel** (content="add-on template rules", table="ccAddontemplaterules") — ~2 file references
    - *(fully documented)*

---

### Phase 4 (Medium Priority — 10 models)

31. **ContentFieldHelpModel** (content="content field help", table="ccfieldhelp") — ~2 file references
    - fieldId : int [documented: no]
    - helpCustom : string [documented: no]
    - helpDefault : string [documented: no]

32. **CopyContentModel** (content="Copy Content", table="cccopycontent") — ~2 file references
    - copy : string [documented: no]

33. **DbCustomBlockingVerificationEmailsModel** (content="Custom Blocking Verification Emails", table="ccCustomBlockingVerificationEmails") — ~2 file references
    - emailSentTo : string [documented: no]

34. **GroupTextMessageModel** (content="group text messages", table="ccGroupTextMessages") — ~2 file references
    - *(fully documented)*

35. **LoginByEmailOtpModel** (content="Login By Email Otp", table="ccLoginByEmailOtp") — ~2 file references
    - email : string [documented: no]
    - otp : string [documented: no]
    - expires : DateTime [documented: no]
    - used : bool [documented: no]

36. **OAuthCodeModel** (content="OAuth Authorization Codes", table="ccOAuthCodes") — ~2 file references
    - *(fully documented)*

37. **OrganizationModel** (content="organizations", table="organizations") — ~2 file references
    - *(fully documented)*

38. **SitePropertyModel** (content="Site Properties", table="ccsetup") — ~2 file references
    - fieldValue : string [documented: no]

39. **SystemEmailModel** (content="system email", table="ccemail") — ~2 file references
    - *(fully documented)*

40. **SystemTextMessageModel** (content="system text messages", table="ccSystemTextMessages") — ~2 file references
    - *(fully documented)*

---

### Phase 5 (Low Priority — Used Models, 10 models)

41. **AuthenticationLogModel** (content="Authentication Log", table="ccAuthenticationLog") — ~1 file reference
    - success : bool [documented: no]
    - memberId : int [documented: no]

42. **AuthoringControlModel** (content="Authoring Controls", table="ccauthoringcontrols") — ~1 file reference
    - *(fully documented)*

43. **CommonPasswordModel** (content="Common Passwords", table="cccommonpasswords") — ~1 file reference
    - *(no additional properties)*

44. **ContentFieldTypeModel** (content="Content Field Types", table="ccfieldtypes") — ~1 file reference
    - *(fully documented)*

45. **EmailQueueModel** (content="email queue", table="ccemailqueue") — ~1 file reference
    - toAddress : string [documented: no]
    - subject : string [documented: no]
    - content : string [documented: no]
    - immediate : bool [documented: no]
    - attempts : int [documented: no]
    - sendingProcessExpiration : DateTime? [documented: no]

46. **GroupRuleModel** (content="group rules", table="ccgrouprules") — ~1 file reference
    - allowAdd : bool [documented: no]
    - allowDelete : bool [documented: no]
    - contentId : int [documented: no]
    - groupId : int [documented: no]

47. **OAuthClientModel** (content="OAuth Clients", table="ccOAuthClients") — ~1 file reference
    - *(fully documented)*

48. **RemoteQueryModel** (content="remote queries", table="ccremotequeries") — ~1 file reference
    - allowInactiveRecords : bool [documented: no]
    - contentId : int [documented: no]
    - criteria : string [documented: no]
    - dataSourceId : int [documented: no]
    - dateExpires : DateTime? [documented: no]
    - maxRows : int [documented: no]
    - queryTypeId : int [documented: no]
    - remoteKey : string [documented: no]
    - selectFieldList : string [documented: no]
    - sortFieldList : string [documented: no]
    - sqlQuery : string [documented: no]
    - visitId : int [documented: no]

49. **SiteWarningModel** (content="Site Warnings", table="ccSiteWarnings") — ~1 file reference
    - count : int [documented: no]
    - dateLastReported : DateTime [documented: no]
    - description : string [documented: no]

50. **TemplateDomainRuleModel** (content="Template Domain Rules", table="ccdomaintemplaterules") — ~1 file reference
    - domainId : int [documented: no]
    - templateId : int [documented: no]

---

### Phase 6 (Low Priority — Used Models, 10 models)

51. **TextMessageLogModel** (content="Text Message log", table="ccTextMessagelog") — ~1 file reference
    - systemTextMessageId : int [documented: no]
    - groupTextMessageId : int [documented: no]
    - memberId : int [documented: no]
    - sendStatus : string [documented: no]
    - toPhone : string [documented: no]
    - body : string [documented: no]

52. **TextMessageQueueModel** (content="text message queue", table="cctextmessagequeue") — ~1 file reference
    - toPhone : string [documented: no]
    - content : string [documented: no]
    - immediate : bool [documented: no]
    - attempts : int [documented: no]

53. **UsedPasswordModel** (content="Used Passwords", table="ccUsedPasswords") — ~1 file reference
    - memberId : int [documented: no]

54. **ViewingModel** (content="viewings", table="ccviewings") — ~1 file reference
    - excludeFromAnalytics : bool [documented: no]
    - form : string [documented: no]
    - host : string [documented: no]
    - memberId : int [documented: no]
    - page : string [documented: no]
    - pageTime : int [documented: no]
    - pageTitle : string [documented: no]
    - path : string [documented: no]
    - queryString : string [documented: no]
    - recordId : int [documented: no]
    - referer : string [documented: no]
    - stateOK : bool [documented: no]
    - visitId : int [documented: no]
    - visitorId : int [documented: no]

55. **AddonCategoryModel** (content="add-on categories", table="ccaddoncategories") — ~0 file references
    - *(no additional properties)*

56. **AddonCollectionCDefRuleModel** (content="Add-on Collection CDef Rules", table="ccAddonCollectionCDefRuleModel") — ~0 file references
    - *(no additional properties)*

57. **AddonCollectionParentRuleModel** (content="Add-on Collection Parent Rules", table="ccAddonCollectionParentRules") — ~0 file references
    - childId : int [documented: no]
    - parentId : int [documented: no]

58. **AddonContentFieldTypeRulesModel** (content="add-on Content Field Type Rules", table="ccaddoncontentfieldtyperules") — ~0 file references
    - *(fully documented)*

59. **AddonContentTriggerRuleModel** (content="Add-on Content Trigger Rules", table="ccAddonContentTriggerRules") — ~0 file references
    - addonId : int [documented: no]
    - contentId : int [documented: no]

60. **AddonEventCatcherModel** (content="Add-on Event Catchers", table="ccAddonEventCatchers") — ~0 file references
    - addonId : int [documented: no]
    - eventId : int [documented: no]

---

### Phase 7 (Unused Models — 10 models)

61. **AddonEventModel** (content="Add-on Events", table="ccAddonEvents") — ~0 file references
    - *(no additional properties)*

62. **AddonEventThrowerModel** (content="Add-on Event Throwers", table="ccAddonEventThrowers") — ~0 file references
    - addonId : int [documented: no]
    - eventId : int [documented: no]

63. **AddonPageRuleModel** (content="add-on page rules", table="ccaddonpagerules") — ~0 file references
    - *(fully documented)*

64. **ConditionalEmailModel** (content="conditional email", table="ccemail") — ~0 file references
    - *(fully documented)*

65. **ContentWatchListModel** (content="Content Watch Lists", table="ccContentWatchLists") — ~0 file references
    - *(no additional properties)*

66. **ContentWatchListRuleModel** (content="Content Watch List Rules", table="ccContentWatchListRules") — ~0 file references
    - *(fully documented)*

67. **ContentWatchModel** (content="Content Watch", table="ccContentWatch") — ~0 file references
    - *(fully documented)*

68. **CountryModel** (content="Countries", table="ccCountries") — ~0 file references
    - abbreviation : string [documented: no]
    - domesticShipping : bool [documented: no]

69. **CustomReportModel** (content="Custom Reports", table="ccCustomReports") — ~0 file references
    - sqlQuery : string [documented: no]

70. **DataSourceModel** (content="data sources", table="ccdatasources") — ~0 file references
    - connString : string [documented: no]
    - endpoint : string [documented: no]
    - username : string [documented: no]
    - password : string [documented: no]
    - dbTypeId : int [documented: no]
    - secure : bool [documented: no]

---

### Phase 8 (Unused Models — 10 models)

71. **DbTextModel** (content="db Text", table="dbText") — ~0 file references
    - *(fully documented)*

72. **EmailGroupModel** (content="Email Groups", table="ccEmailGroups") — ~0 file references
    - *(fully documented)*

73. **EmailTopicModel** (content="Email Topics", table="ccEmailTopics") — ~0 file references
    - *(fully documented)*

74. **GroupEmailModel** (content="group email", table="ccemail") — ~0 file references
    - *(fully documented)*

75. **GroupRoleModel** (content="Group Roles", table="ccgrouproles") — ~0 file references
    - *(no additional properties)*

76. **GroupTextMessageGroupRuleModel** (content="group text message group rules", table="ccGroupTextMessageGroupRules") — ~0 file references
    - groupId : int [documented: no]
    - groupTextMessageId : int [documented: no]

77. **GroupTextMessageTopicRuleModel** (content="group text message topic rules", table="ccGroupTextMessageTopicRules") — ~0 file references
    - topicId : int [documented: no]
    - groupTextMessageId : int [documented: no]

78. **ImportWizardTaskModel** (content="Import Wizard Tasks", table="importWizardTasks") — ~0 file references
    - dateCompleted : DateTime [documented: no]
    - dateStarted : DateTime [documented: no]
    - importMapFilename : string [documented: no]
    - notifyEmail : string [documented: no]
    - resultMessage : string [documented: no]
    - uploadFilename : string [documented: no]

79. **LanguageModel** (content="languages", table="cclanguages") — ~0 file references
    - http_Accept_Language : string [documented: no]

80. **LibraryFileLogModel** (content="library File log", table="cclibrarydownloadlog") — ~0 file references
    - fileId : int [documented: no]
    - memberId : int [documented: no]
    - visitId : int [documented: no]
    - fromUrl : string [documented: no]

---

### Phase 9 (Unused Models — 10 models)

81. **LibraryFileTypeModel** (content="Library File Types", table="ccLibraryFileTypes") — ~0 file references
    - downloadIconFilename : string [documented: no]
    - extensionList : string [documented: no]
    - iconFilename : string [documented: no]
    - isDownload : bool [documented: no]
    - isFlash : bool [documented: no]
    - isImage : bool [documented: no]
    - isVideo : bool [documented: no]
    - mediaIconFilename : string [documented: no]

82. **LibraryFolderModel** (content="Library Folders", table="ccLibraryFolders") — ~0 file references
    - description : string [documented: no]
    - parentId : int [documented: no]

83. **LibraryFolderRuleModel** (content="Library Folder Rules", table="ccLibraryFolderRules") — ~0 file references
    - folderId : int [documented: no]
    - groupId : int [documented: no]

84. **MemberTopicRuleModel** (content="Member Topic Rules", table="ccMemberTopicRules") — ~0 file references
    - *(fully documented)*

85. **MenuModel** (content="Menus", table="ccmenus") — ~0 file references
    - classTopParentItem : string [documented: no]
    - classTopAnchor : string [documented: no]
    - classTopParentAnchor : string [documented: no]
    - dataToggleTopParentAnchor : string [documented: no]
    - classTierAnchor : string [documented: no]
    - classTopWrapper : string [documented: no]
    - classTopList : string [documented: no]
    - classTopItem : string [documented: no]
    - classItemActive : string [documented: no]
    - classTierList : string [documented: no]
    - classTierItem : string [documented: no]
    - classItemFirst : string [documented: no]
    - classItemLast : string [documented: no]
    - classItemHover : string [documented: no]

86. **MenuPageRuleModel** (content="Menu Page Rules", table="ccmenupagerules") — ~0 file references
    - menuId : int [documented: no]
    - pageId : int [documented: no]

87. **PageContentBlockRuleModel** (content="Page Content Block Rules", table="ccPageContentBlockRules") — ~0 file references
    - *(fully documented)*

88. **PageContentTopicRuleModel** (content="page content topic rules", table="ccpagecontenttopicrules") — ~0 file references
    - *(fully documented)*

89. **PageViewSummaryModel** (content="Page View Summary", table="ccPageViewSummary") — ~0 file references
    - *(no additional properties)*

90. **SortMethodModelx** (content="sort methods", table="ccSortMethods") — ~0 file references
    - orderByClause : string [documented: no]

---

### Phase 10 (Unused Models — Final 7 models)

91. **StateModel** (content="states", table="ccstates") — ~0 file references
    - abbreviation : string [documented: no]
    - countryId : int [documented: no]
    - salesTax : double [documented: no]

92. **SystemTextMessageGroupRuleModel** (content="system text message group rules", table="ccSystemTextMessageGroupRules") — ~0 file references
    - groupId : int [documented: no]
    - systemTextMessageId : int [documented: no]

93. **SystemTextMessageTopicRuleModel** (content="system text message topic rules", table="ccSystemTextMessageTopicRules") — ~0 file references
    - topicId : int [documented: no]
    - systemTextMessageId : int [documented: no]

94. **TopicHabitModel** (content="Topic Habits", table="Topic Habits") — ~0 file references
    - *(fully documented)*

95. **TopicModel** (content="topics", table="ccTopics") — ~0 file references
    - *(fully documented)*

96. **VisitSummaryModel** (content="visit summary", table="ccvisitsummary") — ~0 file references
    - authenticatedVisits : int [documented: yes]
    - aveTimeOnSite : int [documented: yes]
    - botVisits : int [documented: yes]
    - botPageViews : int [documented: yes]
    - dateNumber : int [documented: no]
    - mobileVisits : int [documented: no]
    - newVisitorVisits : int [documented: no]
    - noCookieVisits : int [documented: no]
    - pagesViewed : int [documented: no]
    - singlePageVisits : int [documented: no]
    - timeDuration : int [documented: no]
    - timeNumber : int [documented: no]
    - visits : int [documented: no]

---

## Progress Tracking

Use this checklist to track completion:

- [ ] Phase 1 complete (10 models)
- [ ] Phase 2 complete (10 models)
- [ ] Phase 3 complete (10 models)
- [ ] Phase 4 complete (10 models)
- [ ] Phase 5 complete (10 models)
- [ ] Phase 6 complete (10 models)
- [ ] Phase 7 complete (10 models)
- [ ] Phase 8 complete (10 models)
- [ ] Phase 9 complete (10 models)
- [ ] Phase 10 complete (7 models)

---

## Appendix: Complete CDef Field Help Text from aoBase51.xml

The following is a complete mapping of all CDef names and their fields with non-empty `<HelpDefault>` text from the collection XML file. This serves as a reference when documenting model properties.

### Data Sources (ccDataSources)
- **Secure**: "When unchecked, the connection to the database can be made without a secure certificate (SSL/TLS If checked, a secure connection is Required.)"

### Content (ccContent)
- **ParentID**: "Optional. When not set queries for this content should ignore the contentcontrolid, and all records in the content table are in this content. When set, queries should include contentcontrolid for this content id plus any that have this as their parent."
- **Field Definitions**: "This is a list of fields used for this content. It does not include fields inherited from parent tables and from the base content."
- **Abbreviation**: "An abbreviation used for navigation"
- **NavTypeId**: "Set how administrators will find this content in the admin site. Select the navagation section on the top right of the admin site where a administrators will find this content. Also set the Admin Navigation Category to determine where the content appears in that list."
- **AddonCategoryId**: "Select the category in the top right admin navagation that helps the content manager locate this addon."
- **IconHtml**: "Enter html to be used for the add-on's icon. We recommend 60px by 60px. The image can be a single image, or a vertical set of images treated as a sprite with images for normal, over, active, disabled. ex <div class=\"iconContentDefault\"><i class=\"fas fa-book\"></i></div>"

### Content Fields (ccFields)
*(35 fields with help text — see aoBase51.xml lines 12-84 for full text)*

### Visitors (ccVisitors)
- **CookieSupport**: "Indicates if the user's browser supports cookies."
- **Bot**: "Set true if this visitor appears to be a bot or crawler."
- **Fingerprint**: "A composite key built from visitor characteristics to identify this visitor across visits."

### Visits (ccVisits)
*(18 fields with help text including Name, VisitorID, LoginAttempts, MemberID, etc.)*

### Navigator Entries (ccMenuEntries)
- **InstalledByCollectionID**: "The addon collection that installed this record."

### People (ccMembers)
*(45 fields with help text including Name, Email, Username, Password, FirstName, etc.)*

### Groups (ccGroups)
- **Name**: "A unique name for the group."
- **Caption**: "A display caption for the group."
- **AllowBulkEmail**: "If true, the group will be offered in the bulk email tool's group selection."
- **PublicJoin**: "If true, users can join this group from public join forms."
- **CopyFilename**: "Optional html content associated with this group."

### Member Rules (ccMemberRules)
- **MemberID**: "The person in this rule."
- **GroupID**: "The group in this rule."
- **DateExpires**: "If set, the person's membership in this group expires at this date."

### Organizations (organizations)
*(9 fields with help text including Name, Address1, City, State, Phone, etc.)*

### Page Content (ccPageContent)
*(30+ fields with help text including Name, ParentID, Copyfilename, TemplateID, etc.)*

### System Text Messages (ccTextMessages)
- **Body**: "The text body of the system message."
- **collectionId**: "If a collection is selected, this record was added by the installation of this collection, and if the collection is reinstalled the data will be overwritten. To customize this data, make a copy of this record and use the copy."

### Group Text Messages (ccGroupTextMessages)
*(7 fields with help text including Body, FromPhone, Submitted, etc.)*

### Text Message Log (ccTextMessageLog)
*(6 fields with help text)*

### Email (ccEmail)
*(12 fields with help text including Name, Subject, FromAddress, etc.)*

### Email Templates (ccEmailTemplates)
- **Name**: "The name for this email template."
- **BodyHTML**: "The HTML body of the email template."
- **collectionId**: "If a collection is selected, this record was added by the installation of this collection..."

### Email Queue (ccEmailQueue)
- **sendingProcessKey**: "A key representing the process sending this email."
- **sendingProcessExpiration**: "The date/time when the sending process expires."

### Email Bounce List (ccEmailBounceList)
- **Name**: "The bounced email address."
- **Details**: "Details about the bounce."
- **transient**: "If checked, this is a transient (soft) bounce."

### Text Message Queue (ccTextMessageQueue)
- **toPhone**: "The phone number to which the message will be sent."
- **Body**: "The body text of the message."

### Countries (ccCountries)
- **Name**: "The name of the country."
- **Abbreviation**: "The standard country abbreviation."
- **DomainExtension**: "The country's internet domain extension."
- **PhoneCode**: "The international dialing code for this country."

### See Also (ccContentWatch)
- **Link**: "The URL link for this See Also entry."
- **LinkLabel**: "The label displayed for the link."

### Properties (ccProperties)
- **KeyID**: "The identifier for the entity to which this property belongs."
- **FieldValue**: "The value of the property."
- **TypeID**: "The type of entity this property belongs to."

### Add-on Categories (ccAddonCategories)
- **collectionId**: "If a collection is selected, this record was added by the installation of this collection..."

### Add-on Event Throwers (ccAddonEventThrowers)
- **collectionId**: "If a collection is selected, this record was added by the installation of this collection..."

### Add-ons (ccAggregateFunctions)
*(~70 fields with help text including Name, DotNetClass, ScriptingCode, IsInline, RemoteMethod, ProcessInterval, etc.)*

### Add-on Collection CDef Rules (ccAddonCollectionCDefRules)
- **CollectionID**: "If a collection is selected, this record was added by the installation of this collection..."

### Add-on Collections (ccAddonCollections)
*(18 fields with help text including Name, System, Updatable, ChildCollections, etc.)*

### Library Folders (ccLibraryFolders)
- **Name**: "Name of the folder."
- **Description**: "Brief description of the folder."
- **ParentID**: "If empty, this is a root folder."

### Library Folder Rules (ccLibraryFolderRules)
- **FolderID**: "The folder associated to this permission."
- **GroupID**: "The group associated to this permission."

### Library Files (ccLibraryFiles)
*(10 fields with help text including Name, Description, Filename, FileSize, etc.)*

### Link Aliases (ccLinkAliases)
- **Spidered**: "If unchecked, this means this Page URL has not been spidered and is out of date"
- **DateSpidered**: "This is the date when this link was last spidered"

### Domains (ccDomains)
*(11 fields with help text including Name, TypeID, RootPageID, ForwardURL, etc.)*

### Page Templates (ccTemplates)
*(6 fields with help text including Name, BodyHTML, mustacheDataSetAddonId, etc.)*

### Tasks (ccTasks)
- **timeout**: "If not 0 or blank, the time in secdonds allowed for this process in the background."

### Activity Log (ccactivitylog)
*(11 fields with help text including Name, MemberID, typeId, Message, etc.)*

### Layouts (ccLayouts)
- **Layout**: "The styles + html + Javascript used when the site is set to Html Platform version 4..."
- **LayoutPlatform5**: "The styles + html + Javascript for this layout when the site is set to Html Platform version 5..."
- **InstalledByCollectionID**: "The addon collection that installed this record."

### Site Warnings (ccSiteWarnings)
*(10 fields with help text including Name, DateLastReported, count, description, etc.)*

### Menu Page Rules (ccMenuPageRules)
- **collectionId**: "If a collection is selected, this record was added by the installation of this collection..."

### Menus (ccMenus)
- **Name**: "The name for this record."
- **pagesIncludedManyToMany**: "Optional. If any menus are checked, this page will appear as a root page on the menu."
- **collectionId**: "If a collection is selected, this record was added by the installation of this collection..."

### Admin Menuing (ccMenuEntries)
- **InstalledByCollectionID**: "The addon collection that installed this record."

### Admin Recents (ccAdminRecents)
- **userid**: "User who click this recently"
- **contentId**: "If this recent link is to content, set to the contentid."
- **addonId**: "If this recent link is to an addon, set to the id of the addon."
- **href**: "Recent admin hits for addons and data"

### Used Passwords (ccUsedPasswords)
- **memberid**: "The user that used this Password."

### Authentication Log (ccAuthenticationLog)
- **success**: "checked if this login was successful."
- **memberid**: "The user that used this Password."
- **detail**: "Detais about this authentication attempt."

### And several more... (see full aoBase51.xml for complete list)

---

**Document version:** 1.0
**Created:** 2026-09-02
**Last updated:** 2026-09-02

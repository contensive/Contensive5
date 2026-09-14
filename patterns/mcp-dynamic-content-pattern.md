# MCP Dynamic Content Pattern (Draft Recommendation)

> Companion to [Page Widget Pattern](addon-page-widget-pattern.md), [Remote Method Pattern](addon-remote-method-pattern.md), and [Contensive MCP Server](../docs/contensive-mcp-server.md). Written to answer two design questions raised while planning MCP tools for the Blog addon (and, after it, Newsletter/Forum): how do we tell an AI client about the page/widget/dynamic-content hierarchy, and how do we let addons register their own URL-addressable tools consistently?

## 1. The problem, restated from the existing code

`ContensiveMcpServer` today identifies everything by **URL**, resolved to `(pageId, queryStringSuffix)` via the link-alias table (see `contensive-mcp-server.md`, "URL-Centric Paradigm"). `page_get` returns the page's metadata plus its widget list; `widget_instance_update` edits one widget instance's Settings-record fields by `instanceGuid`.

That model has two tiers: **Page** (`ccpagecontent`) and **Widget instance settings** (the addon's Settings content record, per `addon-page-widget-pattern.md`'s `instanceSettingPrimaryContentId`). It works for widgets like Text Block or Gallery, where the whole page maps 1:1 to one record.

The Blog addon breaks that assumption, and Newsletter/Forum will too. Looking at `aoBlog/source/aoBlogs2`:

- `BlogWidget.cs` renders using `BlogModel` as its Settings record — this is "the Blog" (name, `metaTitle`, `metaDescription`, `metaKeywordList`, `defaultImageFilename`). One per widget instance. **Tier B.**
- A specific post is a separate content record, `BlogEntryModel` (table `ccBlogCopy`), selected at render time by the querystring `BlogEntryID={id}&FormID=300` (`BlogBodyRequestModel`, `constants.RequestNameBlogEntryID`). It has its *own* `metaTitle`/`metaDescription`/`metaKeywordList`/`name`/`copy`/`tagList`, entirely separate from `BlogModel`'s fields of the same name. **Tier C — new.**
- `MetadataController.setEntryMetadata()` and `StructuredDataController.addBlogPostingJsonLd()` are what actually push the post's own fields into `cp.Doc` (title, meta description, JSON-LD) at render time, overriding whatever the page or the Blog record would have supplied as fallback. This confirms Tier C is what a browser (and a search engine) actually sees for `/getting-the-most-out-of-your-annual-wellness-visit`, not Tier A or B.
- The existing admin forms (`BlogPostSeoAddon`, `BlogPostDetailsAddon`, `BlogPostInfoAddon`) already group a post's own fields exactly this way: SEO (`metaTitle`/`metaDescription`/`metaKeywordList`), Content (`name`/`copy`), Info (`tagList`/`active`/`datePublished`/`allowComments`). That grouping is a validated, human-tested field split worth reusing for the new MCP tool.

So "update the meta data for this blog post" must land on Tier C (`BlogEntryModel`), never on Tier A (the `/blog` page) or Tier B (the `BlogModel` widget settings) — even though all three have fields literally named `metaTitle`/`metaDescription`. Today's tools have no way to express that distinction, and a generic admin has no way to know it exists.

## 2. Challenge 1 — telling Claude (or any MCP client) about the hierarchy

The admin who types "update the meta data on this blog post URL" is connected only to the deployed MCP server — they have not read this repo's `patterns/` docs, and won't in general. Whatever explains the model has to travel over the MCP protocol itself. Three channels exist; use each for what it's good at, and don't rely on the AI having any out-of-band knowledge:

1. **Server-level instructions, once per session.** The MCP spec's `initialize` response carries a free-text `instructions` field; most clients (Claude Desktop, Claude Code) surface it to the model as context automatically — this is exactly the "MCP Server Instructions" block already used by other connectors in this environment. `ContensiveMcpServer`'s `Program.cs` should set this (`AddMcpServer(options => options.ServerInstructions = "...")` — confirm the exact hook against the installed `ModelContextProtocol` SDK version) with a short primer: pages vs. widget-instance-settings vs. dynamic child content; URLs are the only identifier you need; **always call `page_get` first — its response tells you exactly which tool edits which piece of what you see.**

2. **Self-describing `page_get` results, every call.** This is the load-bearing mechanism, because it's dynamic — it can say "this specific URL currently shows post #6" instead of a static generality. See the schema change in §3. A tool result that names the exact follow-up tool and arguments removes the need for the client to have memorized any convention at all.

3. **Tool description strings** stay as they are — good for "how do I call this tool," not for "which tool applies here." Don't lean on them for the hierarchy question.

Explicitly *not* recommended: shipping the architecture only as a markdown doc in this repo and hoping the connected AI has read it. It won't have, for any admin other than you working directly in this codebase.

If you want a deeper reference for power users, expose it as an MCP **Resource** (e.g. `contensive://docs/content-model`) and have `page_get` mention its URI — but treat it as optional reading, not the mechanism the model depends on.

## 3. Challenge 2 — a consistent pattern for widgets with their own URL-addressable records

### Three tiers, three tool families

| Tier | What it is | Example | Tool(s) |
|---|---|---|---|
| A — Page | `ccpagecontent` record for the URL | the `/blog` page itself | `page_update` (existing, unchanged) |
| B — Widget instance settings | the addon's Settings record, one per `instanceGuid` (`instanceSettingPrimaryContentId`) | `BlogModel` — the Blog's own name/fallback SEO | `widget_instance_update` (existing, unchanged) |
| C — Dynamic child content | a separate content record selected by the URL's querystring, addon-owned, not stored on the widget instance | `BlogEntryModel` — one specific post | **new**, addon-specific, see below |

### Convention for Tier C tools

- **Naming:** `{addonSlug}_{recordSingular}_get` / `_update` — e.g. `blog_post_get`/`blog_post_update`, and later `newsletter_issue_get`/`_update`, `forum_topic_get`/`_update`. Consistent naming lets Claude generalize the pattern after seeing one example, and lets the server instructions describe the rule once instead of per-addon.
- **Identification stays URL-first**, matching every other tool: `blog_post_update(url, metaTitle?, metaDescription?, metaKeywordList?, name?, copy?, tagList?, …)`. The tool resolves `url` through the same link-alias lookup the core tools already use — the admin never needs to know a numeric `BlogEntryID`, and the field list matches `page_update`'s "omit what you don't want to change" convention.
- **Field grouping**, borrowed directly from the existing admin forms: SEO (`metaTitle`, `metaDescription`, `metaKeywordList`) / Content (`name`, `copy`) / Info (`tagList`, `active`, `datePublished`, `allowComments`). A single `blog_post_update` tool covering all of them is simplest for a client to use correctly; split into separate tools later only if you want tighter permission scoping (e.g. an SEO-only bearer token).
- **Transport stays identical to today's pattern**: a new addon-owned remote method (`/content-api-blog-post-get`, `/content-api-blog-post-update`) registered exactly per `addon-remote-method-pattern.md`, deployed with the Blog collection rather than the base collection, called from the MCP server through the same `ContensiveClient` (bearer token forwarded per user, JSON envelope via `ContensiveClient.FormatResponse`).

### Wiring `page_get` to mention the right Tier-C tool

The core `PageGetRemoteMethod` has, and should keep, zero knowledge of Blog-specific concepts. Give widget addons an opt-in extensibility point instead: a small interface such as

```
IUrlAddressableContent {
    DynamicContentDescriptor? DescribeCurrentContent(CPBaseClass cp, string queryStringSuffix);
}
```

that a Page Widget addon implements only if it owns dynamic, URL-addressable child records. `DynamicContentDescriptor` carries `{ recordType, recordId, fields (current values), editTool, editArgs }`. When `PageGetRemoteMethod` walks the page's `addonList` to build the unified snapshot, it calls this method on any widget instance whose addon implements the interface and merges the result into that widget's block in the response, e.g.:

```json
{
  "instanceGuid": "abc-123",
  "designBlockTypeName": "Blog",
  "instanceSettings": {
    "fields": { "name": "Company Blog", "metaDescription": "..." },
    "editTool": "widget_instance_update"
  },
  "dynamicContent": {
    "recordType": "BlogEntry",
    "recordId": 6,
    "fields": {
      "name": "Getting the Most Out of Your Annual Wellness Visit",
      "metaTitle": "...", "metaDescription": "...", "metaKeywordList": "...", "tagList": "..."
    },
    "editTool": "blog_post_update",
    "editArgs": { "url": "/getting-the-most-out-of-your-annual-wellness-visit" }
  }
}
```

`BlogWidget` implements `DescribeCurrentContent` by loading the `BlogEntryModel` for the current `BlogEntryID` — the same lookup `BlogBodyRequestModel` already performs — and returning its fields plus the tool name/args above. Newsletter and Forum implement the same interface against their own record types later, with zero changes to the core Page/Widget system.

### Getting the tool itself into the running MCP server

`ContensiveMcpServer/Program.cs` wires tool classes at compile time today (`.WithTools<PageTools>()`, etc.). That's fine while the whole tool set lives in this repo, but `aoBlog` is a separate collection/assembly, and a hard-coded list won't scale once Newsletter and Forum also want tools without redeploying the core MCP server on every addon release. Two options, in order of preference:

1. **Plugin-assembly discovery.** At MCP server startup, scan a configured folder (or the list of collections installed on the target site) for companion `*.McpTools.dll` assemblies — small class libraries containing only `[McpServerToolType]` classes plus thin HTTP calls through the shared `ContensiveClient`, with no dependency on the addon's own runtime model classes — and register each with the SDK's `WithToolsFromAssembly()`. `aoBlog` ships `BlogMcpTools.dll` alongside its collection zip; the MCP server picks it up without a code change on its side.
2. **Fallback / stepping stone.** Keep new tool classes compiled straight into `ContensiveMcpServer` (a `BlogTools.cs` next to `PageTools.cs`, added to the `Program.cs` chain), exactly like today. Zero new infrastructure, but couples an MCP server deploy to every addon's tool changes — reasonable as a first step for Blog while the plugin loader is still being designed.

## 4. Summary

- Keep Tier A/B tools (`page_update`, `widget_instance_update`) exactly as they are.
- Add Tier C as its own concept — addon-owned dynamic content, addressed by URL, edited through addon-specific but consistently-named tools (`blog_post_update`, etc.), grouped into SEO/Content/Info fields the same way the existing admin forms already do.
- Don't rely on the connected AI having read this document — push the hierarchy into the protocol itself via server instructions (static, once) and a self-describing `page_get` response (dynamic, every call, names the exact tool + args to use next).
- Give widget addons one small opt-in interface (`IUrlAddressableContent`) so the core Page/Widget system never has to know what a "blog post" is, while `page_get` can still point straight at it.

# Contensive Help Pattern

This document describes the Contensive Help Files pattern — a markdown-based help system with AI-powered semantic search. Every Contensive addon collection can contribute help content by including markdown files in its build. The Help Pages addon indexes all content and provides a unified search and navigation experience at /help.

All new projects should follow this pattern for help content.

## Overview

The help system has three layers:

1. **Content** — Markdown files deployed to privateFiles/helpfiles/, organized by role-based subfolders and collection subfolders.
2. **Navigation** — A hierarchical left-side nav built from filenames. Topics and articles are encoded in the filename using period-delimited segments.
3. **AI Search** — Semantic search using OpenAI embeddings stored in SQL Server, with cosine similarity ranking and GPT-generated answers.

## Help File Storage

Help files are markdown (.md) files stored in the Contensive privateFiles system under the `helpfiles/` root folder. Files are organized into subfolders based on roles:

Content visibility is controlled by folder location:

| Folder | Audience | Visible To |
|---|---|---|
| `helpfiles/` | Public visitors | Everyone |
| `helpfiles/members/` | Authenticated users | Members, Administrators, Developers |
| `helpfiles/admin/` | Site admins | Administrators, Developers |
| `helpfiles/dev/` | Developers | Developers only |
| `helpfiles/fieldhelp/` | Database documentation | Administrators, Developers |

The addon collection installation process adds files to these folders from each collection. 

## File Naming Convention

The filename determines navigation placement. The `.md` extension is stripped. For files installed on a website, periods are used to delimit filenames into topics and article name. Files in the source repositories that will be installed use commas to delimit topic from article name. Names are delmited as follows, with the fillowing special file names:

| Installed Filename | Navigation Result |
|---|---|
| `default.md` | Content for the /help root page |
| `Article.md` | Top-level nav entry "Article" |
| `Topic.Article.md` | "Article" nested under "Topic" |
| `Topic.default.md` | Content shown when "Topic" is clicked |
| `Topic.SubTopic.Article.md` | "Article" under "SubTopic" under "Topic" |

During installation, the installation process prepends each file with the name of the collection and a comma, making the collection name the first topic for every help file from a collection. Help files added with the base system will not have the collection name prepended. 

Commas in filenames are converted to periods during collection installation (the OnInstall task handles this). In Git repositories, use commas in filenames to avoid confusion with the file extension period: `Topic,Article.md`.

## Adding Help to an Addon Collection

Every Contensive addon collection can include help files. Follow these steps:

### 1. Create a /helpfiles Folder in the Project

Add folders for each of the roles used in the project
- `helpfiles/` folder at the root of the addon project's Git repository. This folder contains articles available to anyone
- `helpfiles/dev/' folder for help only available devs
- `helpfiles/admin/' folder for help only available to admins and devs
- `helpfiles/members/' folder for help only available to admins, devs and anyone who an authenticate to the site
- `helpfiles/fieldhelp/' folder for help related to content metadata, available to admins and devs


### 2. Update the Build Script

Add a step to the build script that zips the help folder and places the zip in the collection folder:

```cmd
cd ..\helpfiles
del %collectionPath%HelpFiles.zip
"c:\program files\7-zip\7z.exe" a "%collectionPath%HelpFiles.zip"
cd ..\scripts
```

### 3. Add a Resource Element to the Collection XML

In the collection's XML file, add a Resource element that installs the help zip to the correct privateFiles location:

```xml
<Resource name="HelpFiles.zip" type="privatefiles" path="helpfiles" />
```

## AI Search Architecture

The search system uses Retrieval-Augmented Generation (RAG):

### Indexing Pipeline

When the Help Pages collection is installed (or an admin clicks Re-index Help), the indexer:

1. Reads all `.md` files from `helpfiles/` and `fieldhelp/` using `cp.PrivateFiles`.
2. Splits each file into chunks by heading structure (`#` and `##` headings start new chunks). Oversized chunks are split on paragraph boundaries at ~2000 characters.
3. Tags each chunk with a **role** based on its file path (`guest`, `member`, `administrator`, `developer`, or `fieldhelp`).
4. Calls the **OpenAI text-embedding-3-small** API to generate a 1536-dimension embedding vector for each chunk.
5. Stores the chunk text, heading, file path, role, and embedding in the `help_search_index` SQL Server table.

Indexing is a full rebuild — the table is cleared and all files are re-processed. This ensures consistency with current content.

### Search Pipeline

When a user submits a search query:

1. The query text is embedded using the same OpenAI model.
2. All chunks matching the user's allowed roles are loaded from SQL Server.
3. Cosine similarity is computed between the query embedding and each chunk embedding in C#.
4. The top 5 chunks are selected as context.
5. The context and query are sent to **OpenAI gpt-4o-mini** with a system prompt instructing it to answer from the documentation only.
6. The AI answer and source chunks are returned to the UI.

If the OpenAI API key is not configured or the API call fails, the system falls back to basic substring search.

### Database Table

The `help_search_index` table is created automatically on first indexing. Schema:

```sql
CREATE TABLE help_search_index (
    id              INT IDENTITY PRIMARY KEY,
    file_path       NVARCHAR(500),
    role            NVARCHAR(50),
    heading         NVARCHAR(500),
    chunk_text      NVARCHAR(MAX),
    embedding       NVARCHAR(MAX),  -- JSON array of floats
    file_modified   DATETIME,
    indexed_at      DATETIME DEFAULT GETDATE()
);
```

Embeddings are stored as JSON float arrays in NVARCHAR(MAX) and parsed to float[] in C# for cosine similarity calculation. This works with all SQL Server versions without requiring vector type support.

## Configuration

The help search system requires one site property:

| Site Property | Value | Purpose |
|---|---|---|
| `OpenAI API Key` | Your OpenAI API key | Used for embeddings and answer generation |

Set this property in the Contensive Control Panel under Settings > Site Properties. Without this key, search falls back to basic text matching.

## Project Structure

The Help Pages addon follows the standard Contensive addon project structure:

```
/collections/HelpPages/         — Collection build output
    Help Pages.xml               — Collection manifest
/help/                           — Help files installed to helpfiles/Help Pages/
/helpfiles/                      — Role-based help files
    default.md                   — Root help file, Documentation about the help system
    /dev/                        — Dev help content
    /admin/                      — Admin help content
    /member/                     — Member help content
    /fieldhelp/                  — Database table documentation (if applicable)
/scripts/
    build.cmd                    — Build and deployment script
/source/
    HelpPages.sln                — Visual Studio solution
    /HelpPages/                  — Main addon project (.NET Framework 4.8)
        /Addons/
            HelpRemote.cs        — Remote method addon, renders /help
            OnInstallTask.cs     — Post-install: rename files, re-index search
        /Controllers/
            HelpPagesViewController.cs — Search, upload, delete handlers
            HelpSearchIndexer.cs       — Chunking, embedding, SQL storage
            HelpSearchService.cs       — Query embedding, ranking, answer generation
            FileController.cs          — Recursive file listing
            httpController.cs          — URL normalization
        /Models/
            /Db/
                HelpSearchChunkModel.cs — POCO for indexed chunks
            /View/
                HelpPagesViewModel.cs   — Navigation tree builder, view properties
        Constants.cs               — Folder paths, site property names, GUIDs
    /Tests/                        — Unit tests (MSTest)
/ui/HelpPages/
    HelpPagesLayout.html          — Mustache template (Bootstrap 5 layout)
```

## Key Source Files

### HelpSearchIndexer.cs

Handles the indexing pipeline. Key methods:

- `IndexAllFiles(CPBaseClass cp)` — Entry point. Reads API key from site properties, ensures the SQL table exists, clears existing data, indexes all files from helpfiles/.
- `ChunkMarkdownByHeading(string content, string defaultHeading)` — Splits markdown by `#` and `##` headings. Each heading starts a new chunk. Returns a list of `MarkdownChunk` (heading + text).
- `DetermineRole(string filePath)` — Maps file path to role string based on folder name.
- `GetEmbedding(string apiKey, string text)` — Calls OpenAI text-embedding-3-small via HttpClient. Returns float[].
- `FloatArrayToJson / JsonToFloatArray` — Serialization helpers for storing embeddings in SQL.

### HelpSearchService.cs

Handles the search pipeline. Key methods:

- `Search(CPBaseClass cp, string query, int topK = 5)` — Full search pipeline: embed query, load role-filtered chunks, rank by cosine similarity, generate AI answer.
- `GetAllowedRoles(CPBaseClass cp)` — Returns list of role strings the current user can access based on `cp.User.IsAdmin`, `cp.User.IsDeveloper`, `cp.User.IsAuthenticated`.
- `CosineSimilarity(float[] a, float[] b)` — Dot product divided by magnitude product.
- `GenerateAnswer(string apiKey, string query, string context)` — Calls OpenAI gpt-4o-mini chat completions with documentation context.

### HelpPagesViewModel.cs

Builds the navigation tree from flat file listings. The filename convention (period-delimited segments) is parsed into a tree of `NodeClass` objects. Each node has a name, link, optional content path, and child nodes.

### HelpPagesViewController.cs

Processes form submissions:

- `helpSearchInput` — Calls HelpSearchService.Search(), falls back to substring search on failure.
- `helpReindexButton` — Calls HelpSearchIndexer.IndexAllFiles() (admin only).
- `helpUploadButton` — Saves uploaded markdown file to the selected collection folder.
- `helpDeleteButton` — Removes a help file and redirects.

## Writing Help Content for AI Search

For the best search results, follow these guidelines when writing help markdown:

- **Use headings to structure content.** The indexer splits on `#` and `##` headings, so each section should cover a focused topic.
- **Write in natural language.** Semantic search matches meaning, not exact keywords. Describe concepts clearly rather than using terse abbreviations.
- **Keep sections under ~2000 characters.** Longer sections are split on paragraph boundaries, which may break the context. Shorter, focused sections index more precisely.
- **Include the topic in each section.** Since chunks are retrieved independently, a section that says "To configure this feature..." without naming the feature will produce poor search results. Instead write "To configure email notifications...".
- **Use descriptive filenames.** The filename (without extension) is used as the default heading for chunks that appear before the first heading in the file.

## Maintenance

### Re-indexing

The search index must be rebuilt when help content changes. This happens automatically during collection installation. For manual changes (uploads, deletes), click the Re-index Help button on the /help page.

### Monitoring

Indexing and search errors are reported through `cp.Site.ErrorReport()`. Check the Contensive error log if search is not returning results or if indexing fails.

### Scaling

The current architecture loads all role-matching chunks into memory and computes cosine similarity in C#. This works well for sites with up to several thousand chunks (typical for Contensive installations). For significantly larger content libraries, the similarity search could be moved to SQL Server vector functions or an external vector database without changing the chunking or embedding logic.

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| Contensive.CPBaseClass | 25.10.4.2 | Contensive addon base class |
| Contensive.DbModels | 25.10.4.2 | Database model helpers |
| Markdig | 0.42.0 | Markdown to HTML rendering |
| System.Net.Http | (framework) | HttpClient for OpenAI API calls |
| System.Web.Extensions | (framework) | JavaScriptSerializer for JSON |

No external NuGet packages are required for the AI search feature — it uses framework classes and direct HTTP calls to the OpenAI API for .NET Framework 4.8 compatibility.

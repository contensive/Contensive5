# Filename Sanitization - Future Enhancements

This document outlines potential future enhancements to the Contensive filename sanitization system. These features are currently out of scope for the initial implementation but may be valuable additions in future releases.

---

## 1. Admin UI for Configuring Sanitization Level

**Description**: Provide a user-friendly admin interface for configuring filename sanitization settings without editing config files.

**Benefits**:
- Non-technical administrators can adjust security levels
- Real-time preview of how filenames would be sanitized
- Per-application configuration (different levels for different apps)
- Visual explanation of each security level

**Implementation Ideas**:
```
Admin Site → Settings → File Security
├── Sanitization Level: [Dropdown: Strict/Moderate/Permissive]
├── Preview Tool: "Test a filename" input box
│   └── Shows: "transcript[2024].pdf" → "transcript_2024_.pdf"
├── Help Text: Explains each level with examples
└── Apply to: [All Apps / Specific App]
```

**Technical Considerations**:
- Cache invalidation when settings change
- Audit log of configuration changes
- Role-based access (only admins can change)
- Migration tool if changing from Strict → Moderate (existing files unaffected)

---

## 2. Per-Content-Type Sanitization Rules

**Description**: Allow different sanitization rules for different content types or file upload contexts.

**Use Cases**:
- **Documents**: Strict mode (legal, compliance docs)
- **Images**: Moderate mode (allow international artist names)
- **Internal Files**: Permissive mode (trusted staff uploads)
- **Public Uploads**: Strict mode (untrusted users)

**Configuration Example**:
```json
{
  "filenameSanitization": {
    "default": "Moderate",
    "rules": [
      {
        "contentType": "Legal Documents",
        "level": "Strict",
        "reason": "Compliance requirement"
      },
      {
        "contentType": "Image Library",
        "level": "Moderate",
        "allowUnicode": true
      },
      {
        "uploadContext": "publicForm",
        "level": "Strict",
        "maxFilesize": 10485760
      }
    ]
  }
}
```

**Implementation**:
- Add `contentTypeId` or `uploadContext` parameter to sanitization methods
- Lookup rules from configuration
- Fall back to default level if no rule matches

---

## 3. Filename Collision Detection and Auto-Renaming

**Description**: Automatically handle filename collisions by appending numbers or timestamps instead of overwriting.

**Current Behavior**:
- Upload "document.pdf" → saves as "document.pdf"
- Upload "document.pdf" again → **overwrites** previous file

**Enhanced Behavior**:
```
Upload 1: "document.pdf" → "document.pdf"
Upload 2: "document.pdf" → "document-2.pdf"
Upload 3: "document.pdf" → "document-3.pdf"

OR with timestamp:
Upload 1: "document.pdf" → "document.pdf"
Upload 2: "document.pdf" → "document-20260825-143022.pdf"
```

**Configuration Options**:
- `filenameCollisionStrategy`: "overwrite" | "increment" | "timestamp" | "uuid"
- `preserveOriginalFilename`: Store original name in metadata

**Database Schema Addition**:
```sql
ALTER TABLE ccLibraryFiles ADD originalFilename VARCHAR(255);
ALTER TABLE ccLibraryFiles ADD sanitizedFilename VARCHAR(255);
ALTER TABLE ccLibraryFiles ADD collisionCount INT DEFAULT 0;
```

**Benefits**:
- Prevent accidental file overwrites
- Maintain file history
- Support versioning workflows

---

## 4. Audit Log of All Filename Changes

**Description**: Track every filename sanitization operation for security auditing and debugging.

**Logged Information**:
- Original filename (as uploaded by user)
- Sanitized filename (as saved to filesystem)
- Sanitization level applied
- User who uploaded
- Timestamp
- IP address
- Reason for changes (which characters were replaced)

**Example Log Entry**:
```json
{
  "timestamp": "2026-08-25T14:30:22Z",
  "userId": 123,
  "ipAddress": "192.168.1.100",
  "originalFilename": "transcript[2024].pdf",
  "sanitizedFilename": "transcript_2024_.pdf",
  "sanitizationLevel": "Moderate",
  "charactersReplaced": ["[", "]"],
  "contentType": "Course Transcripts",
  "fileSize": 1048576,
  "uploadContext": "evaluationStatusModal"
}
```

**Use Cases**:
- **Security Audits**: Track suspicious filename patterns
- **Debugging**: "Why was my filename changed?"
- **Compliance**: Demonstrate sanitization for regulatory requirements
- **Analytics**: Which characters are most commonly replaced?

**Storage Options**:
- Database table: `ccFilenameAuditLog`
- Log files: `/logs/filename-sanitization.log`
- External service: Send to SIEM or log aggregator

**Retention Policy**:
- Keep logs for 90 days (configurable)
- Archive older logs to cold storage
- Automatic cleanup job

---

## 5. Bulk Re-Sanitization Tool for Existing Files

**Description**: Administrative tool to re-sanitize filenames of existing files using updated rules.

**Use Cases**:
- Upgrading from old sanitization to new system
- Changing security level (Permissive → Strict)
- Fixing files uploaded before sanitization was implemented
- Compliance audit required ASCII-only filenames

**Tool Features**:
```
Admin Site → Tools → File Sanitization Tool

[Scan Files]
Found 1,247 files with non-compliant filenames:
  ├── 89 files with brackets: []
  ├── 34 files with Unicode characters
  ├── 12 files with reserved names (CON, PRN, etc.)
  └── 8 files exceeding length limit

[Preview Changes]
transcript[2024].pdf → transcript_2024_.pdf
成绩单.pdf → __.pdf (if Strict mode)
CON.txt → _CON.txt

[Options]
☐ Update database records
☐ Rename physical files
☐ Create redirects for old URLs
☐ Update all content references
☑ Create backup before proceeding
☐ Send notification to file owners

[Dry Run] [Execute]
```

**Safety Features**:
- **Dry run mode**: Preview changes without applying
- **Automatic backup**: Create restore point
- **Transaction support**: Roll back if errors occur
- **Batch processing**: Process in chunks to avoid timeouts
- **Progress tracking**: Real-time progress bar
- **Error handling**: Continue on errors, log failures

**Technical Implementation**:
```csharp
public class FileSanitizationTool {
    public ScanResult ScanFiles(string contentType = null);
    public List<RenamePreview> PreviewChanges(ScanResult scan, FilenameSanitizationLevel level);
    public RenameResult ExecuteRename(List<RenamePreview> previews, RenameOptions options);
    public void CreateBackup(List<string> filePaths);
    public void RollbackChanges(string backupId);
}
```

**Database Updates**:
- Update `filename` field in affected tables
- Update content records referencing old paths
- Create `ccFileRenameHistory` table for tracking

---

## 6. Machine Learning-Based Security Detection

**Description**: Use ML/AI to detect suspicious filename patterns beyond simple character blacklists.

**Detection Capabilities**:

### 6.1 Homograph Attack Detection
- Train model on known homograph pairs
- Detect visually similar but different Unicode characters
- Flag filenames mixing multiple scripts (Latin + Cyrillic)
- Calculate "visual similarity score" between filenames

**Example**:
```
"invoice.pdf" (legitimate)
"іnvoice.pdf" (Cyrillic і, score: 0.98 similar, FLAGGED)
```

### 6.2 Anomaly Detection
- Learn normal filename patterns per content type
- Flag unusual patterns, lengths, character distributions
- Detect obfuscation attempts

**Examples**:
```
Normal: "Q1-2024-Report.pdf"
Anomaly: "R3p0rt_Q1_2024.pdf" (unusual character substitution)

Normal: "transcript.pdf" (average length: 15 chars)
Anomaly: "a.pdf" (too short, suspicious)
Anomaly: "transcripttranscripttranscript...{200 chars}.pdf" (too long)
```

### 6.3 Malicious Extension Detection
- Detect double extensions: "report.pdf.exe"
- Detect extension spoofing with Unicode: "report.pdf[RTLO]exe"
- Flag unusual extension combinations

### 6.4 Content-Filename Mismatch Detection
- Analyze file content (MIME type detection)
- Compare to claimed extension
- Flag mismatches (executable disguised as PDF)

**Example**:
```
Filename: "document.pdf"
Actual content: PE32 executable (Windows .exe)
Risk Level: HIGH - Content mismatch detected
Action: Block upload, alert admin
```

**Implementation Approach**:
```csharp
public class MLFilenameSecurity {
    // Train model on safe/unsafe filename corpus
    public void TrainModel(List<FilenameExample> trainingData);

    // Analyze filename for threats
    public SecurityAnalysis AnalyzeFilename(string filename, byte[] fileContent);

    // Returns risk score 0.0 (safe) to 1.0 (dangerous)
    public double CalculateRiskScore(string filename);

    // Detect homograph attacks
    public HomographDetectionResult DetectHomographs(string filename);
}

public class SecurityAnalysis {
    public double RiskScore { get; set; }
    public List<string> Threats { get; set; }
    public bool ShouldBlock { get; set; }
    public string Recommendation { get; set; }
}
```

**Integration with Existing System**:
```csharp
// In sanitization pipeline:
var mlAnalysis = MLFilenameSecurity.AnalyzeFilename(uploadedFilename, fileBytes);

if (mlAnalysis.RiskScore > 0.8) {
    // High risk - block upload
    throw new SecurityException($"Suspicious filename detected: {mlAnalysis.Threats}");
}
else if (mlAnalysis.RiskScore > 0.5) {
    // Medium risk - sanitize aggressively
    level = FilenameSanitizationLevel.Strict;
    LogSecurityEvent("Suspicious filename pattern", filename, mlAnalysis);
}
```

**Training Data Sources**:
- VirusTotal filename database
- MITRE ATT&CK filename patterns
- Public malware sample repositories
- Historical upload patterns from production

**Privacy Considerations**:
- Model trained on anonymized data only
- No PII in training corpus
- On-premises model execution (no data sent to external services)

---

## 7. Per-User/Group Sanitization Rules

**Description**: Allow different sanitization levels based on user roles, groups, or trust levels.

**Use Cases**:
- **Staff/Admin**: Permissive mode (trusted users)
- **Authenticated Users**: Moderate mode
- **Public/Anonymous**: Strict mode
- **Specific Groups**: Custom rules (e.g., "International Partners" group allows Unicode)

**Configuration**:
```json
{
  "sanitizationRules": [
    {
      "applies_to": "group:Staff",
      "level": "Permissive",
      "reason": "Trusted internal users"
    },
    {
      "applies_to": "group:External Reviewers",
      "level": "Moderate",
      "reason": "Authenticated but external"
    },
    {
      "applies_to": "anonymous",
      "level": "Strict",
      "reason": "Untrusted public uploads"
    }
  ]
}
```

---

## 8. Filename Sanitization API Endpoint

**Description**: Expose sanitization functionality as a REST API for external integrations.

**Endpoints**:
```
POST /api/filename/sanitize
POST /api/filename/validate
POST /api/filename/analyze
```

**Example Request**:
```bash
curl -X POST https://cms.example.com/api/filename/sanitize \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "filename": "transcript[2024].pdf",
    "level": "Moderate"
  }'
```

**Example Response**:
```json
{
  "original": "transcript[2024].pdf",
  "sanitized": "transcript_2024_.pdf",
  "changes": [
    {"position": 10, "original": "[", "replaced": "_"},
    {"position": 15, "original": "]", "replaced": "_"}
  ],
  "safe": true,
  "level": "Moderate"
}
```

---

## 9. Internationalization (i18n) Support

**Description**: Provide localized error messages and explanations when filenames are sanitized.

**Languages**:
- English: "File '[' is not allowed. Renamed to '_'."
- Spanish: "El carácter '[' no está permitido. Renombrado a '_'."
- Chinese: "字符 '[' 不被允许。已重命名为 '_'。"
- Arabic: "الحرف '[' غير مسموح به. تمت إعادة التسمية إلى '_'."

**User Notifications**:
```
Upload Status: Success ✓

Your file "transcript[2024].pdf" was uploaded successfully.
Note: The filename was changed to "transcript_2024_.pdf" because
square brackets are not allowed for security reasons.

[Why?] [Learn More]
```

---

## 10. Filename Template System

**Description**: Allow administrators to define filename templates/patterns for specific content types.

**Template Examples**:
```
Course Transcripts: "{coursecode}_{year}_transcript.pdf"
Legal Documents:    "{year}-{month}-{type}-{sequential}.pdf"
Images:            "{category}/{date}/{uuid}.{ext}"
```

**Enforcement Options**:
- **Suggest**: Show template to user, allow override
- **Require**: Force compliance with template
- **Auto-generate**: Generate filename from metadata

---

## Implementation Priority

Based on value and complexity, suggested implementation order:

**High Priority** (Next 6 months):
1. Admin UI for Configuring Sanitization Level
2. Audit Log of All Filename Changes
3. Filename Collision Detection

**Medium Priority** (6-12 months):
4. Per-Content-Type Sanitization Rules
5. Bulk Re-Sanitization Tool
6. Filename Sanitization API

**Low Priority** (Future):
7. Machine Learning-Based Detection (requires significant ML expertise)
8. Per-User/Group Rules (adds complexity)
9. i18n Support (nice-to-have)
10. Filename Template System (specialized use case)

---

## Contributing

If you'd like to implement any of these features, please:
1. Open a GitHub issue to discuss the approach
2. Reference this document in your proposal
3. Consider backward compatibility
4. Include comprehensive tests
5. Update documentation

---

## References

- Main Implementation: See current filename sanitization in `FileController.cs`
- Configuration: `ServerConfigModel.FilenameSanitizationLevelEnum`
- Security Best Practices: OWASP File Upload Guidelines
- Unicode Security: Unicode Technical Report #36 (UTR36)

---

*Last Updated: 2026-08-25*
*Document Version: 1.0*

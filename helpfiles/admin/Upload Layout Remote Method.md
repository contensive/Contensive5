The Upload Layout remote method is a built-in endpoint that lets you upload HTML layout files to a non-production Contensive site using an HTTP POST request. The uploaded file is processed and saved as a layout record.

This method is typically used during development to automate layout deployment from a design tool or build process.

### Requirements

- The site must be a **non-production** environment. This method is disabled on production sites.
- The user must have **Admin** access.
- Authentication is required. You can authenticate with a browser session or by including your user GUID in the POST request.

### Endpoint

POST to the site URL with the addon route:

```
POST http://yoursite.com/uploadLayout
```

### Request Parameters

| Name | Type | Required | Description |
|------|------|----------|-------------|
| htmlFile | file | Yes | The HTML file to upload. Accepted extensions: `.html`, `.htm`, `.zip`. Maximum size: 100MB. |
| userGuid | string | No | Your user GUID for authentication when not using a browser session. Found on your user record in the admin site. |

### Example Using curl

```
curl -X POST http://yoursite.com/uploadLayout -F "htmlFile=@myLayout.html" -F "userGuid={1234-1234-1234-1234}"
```

### Response

The response is JSON with the following structure:

**Success:**
```json
{
  "success": true,
  "message": "File processed successfully",
  "data": {
    "messages": ["list of processing messages"]
  }
}
```

**Error:**
```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

### Error Messages

- **"This remote method is not available on production sites."** - The endpoint is disabled on production environments.
- **"Authentication required."** - Include a valid userGuid in the POST request or authenticate with a browser session.
- **"Admin access required."** - Your user account must have Admin access.
- **"No file uploaded."** - Attach the file to the POST request using the field name `htmlFile`.
- **"Only HTML files are allowed."** - The uploaded file must have an `.html`, `.htm`, or `.zip` extension.
- **"File size exceeds 100MB limit."** - Reduce the file size before uploading.
- **"Too many uploads."** - Rate limit reached. Wait a few minutes before trying again.

### Rate Limiting

Uploads are limited to 10 per user within a 5-minute window. Each upload is logged in the activity log.

### How the File is Processed

The uploaded HTML file is processed using the layout import system (`processImportFile`). The import reads metadata embedded in the HTML to determine the target layout record. For details on how to format HTML files for import, see the Html Import help file.

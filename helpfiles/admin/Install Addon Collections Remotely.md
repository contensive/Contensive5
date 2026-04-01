## What Does This Do?

The Install Collection remote method lets you upload and install an add-on collection file (.zip) to your site without logging into the admin site. You send the file to the site using a command-line tool called **curl**, and the site installs it automatically.

This is most useful when you have a build script that produces a collection file and you want to deploy it to a development or staging site in one step.

---

## Requirements

- The site must be a **non-production** environment. This method is disabled on production sites to prevent accidental deployments. The `productionEnvironment` setting in the server's `config.json` file controls this.
- You need a **bearer token** for authentication. See the *Create Authentication Bearer Token* help file for instructions on creating one.
- The user account tied to the bearer token must have **Admin** access.
- The uploaded file must be a **.zip** collection file.

---

## Endpoint

Send a POST request to your site using the `/installCollection` route:

```
POST https://yoursite.com/installCollection
```

Replace `yoursite.com` with your site's address.

---

## How to Use curl

Open a terminal or command prompt and run:

```
curl -X POST https://yoursite.com/installCollection \
  -H "Authorization: Bearer <your token here>" \
  -F "collectionFile=@myCollection.zip"
```

- Replace `https://yoursite.com` with your site's address.
- Replace `<your token here>` with the bearer token you copied from the admin site.
- Replace `myCollection.zip` with the path to your collection file.

---

## Using It in a Build Script

You can add the curl command to a build script so the collection is deployed automatically after it is built. Below are examples for common script formats.

**Windows batch file (.cmd):**

```bat
@echo off
set SITE_URL=https://yoursite.com/installCollection
set TOKEN=your-bearer-token-here
set COLLECTION=build\output\myCollection.zip

curl -X POST %SITE_URL% -H "Authorization: Bearer %TOKEN%" -F "collectionFile=@%COLLECTION%"
```

**Bash script (.sh):**

```bash
#!/bin/bash
SITE_URL="https://yoursite.com/installCollection"
TOKEN="your-bearer-token-here"
COLLECTION="build/output/myCollection.zip"

curl -X POST "$SITE_URL" \
  -H "Authorization: Bearer $TOKEN" \
  -F "collectionFile=@$COLLECTION"
```

> **Tip:** Store your bearer token in an environment variable or a secrets manager rather than pasting it directly into your script. This keeps the token out of source control.

---

## Response

The site responds with JSON indicating whether the install succeeded.

**Success:**

```json
{
  "success": true,
  "message": "Collection installed successfully from myCollection.zip.",
  "data": null
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

---

## Troubleshooting

**"This method is not available on production sites."** — The server is configured as a production environment. This method only works on development and staging sites. Check the `productionEnvironment` setting in the server's `config.json` file.

**"Authentication required."** — The bearer token is missing or invalid. Make sure the Authorization header is formatted as `Authorization: Bearer <token>` with no extra spaces or line breaks. See the *Create Authentication Bearer Token* help file for details.

**"Admin access required."** — The user account tied to the bearer token does not have Admin access. Contact your site administrator.

**"No file uploaded."** — The collection file was not included in the request. Make sure you are using `-F "collectionFile=@yourfile.zip"` in your curl command and that the file path is correct.

**"Only .zip collection files are accepted."** — The uploaded file does not have a `.zip` extension. Rename or repackage the file as a .zip before uploading.

**"Token has expired"** — The bearer token has passed its expiration date. Create a new token from the user record in the admin site.

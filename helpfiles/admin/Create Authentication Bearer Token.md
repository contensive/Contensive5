## What Is a Bearer Token?

A bearer token is a secure pass-code that lets an outside program — such as an automated script or a deployment tool — communicate with your site without needing to log in through a browser. Think of it like a temporary access key that you hand to a trusted tool.

Once created, the token is copied into the tool's settings one time. From that point on, the tool can send requests to the site and the site will recognize it.

---

## Who Can Create a Bearer Token?

Only users with **Admin** access can create bearer tokens, and the token is tied to the specific user account it was created on. The token carries that user's permissions, so if you create a token on your account, anything done using that token will be done with your level of access.

---

## How to Create a Bearer Token

**Step 1 — Open the user record**

In the admin site, navigate to **People** and open the user record you want to create the token for. This is typically your own account.

**Step 2 — Click "Create Bearer Token"**

At the top of the edit page, click the **Create Bearer Token** button in the button bar.

**Step 3 — Copy the token**

A popup will appear displaying the token and the date it expires. The token is a long string of characters.

Select all of the text in the popup and copy it. This is the only time the full token will be shown — it cannot be retrieved again. If you lose it, simply create a new one by clicking the button again.

> **Note:** Creating a new token replaces the previous one. Any tool using the old token will stop working and will need to be updated with the new token.

**Step 4 — Use the token in your tool**

Paste the token into your tool or script wherever it asks for authentication. The token is added to the request as an **Authorization** header in this format:

```
Authorization: Bearer <paste your token here>
```

---

## Token Expiration

Bearer tokens expire **one year** from the date they were created. The expiration date is shown in the popup when the token is created.

When a token expires, the tool using it will receive an authentication error. To restore access, log into the admin site, open the same user record, and click **Create Bearer Token** again to generate a fresh token.

---

## Example: Installing a Collection File

One common use for a bearer token is deploying a collection file to the site from a build process. After copying your token, the command looks like this:

```
curl -X POST https://yoursite.com/installCollection ^
  -H "Authorization: Bearer <your token here>" ^
  -F "collectionFile=@myCollection.zip"
```

Replace `https://yoursite.com` with your site's address and `myCollection.zip` with the name of the file you want to install.

---

## Troubleshooting

**"Authentication required"** — The token is missing or was not included correctly in the request. Confirm the header is formatted as `Authorization: Bearer <token>` with no extra spaces or line breaks.

**"Token has expired"** — The token is more than one year old. Create a new token from the user record and update your tool.

**"Admin access required"** — The user account the token belongs to does not have Admin access. Contact your site administrator.

**The tool was working and stopped** — A new token may have been created on the same user account, which replaces the previous one. Check with your administrator and generate a new token if needed.

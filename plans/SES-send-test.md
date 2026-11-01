# SES Send Test - Site Status Diagnostic

## Overview

Add an SES send diagnostic to the aoStatus addon (`SESDiagnosticController`) that verifies each site's SES configuration and permissions are correct by sending to the AWS SES mailbox simulator.

## How It Works

AWS SES provides simulator email addresses that produce deterministic outcomes without delivering real mail. Sending to `success@simulator.amazonses.com` exercises the full SES send path and validates configuration without needing a real destination mailbox.

### What the test validates

- The From address (or its domain) is verified in SES
- IAM credentials have `ses:SendEmail` / `ses:SendRawEmail` permission
- The account is out of sandbox mode (or the simulator address is verified, which it is by default)
- The sending quota hasn't been exceeded

### What the test does NOT validate

- DNS records (SPF, DKIM, DMARC) for deliverability — no real receiving server evaluates these
- Actual email content rendering
- Network-level issues between the server and SES endpoints

## SES Simulator Addresses (Reference)

| Address | Result |
|---|---|
| `success@simulator.amazonses.com` | Accepted, simulates successful delivery |
| `bounce@simulator.amazonses.com` | Generates a hard bounce notification |
| `complaint@simulator.amazonses.com` | Generates a spam complaint notification |
| `suppressionlist@simulator.amazonses.com` | Simulates address on the suppression list |
| `ooto@simulator.amazonses.com` | Generates an out-of-office auto-reply |

Only the `success@` address is needed for this diagnostic.

## Implementation Plan

1. Add a new `SESDiagnosticController.cs` in `addons/aoStatus/server/Status/`, following the pattern of existing diagnostic controllers (e.g., `ReliabilityDiagnosticsController`, `SecurityDiagnosticsController`)
2. The controller sends a test email using the site's configured email system with:
   - **To:** `success@simulator.amazonses.com`
   - **From:** *(see open issue below)*
   - **Subject:** SES diagnostic test identifier (include site name and timestamp)
3. If the send completes without exception, the diagnostic reports success
4. If the send throws (e.g., `MessageRejected`, `AccessDenied`), the diagnostic captures the error and reports failure with the SES error message

## Open Issue: From Address

The From address used for the test must be a verified identity in SES (either the specific address or its domain). The site's current from-address configuration needs to be resolved before this can be implemented. This is a broader issue affecting production email sending, not just this diagnostic.

## No Additional AWS Permissions Required

This test uses the site's existing SES send permissions. No new IAM policies, S3 buckets, or SES receive rules are needed.

# ProcessorTests Failures

**Last run:** 2026-10-11 — 384 total, 1 failed, 383 succeeded, 0 skipped

Previously 49 failures across 6 groups. After fixes: 48 tests now passing, 1 remains.

## Fixed (previously failing)

| Group | Tests Fixed | Resolution |
|-------|------------|------------|
| FileController — remote filesystem not configured | 21 → 0 | All 26 FileController tests now pass (confirmed with filtered run) |
| EmailController — assertion mismatches | 4 → 0 | Added `EmailQueueModel` cleanup to `testGroupEmail` to clear leftover queued emails |
| Addon/Script execution returning empty | 3 → 0 | All addon/script tests now pass |
| ImageController — resize/crop failures | 17 → 0 | All 18 image tests now pass |
| LayoutController — updateLayout returning 0 | 3 → 0 | Tests incorrectly asserted `contentControlId` equals the Layouts content ID; root content records inserted by table name correctly have `contentControlId=0` (see `patterns/content-control-id-pattern.md`) |

## Remaining Failures (1 test)

### 1. IIS / iisutil.exe not found (1 test)

**Root cause:** `iisutil.dll` missing from test bin output directory.

| Test | Error | File |
|------|-------|------|
| `verifyAppPool_test1` | iisutil exited with code -2147450726: app does not exist | `source/Processor/Controllers/WebServerController.cs:1238` |

# Status Speedometer UI Specification

**For:** ContensiveSite repository (siteMonitor feature)
**Date:** 2026-04-11
**Version:** 1.0

## Overview

The Contensive5 `/status` endpoint now supports a `format=json` parameter that returns real-time performance metrics alongside diagnostic results. This document specifies how the siteMonitor in the ContensiveSite repo should consume this data and render a speedometer-style UI.

---

## 1. Endpoint

```
GET https://{domain}/?method=status&format=json
```

**Response:** `application/json`

```json
{
  "status": "ok",
  "message": "ok, all tests passed.",
  "metrics": {
    "avgResponseTimeMs": 145,
    "avgResponseTime5MinMs": 230,
    "hitCount": 12345,
    "hitCount5Min": 87,
    "uptimeMinutes": 4320
  },
  "diagnostics": "ok, all tests passed.\nOK, Server Diagnostic ..."
}
```

### Field Reference

| Field | Type | Description |
|-------|------|-------------|
| `status` | string | `"ok"` when all diagnostics pass, `"error"` when any fail |
| `message` | string | Human-readable summary of diagnostic results |
| `metrics.avgResponseTimeMs` | int | All-time average response time in milliseconds (since process start) |
| `metrics.avgResponseTime5MinMs` | int | Average response time over the last 5 minutes |
| `metrics.hitCount` | long | Total web requests since process start |
| `metrics.hitCount5Min` | long | Web requests in the last 5 minutes |
| `metrics.uptimeMinutes` | int | Minutes since the web worker process started |
| `diagnostics` | string | Full diagnostic output text (all addon results) |

### Edge Cases

- **Fresh process start:** `hitCount` and `hitCount5Min` will be 0, averages will be 0, `uptimeMinutes` will be low. The UI should display a "warming up" state rather than alarming on zero values.
- **No traffic:** If `hitCount5Min === 0` and `uptimeMinutes > 5`, the site may be receiving no traffic.
- **Error status:** When `status` is `"error"`, the `message` field starts with `"ERROR"` and contains the failing diagnostic name.

### Backward Compatibility

The plain `/?method=status` (without `&format=json`) still returns the original plain-text response. Only use the `format=json` parameter for the speedometer UI.

---

## 2. Speedometer Gauge UI

### Design

Use an **SVG semicircular arc** gauge. No external charting library required -- a simple SVG arc with a rotating needle is lightweight and avoids dependencies.

### Layout

```
  +-------------------------------+
  |     Response Time (5 min)     |
  |                               |
  |      [===GAUGE ARC===]        |
  |         /   |   \             |
  |        /  needle  \           |
  |           230 ms              |
  |                               |
  |  All-time avg: 145 ms         |
  |  Hits (5 min): 87             |
  |  Total hits: 12,345           |
  |  Uptime: 3 days               |
  +-------------------------------+
```

### Gauge Color Ranges

The arc should be divided into colored segments representing performance tiers:

| Range (ms) | Color | Hex | Label |
|------------|-------|-----|-------|
| 0 - 200 | Green | `#22c55e` | Fast |
| 200 - 500 | Yellow | `#eab308` | Normal |
| 500 - 1000 | Orange | `#f97316` | Slow |
| 1000+ | Red | `#ef4444` | Critical |

### Needle

- The needle points to the **`avgResponseTime5MinMs`** value on the gauge scale
- The gauge scale should range from 0 to 2000ms (configurable)
- Animate the needle smoothly when the value changes between polls

### Text Display

Below the gauge, display:
- **Current 5-min average** (large, prominent): the needle value in ms
- **All-time average**: `avgResponseTimeMs` value for baseline comparison
- **Hit counts**: `hitCount5Min` and `hitCount` for context
- **Uptime**: Convert `uptimeMinutes` to a human-readable format (e.g., "3 days 12 hours")

---

## 3. Polling

- Poll the endpoint every **30 seconds**
- Use `fetch()` with `setTimeout` for the next poll (not `setInterval`) to prevent overlapping requests if a response is slow
- On network error or timeout, display the gauge in a **"disconnected" state** (gray gauge, last known value shown with a "disconnected" label)
- Retry on next poll interval -- do not back off aggressively since the monitor is the observer

### Example Polling Pattern

```javascript
async function pollStatus() {
    try {
        const response = await fetch(endpoint);
        const data = await response.json();
        updateGauge(data);
    } catch (err) {
        showDisconnected();
    }
    setTimeout(pollStatus, pollIntervalMs);
}
```

---

## 4. Alerting Rules

### Visual Alerts (on the gauge widget)

| Condition | Alert | Visual |
|-----------|-------|--------|
| `status !== "ok"` | Diagnostics failed | Red banner overlay: "DIAGNOSTICS FAILED: {message}" |
| `avgResponseTime5MinMs > 1000` | Critical response time | Pulse gauge border red |
| `avgResponseTime5MinMs > avgResponseTimeMs * 1.5` | Above baseline | Yellow warning: "Response time above baseline" |
| `hitCount5Min === 0 && uptimeMinutes > 5` | No traffic | Warning: "No traffic in last 5 minutes" |

### Alert Priority

When multiple alerts are active, display in priority order (highest first):
1. Diagnostics failed (red)
2. Critical response time (red)
3. Above baseline (yellow)
4. No traffic (yellow)

### Optional: Browser Notifications

If the page has notification permission and `status` transitions from `"ok"` to `"error"`, fire a browser `Notification`:
```javascript
if (previousStatus === "ok" && data.status !== "ok") {
    new Notification("Site Alert", { body: data.message });
}
```

---

## 5. Implementation Guidelines

### Standalone Module

The speedometer should be a **standalone JavaScript module** (no framework dependency) that can be embedded in any page:

```html
<div id="contensive-status-gauge" data-endpoint="/?method=status&format=json"></div>
<script src="/contensive/status-gauge.js"></script>
<script>
  ContensiveStatusGauge.init({
    elementId: "contensive-status-gauge",
    endpoint: "/?method=status&format=json",
    pollIntervalMs: 30000,
    thresholds: {
      fast: 200,
      normal: 500,
      slow: 1000
    },
    gaugeMax: 2000
  });
</script>
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `elementId` | string | required | DOM element ID to render into |
| `endpoint` | string | required | Full URL to the status endpoint |
| `pollIntervalMs` | number | `30000` | Polling interval in milliseconds |
| `thresholds.fast` | number | `200` | Upper bound of "fast" range (ms) |
| `thresholds.normal` | number | `500` | Upper bound of "normal" range (ms) |
| `thresholds.slow` | number | `1000` | Upper bound of "slow" range (ms) |
| `gaugeMax` | number | `2000` | Maximum value on gauge scale (ms) |

### Multi-Site Dashboard

The siteMonitor typically watches multiple sites. Each site should get its own gauge widget instance. The dashboard layout should show all gauges in a grid, with failing/slow sites sorted to the top.

---

## 6. Metrics Behavior Notes

- **Metrics reset on process recycle.** When IIS recycles the worker process, all in-memory metrics reset to zero. The `uptimeMinutes` field tells you how long the process has been running -- a low value with low hit counts means the data is still warming up.
- **Only web requests are measured.** Background tasks and scheduled jobs do not contribute to the response time metrics.
- **The 5-minute window is sliding.** It always reflects the most recent 5 minutes, updated on every request.
- **The all-time average is cumulative.** It's the mean of all requests since process start. Over time this becomes very stable and serves as the baseline for comparison.

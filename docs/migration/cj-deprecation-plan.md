# CJ Object Deprecation Plan

## Status: In Progress (Started 2026-11-20)

## Overview
The `cj` JavaScript object is being deprecated in favor of vanilla JavaScript. This document tracks the migration strategy and progress.

## Strategy: Option A - Gradual Deprecation with Backward Compatibility

### Goals
1. Migrate all platform code to vanilla JavaScript
2. Keep `cj` object functional with deprecation warnings
3. Allow 6-12 month transition period for external addons
4. Eventually remove `cj` entirely

### Modern JavaScript Pattern (Inline in Layouts)

All new layouts should use inline JavaScript following the pattern in `DashboardLayout.html`:
- No external JavaScript dependencies for simple interactions
- Vanilla JavaScript (querySelector, fetch, addEventListener)
- Self-contained `<script>` tags at end of layout HTML

## Migration Mapping

### DOM Manipulation
```javascript
// OLD
cj.hide('myDiv');
cj.show('myDiv');
cj.invisible('myDiv');
cj.visible('myDiv');

// NEW (Vanilla JS)
document.getElementById('myDiv').style.display = 'none';
document.getElementById('myDiv').style.display = 'block';
document.getElementById('myDiv').style.visibility = 'hidden';
document.getElementById('myDiv').style.visibility = 'visible';

// BEST (Bootstrap classes if available)
document.getElementById('myDiv').classList.add('d-none');
document.getElementById('myDiv').classList.remove('d-none');
```

### AJAX Calls
```javascript
// OLD
cj.ajax.addon('MyAddon', 'param=value', 'formId', 'destId');

// NEW (Vanilla JS with fetch)
const formData = new FormData(document.getElementById('formId'));
fetch('/?remotemethodaddon=MyAddon&param=value', {
  method: 'POST',
  body: formData
})
.then(response => response.text())
.then(html => {
  document.getElementById('destId').innerHTML = html;
})
.catch(error => console.error('Error:', error));
```

### Event Handling
```javascript
// OLD
cj.addLoadEvent(myFunction);
cj.addListener(element, 'click', handler);

// NEW (Vanilla JS)
document.addEventListener('DOMContentLoaded', myFunction);
element.addEventListener('click', handler);
```

### HTML Encoding
```javascript
// OLD
cj.encodeHTML(str);

// NEW (Vanilla JS)
function encodeHTML(str) {
  const div = document.createElement('div');
  div.textContent = str;
  return div.innerHTML;
}
```

### Dynamic Script/Style Loading
```javascript
// OLD
cj.addHeadScriptLink(url);
cj.addHeadStyleLink(url);

// NEW (Vanilla JS)
const script = document.createElement('script');
script.src = url;
script.type = 'text/javascript';
document.head.appendChild(script);

const link = document.createElement('link');
link.rel = 'stylesheet';
link.href = url;
document.head.appendChild(link);
```

## Implementation Phases

### Phase 1: Add Deprecation Warnings ✓ (Target: Week 1)
- [x] Document migration plan
- [ ] Add console.warn() to all cj methods in baseAssets.js
- [ ] Add console.warn() to all cj methods in adminUI.js
- [ ] Update CHANGELOG.md with deprecation notice

### Phase 2: Migrate Platform Layouts (Target: Week 2)
- [ ] `ui/layoutFiles/BaseAssets/AdminUIPageWithNavLayout.html` (5 cj calls)
  - Replace cjFrameSubmitForm
  - Replace cjFrameUpdate
  - Replace cj.frame initialization
- [ ] `source/Processor/Resources/layoutBuilderJavaScript.js` (5 cj calls)

### Phase 3: Migrate C# Generated JavaScript (Target: Week 3)
- [ ] `source/Processor/Controllers/PageManagerController.cs`
- [ ] `source/Processor/Controllers/HtmlController.cs`
- [ ] `source/Processor/Addons/AdminSite/Views/EditView.cs`
- [ ] `source/Processor/Addons/AdminSite/Views/RemoteMethods/getFieldEditorPreference.cs`

### Phase 4: Update Documentation (Target: Week 3)
- [ ] Create `patterns/javascript-patterns.md`
- [ ] Update existing patterns with vanilla JS examples
- [ ] Add migration examples to README

### Phase 5: External Addon Audit (Target: Week 4)
- [ ] Search aoEcommerce for cj usage
- [ ] Search aoCrm for cj usage
- [ ] Search aoSecurity for cj usage
- [ ] Search aoPortal for cj usage
- [ ] Create addon developer migration guide
- [ ] Notify addon developers via GitHub issues

### Phase 6: Deprecation Period (6-12 months)
- Keep cj functional with warnings
- Monitor console logs for usage
- Provide support for addon developers

### Phase 7: Final Removal (After deprecation period)
- Remove cj object from baseAssets.js
- Remove baseAssets.js if no longer needed
- Remove legacy code

## Files Using cj Object (81 total occurrences)

### Core Platform Files
- `ui/wwwFiles/BaseAssets/baseAssets.js` - 21 occurrences (defines cj)
- `ui/cdnFiles/BaseAssets/baseAssets.js` - 21 occurrences (duplicate)
- `cdn/source/adminUI.js` - 22 occurrences (defines cj)
- `ui/wwwFiles/BaseAssets/adminUI-legacy.js` - 1 occurrence
- `ui/cdnFiles/BaseAssets/adminUI-legacy.js` - 1 occurrence

### Layout Files
- `ui/layoutFiles/BaseAssets/AdminUIPageWithNavLayout.html` - 5 occurrences

### C# Source Files
- `source/Processor/Controllers/PageManagerController.cs` - 1 occurrence
- `source/Processor/Controllers/HtmlController.cs` - 1 occurrence
- `source/Processor/Addons/AdminSite/Views/EditView.cs` - 1 occurrence
- `source/Processor/Addons/AdminSite/Views/RemoteMethods/getFieldEditorPreference.cs` - 1 occurrence
- `source/Processor/Resources/layoutBuilderJavaScript.js` - 5 occurrences
- `source/Processor/Properties/Resources.Designer.cs` - 1 occurrence (generated)

## Communication Plan

### Internal Team
- [x] Document deprecation plan
- [ ] Team review and approval
- [ ] Update sprint planning

### External Developers
- [ ] GitHub announcement (issue + discussion)
- [ ] Update documentation website
- [ ] Release notes in next version
- [ ] Email to known addon developers

## Rollback Plan

If issues arise:
1. Git branch allows easy rollback
2. Console warnings can be removed without breaking functionality
3. Gradual migration means partial completion is safe

## Success Metrics

- Zero cj calls in platform code
- All layouts use vanilla JavaScript
- Zero console warnings in development
- External addons migrated or using legacy shim
- Clean removal of cj after deprecation period

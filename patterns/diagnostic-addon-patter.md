
# Diagnostic Addon Pattern

## Overview
A diagnostic addon is an addon configured to be run by the system when the /status method is executed. 

The /status method is typically used to monitor a website or application. If successful it should return
- the first two charcters "ok"
- it should NOT contain the characters "error"

## Architecture  
A process addon is constructed the same as any addon, but in the addon record it the diagnostic checkbox is checked.

If a disgnostic addon returns the first two characters "ok", the test is assume to have passed. Any other result will flag an error to the status process and cause that method to fail.
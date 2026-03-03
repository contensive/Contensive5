
# Process Addon Pattern

## Overview
A process addon is an addon configured to run in the background without a UI on a periodic or on-demand schedule.

## Architecture  
A process addon is constructed the same as any addon, but in the addon record it can be run peridocially by setting Minutes between execute in the process tab, and on demand by checking the Execute Once Now checkbox, or calling the cp.Addon.ExecuteAsProcess() method.

The return output of a process addon is ignored.
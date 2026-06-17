
# Task Addon Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview
A task addon is an addon configured to run in the background without a UI on a periodic or on-demand schedule.

## Architecture
A task addon is constructed the same as any addon, but in the addon record it can be run periodically by setting Minutes between execute in the process tab, and on demand by checking the Execute Once Now checkbox, or calling the cp.Addon.ExecuteAsProcess() method.

The return output of a task addon is ignored.

# Remote Method Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

A Remote Method is an addon that is executed when an HTTP request matches the addon's name as a path segment. The addon can update application data and its return value becomes the HTTP response body.

## URL Format

Remote methods are invoked as path segments, not query string parameters. The addon name becomes a route in the URL path.

```
CORRECT:   /remoteMethodName
WRONG:     ?remotemethod=remoteMethodName
```

For example, an addon named `searchResultsRemote` is called at:
```
GET  {baseURL}/searchResultsRemote
POST {baseURL}/searchResultsRemote
```

State-changing operations (create, update, delete) must use POST — see [POST for State-Changing Remote Methods](security-best-practices.md#post-for-state-changing-remote-methods).

## Architecture

## The Dotnet Component
- The addon references a Dotnet+class that includes an Execute method that returns the rendered layout

## Steps to Scaffold a new Remote Method Addon

1) follow the pattern established in the example RemoteMethodExample solution
2) Determine the Addon Collection in which this addon will be created. If one does not exists, first scaffold  a new Addon Collection repository with collection and new dotnet solution.
3) Determine The project in the dotnet solution. 
4) In the Addon Collection create a new addon node
    - Create a new Addon node
    - add a name that will be the endpoint that executes the addon. Best practice is to use only lowercase characters
    - ceate a new guid for the addon
    - create a dotnetclass node and use the dotnet projects namespace to the Addons folder
    - mark the node remotemethod true to allow this addon to be executed from an endpoint
5) In the dotnet project, the Addons folder create a new class that inherits CPBaseClass. 


# Remote Method Pattern

## Overview
[Detailed explanation, history, why this pattern exists]
A Remote Method is an addon that can is executed when an endpoint is hit that matches the addon's name. The addon can update the application data and its return is returned as the response to the endpoint.

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

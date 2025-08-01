# Use the official ASP.NET image with .NET Framework 4.7.2
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.7.2




# Install Web Deploy
ADD https://download.microsoft.com/download/0/1/D/01DC28EA-638C-4A22-A57B-4CEF97755C6C/WebDeploy_amd64_en-US.msi C:/WebDeploy.msi
RUN powershell -Command Start-Process msiexec.exe -ArgumentList '/i', 'C:\\WebDeploy.msi', '/quiet', '/norestart' -Wait


# Remove default IIS site content
RUN powershell -NoProfile -Command Remove-Item -Recurse C:\inetpub\wwwroot\*

# copy the contensive default app to the container
COPY MyApp.zip C:/app/MyApp.zip
RUN powershell -Command "C:\\Program Files\\IIS\\Microsoft Web Deploy V3\\msdeploy.exe -source:package='C:\\app\\MyApp.zip' -dest:auto,computerName='localhost' -verb:sync"

EXPOSE 80
# Publishing

To build in preparation to publish to Nuget:

```bash
dotnet pack DurableFunctionSupport/DurableFunctionSupport.csproj  -p:Version=<version goes here> --configuration Release
```

To publish to Nuget:

```bash
dotnet nuget push DurableFunctionSupport/bin/Release/Hids.DurableFunctionSupport.<version goes here>.nupkg --api-key <API key goes here> --source https://api.nuget.org/v3/index.json
```

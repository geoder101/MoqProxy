# NOTES

## Create nuget packages

```bash
rm out/nupkgs/*
```

```bash
dotnet pack --output out/nupkgs src/MoqProxy.sln
```

## Publish nuget packages

```bash
dotnet nuget push "out/nupkgs/geoder101.MoqProxy.DependencyInjection.Microsoft.*.nupkg" -k <API_KEY> -s https://api.nuget.org/v3/index.json
```

```bash
dotnet nuget push "out/nupkgs/geoder101.MoqProxy.1.*.nupkg" -k <API_KEY> -s https://api.nuget.org/v3/index.json
```

# Deployment Guide

This project consists of an Angular frontend (`UI/`) and an ASP.NET Core 8 API (`TeamConnect.Api/`). Both are deployed together to a single Azure App Service via Visual Studio's Publish feature.

The API's `.csproj` contains a target (`IncludeAngularDist`) that automatically copies the Angular build output from `UI/dist/team-building/browser/` into `wwwroot/` during the publish step. **The Angular app must be built before publishing**, otherwise the old or no frontend files will be deployed.

## Target Environment

- **Platform**: Azure App Service (Linux, `linux-x64`)
- **Deploy method**: Zip Deploy
- **App Service**: `teamconnect-01.azurewebsites.net`
- **Resource group**: `my-rg`

## Deployment Steps

### 1. Build the Angular app

From the `UI/` directory:

```bash
npm run build
```

This produces the production build at `UI/dist/team-building/browser/`.

### 2. Publish via Visual Studio

1. Open the solution in Visual Studio.
2. Right-click `TeamConnect.Api` in Solution Explorer → **Publish**.
3. Select the `teamconnect-01 - Zip Deploy` publish profile.
4. Click **Publish**.

Visual Studio will build the .NET project in Release mode, include the Angular dist files in `wwwroot/`, and deploy the zip package to Azure.

## Deployment Steps (CLI)

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download), [Node.js](https://nodejs.org), [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli). Log in to Azure before running:

```bash
az login
```

### 1. Build the Angular app

```bash
cd UI
npm run build
cd ..
```

### 2. Publish the .NET project

```bash
dotnet publish TeamConnect.Api/TeamConnect.Api.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o ./publish-output
```

The `IncludeAngularDist` target in the `.csproj` will automatically include the Angular files built in step 1 into `publish-output/wwwroot/`.

### 3. Zip the publish output

```bash
cd publish-output
zip -r ../deploy.zip .
cd ..
```

On Windows (PowerShell):

```powershell
Compress-Archive -Path publish-output\* -DestinationPath deploy.zip -Force
```

### 4. Deploy to Azure

```bash
az webapp deploy \
  --resource-group my-rg \
  --name teamconnect-01 \
  --src-path deploy.zip \
  --type zip
```

### 5. Clean up (optional)

```bash
rm -rf publish-output deploy.zip
```

---

## Notes

- The Angular build step is manual — Visual Studio's Publish does **not** trigger `npm run build` automatically.
- If the Angular build is skipped, the deployed app will serve stale or missing frontend files.
- The publish profile targets `linux-x64`. Do not change the runtime identifier without also updating the Azure App Service OS configuration.

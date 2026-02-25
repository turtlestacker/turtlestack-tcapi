# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This repository contains Siemens Teamcenter SOA (Service-Oriented Architecture) .NET client SDK examples and pre-built DLL libraries. It is a reference/example repository for building .NET client applications that communicate with a Teamcenter PLM server via its SOA REST/HTTP API.

## Building the Samples

All C# samples target **.NET Framework 4.8** and are built with **Visual Studio 2019 (16.7.10)**. There is no CLI build script — use Visual Studio:

1. Open the desired `.csproj` (or `.vbproj`) from the `soa_client_zip_examples/samples/` subdirectory
2. Build the project (F6 or Build → Build Solution)
3. Run with `-host http://server:port/tc` to override the default `http://localhost:7001/tc`

**Prerequisites before building:**
- A running Teamcenter Web Tier server and Pool Manager
- For `FileManagement`: set the `FMS_HOME` environment variable to your Teamcenter installation path
- For `RuntimeBO`: the sample runtime BO template must be deployed to the database via Business Modeler IDE

**Running in TCCS mode** (Teamcenter Client Communication System):
```
-host tccs://env_name    # specific TCCS environment
-host tccs               # let the app query available environments
```
TCCS requires `FMS_HOME` set and TCCS native libs in `PATH`.

## Repository Structure

```
soa_client_zip_examples/
├── docs/            # Link to Siemens Support Center API docs
├── net48/           # .NET Framework 4.8 DLLs (FCCNetClientProxy, TcSoaFMS)
├── netstandard2.0/  # .NET Standard 2.0 DLLs — the main SDK (hundreds of service DLLs)
└── samples/
    ├── HelloTeamcenter/    # Canonical starting point — session, home folder, query, data management (C#)
    ├── FileManagement/     # FMS file upload/download (C#)
    ├── RuntimeBO/          # Runtime Business Object create/modify (C#)
    ├── VendorManagement/   # Vendor/BidPackage/LineItem/VendorPart CRUD (C#)
    └── ProductConfigurator/ # Product configurator example (VB.NET)
```

Each sample has its own `clientx/` subfolder with the boilerplate framework implementations (Session, AppXCredentialManager, etc.) duplicated across samples.

## SOA Client Architecture

### Core Concept

The `Connection` object is the central singleton — it holds the server address, credential manager, and all listeners. All service stubs are created from it.

### Required Framework Interfaces

Every client application must provide implementations of these interfaces:

| Interface | Purpose |
|---|---|
| `CredentialManager` | Supplies username/password or SSO tokens; called on session timeout for re-auth |
| `ExceptionHandler` | Handles low-level communication/XML marshalling errors |
| `PartialErrorListener` | (Optional) Notified when a service op returns partial errors |
| `ModelEventListener` | (Optional) Notified when model objects are updated or deleted |

### Connection Setup Pattern

```csharp
// 1. Create credential manager
var credManager = new AppXCredentialManager(ssoURL, appID);

// 2. Create connection (no network contact until first service call)
var connection = new Connection(host, credManager);

// 3. Register handlers
connection.ExceptionHandler = new AppXExceptionHandler();
connection.ModelManager.AddPartialErrorListener(new AppXPartialErrorListener());
connection.ModelManager.AddModelEventListener(new AppXModelEventListener());
Connection.AddRequestListener(new AppXRequestListener());
```

### Service Call Pattern

Every service operation follows the same four steps:

```csharp
// 1. Get the service stub
DataManagementService dmService = DataManagementService.getService(connection);

// 2. Prepare input
var input = new SomeInputType { ... };

// 3. Call the operation
SomeResponse response = dmService.SomeOperation(input);

// 4. Check for partial errors and process results
if (response.ServiceData.sizeOfPartialErrors() > 0)
    throw new ServiceException("Operation returned partial errors.");
// ... use response data
```

### Session Lifecycle

A session must be established before any other service operations:
```csharp
SessionService sessionService = SessionService.getService(connection);
LoginResponse resp = sessionService.Login(user, password, group, role, locale, discriminator);
// ... do work ...
sessionService.Logout();
```

### Key SDK DLLs (from `netstandard2.0/`)

| DLL | Provides |
|---|---|
| `TcSoaClient.dll` | `Connection`, `CredentialManager`, `ModelManager` |
| `TcSoaCommon.dll` | `SoaConstants`, common utilities |
| `TcSoaCoreStrong.dll` | `SessionService`, `DataManagementService` (typed/strong) |
| `TcSoaQueryStrong.dll` | `SavedQueryService` |
| `TcSoaStrongModel.dll` | Strong-typed model objects (`Item`, `ItemRevision`, `WorkspaceObject`, `User`) |
| `TcServerNetBinding.dll` | HTTP/REST transport binding |
| `TcLogging.dll` | Logging framework |
| `TcSoaFMS.dll` (net48/) | `FileManagementUtility` for FMS file transfers |

### Namespace Conventions

- Service interfaces: `Teamcenter.Services.Strong.<Module>` (e.g., `Teamcenter.Services.Strong.Core`)
- Input/output types: `Teamcenter.Services.Strong.Core._YYYY_MM.DataManagement` (versioned sub-namespaces)
- Framework types: `Teamcenter.Soa.Client`, `Teamcenter.Soa.Client.Model`
- Strong model types: `Teamcenter.Soa.Client.Model.Strong`
- Exceptions: `Teamcenter.Schemas.Soa._2006_03.Exceptions`, `Teamcenter.Soa.Exceptions`

### Authentication Options

- **Standard**: `SoaConstants.CLIENT_CREDENTIAL_TYPE_STD` — prompts for username/password
- **SSO**: `SoaConstants.CLIENT_CREDENTIAL_TYPE_SSO` — uses `SsoCredentials(ssoURL, appID)`

The `ProductConfigurator` VB.NET sample shows an alternative pattern where credentials are passed in at construction time (instead of being prompted) — useful when integrating SOA calls into a larger application.

## API Documentation

Full service reference is available on the [Siemens Support Center](https://support.sw.siemens.com/en-US): Products → Teamcenter → Documentation tab → search "Teamcenter Services Reference".

---

## TcExplorer — Custom Console App

`TcExplorer/` is a .NET Framework 4.8 console app that connects to the RR-SMR Teamcenter server and exports:
1. The full recursive home-folder tree (all sub-folders and items)
2. The Cls0 classification hierarchy with class attribute definitions

**Build:** `dotnet build TcExplorer/TcExplorer.csproj -c Debug`
**Run:** `TcExplorer.exe -host https://tcweb03.dev.rolls-royce-smr.com:3000/tc`
**Output:** console tree (box-drawing chars) + `tc_explorer_output.json` (or `-out path.json`)

`bin/Debug/` is intentionally committed to git so the exe can be deployed by `git pull` without needing VS or dotnet SDK on the target machine.

### TcExplorer Structure

```
TcExplorer/
├── TcExplorer.csproj          .NET 4.8 Exe, x64+AnyCPU configs
├── Program.cs                 Entry point; args: -host -sso -appID -out
├── clientx/                   Verbatim copy from HelloTeamcenter/clientx/ (Teamcenter.ClientX ns)
├── model/ExplorerModel.cs     POCOs: FolderNode, ItemInfo, ClassNode, ClassAttribute, ExplorerResult
├── explore/
│   ├── FolderExplorer.cs      DataManagementService.GetProperties recursive walk
│   └── ClassificationExplorer.cs  Cls0 hierarchy + classic Classification attributes
└── output/
    ├── ConsoleRenderer.cs     UTF-8 box-drawing tree
    └── JsonExporter.cs        JavaScriptSerializer + PrettyPrint → UTF-8 file
```

### Classification API — Confirmed Patterns

The Siemens docs portal requires login; API was discovered via PowerShell reflection on the DLLs.

**Two services are used together:**
- `Cls0.Services.Strong.Classificationcore.ClassificationService` — hierarchy (top nodes, children)
- `Teamcenter.Services.Strong.Classification.ClassificationService` — attribute definitions

**Required DLLs** (beyond core SDK): `TcSoaClassificationStrong`, `Cls0SoaClassificationCoreStrong`, `Cla0SoaClassificationCommonStrong`, `TcSoaStrongModelClassificationCore`, `TcSoaStrongModelPartition`, `TcSoaStrongModelModelCore`

**Call `StrongObjectFactoryClassification.Init()` before any Cls0 model types are used.**

**Confirmed hashtable structures (discovered at runtime):**
```
GetHierarchyNodeDetails().NodeDetails  → key: Cls0HierarchyNode,  value: HierarchyNodeDetails[]
GetHierarchyNodeChildren().Children    → key: Cls0GroupNode,       value: HierarchyNodeDetails[]
GetAttributesForClasses().Attributes   → key: string (classId),    value: ClassAttribute[]
```

**Key gotcha:** `HierarchyNodeDetails.IsLeafNode` means "objects can be classified here", NOT "no children in the hierarchy". Always recurse regardless of this flag; children simply return empty when a node truly has none.

**Recursion pattern:** `GetTopLevelNodes()` → `GetHierarchyNodeDetails()` → iterate `HierarchyNodeDetails[]` → for each, call `GetHierarchyNodeChildren(d.NodeToUpdate)` → repeat. `d.NodeToUpdate` is the `Cls0HierarchyNode` needed as input to `GetHierarchyNodeChildren`.

### Known Fixes Applied

| Problem | Fix |
|---|---|
| `dotnet build` fails (AnyCPU platform not found) | Added `Debug\|AnyCPU` and `Release\|AnyCPU` PropertyGroups to .csproj |
| Home folder shows `(unknown)` for name/type | `LoadContents` now fetches `object_string` + `object_type` alongside `contents` in the same `GetProperties` call, before the node is constructed |
| JSON export crashes (`capacity must be > zero`) | `Indent()` helper changed from `new StringBuilder(level * unit.Length)` to `new StringBuilder()` — capacity=0 throws on .NET Framework 4.8 |
| Classification children always empty | `entry.Value` in Children hashtable is `HierarchyNodeDetails[]` not `Cls0HierarchyNode[]`; cast was always null |

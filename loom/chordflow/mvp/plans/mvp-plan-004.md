---
type: plan
id: pl_01KTKBD869200M81FCSWES0Q8A
title: "Phase 2b — Host migration: Photino → WinForms + WebView2"
status: done
created: "2026-06-08T00:00:00.000Z"
updated: "2026-06-08T00:00:00.000Z"
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: de_01KTHJD3QTBGRVX3BBRD29PKAW
requires_load: []
target_version: 0.1.0
steps:
  - id: repoint-the-host-project-csproj-targetframework
    order: 1
    status: done
    description: "Repoint the host project: csproj TargetFramework → net10.0-windows, UseWindowsForms=true, OutputType=WinExe; remove Photino.NET, add Microsoft.Web.WebView2; keep the wwwroot copy-to-output."
    files_touched: [src/ChordFlow.App/ChordFlow.App.csproj]
    blocked_by: []
    satisfies: [IN6, C2]
  - id: rewrite-program
    order: 2
    status: done
    description: "Rewrite Program.cs as a WinForms host (Form + dock-filled WebView2; EnsureCoreWebView2Async → SetVirtualHostNameToFolderMapping(\"chordflow.local\", wwwroot, Allow) → Navigate https://chordflow.local/index.html) and replace PhotinoBridge with WebView2Bridge (PostWebMessageAsString out; WebMessageReceived → WebMessageRouter in). Re-wire GenerateExercise + PracticeSession."
    files_touched: [src/ChordFlow.App/Program.cs, src/ChordFlow.App/Infrastructure/WebView2Bridge.cs, src/ChordFlow.App/Infrastructure/PhotinoBridge.cs]
    blocked_by: [1]
    satisfies: [IN6, IN8, C8]
  - id: update-the-app
    order: 3
    status: done
    description: "Update the app.js Bridge module: window.external.{sendMessage,receiveMessage} → window.chrome.webview.postMessage / addEventListener('message'); keep the no-host fallback; re-enable core.useWorkers (real origin) and confirm the same-origin soundfont path."
    files_touched: [src/ChordFlow.App/wwwroot/app.js]
    blocked_by: [2]
    satisfies: [IN7, IN8]
  - id: build-run-and-verify-tablature-renders
    order: 4
    status: done
    description: "Build, run and verify: tablature renders, status reaches \"ready · soundfont loaded\", Play → audio + synced beat cursor; dotnet test stays green; remove the throwaway spike and any Photino leftovers."
    files_touched: ["spike/WebView2Spike/*", "src/ChordFlow.App/*"]
    blocked_by: [3]
    satisfies: [IN7, C8]
---
# Phase 2b — Host migration: Photino → WinForms + WebView2

## Goal

Replace the Photino host with the official **WinForms + `Microsoft.Web.WebView2`** control (windowed controller) — Photino's composition controller renders a black window on the .NET 10 + WebView2-149 stack. Serve `wwwroot` over a `SetVirtualHostNameToFolderMapping` `https` origin (no server/port → C2; also fixes the `file://` soundfont-CORS block). Keep the bridge **envelope contract** identical; only `Infrastructure/` + the `app.js` transport shim change. Engine, renderer, feature slices, `WebMessageRouter`, and all tests are untouched. Satisfies IN6, IN7, IN8; constraints C2, C8. Rationale: `loom/refs/photino-net-desktop-host-reference.md`, chat `mvp-chat-002`.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Repoint the host project: csproj TargetFramework → net10.0-windows, UseWindowsForms=true, OutputType=WinExe; remove Photino.NET, add Microsoft.Web.WebView2; keep the wwwroot copy-to-output. | src/ChordFlow.App/ChordFlow.App.csproj | — | IN6, C2 |
| ✅ | 2 | Rewrite Program.cs as a WinForms host (Form + dock-filled WebView2; EnsureCoreWebView2Async → SetVirtualHostNameToFolderMapping("chordflow.local", wwwroot, Allow) → Navigate https://chordflow.local/index.html) and replace PhotinoBridge with WebView2Bridge (PostWebMessageAsString out; WebMessageReceived → WebMessageRouter in). Re-wire GenerateExercise + PracticeSession. | src/ChordFlow.App/Program.cs, src/ChordFlow.App/Infrastructure/WebView2Bridge.cs, src/ChordFlow.App/Infrastructure/PhotinoBridge.cs | 1 | IN6, IN8, C8 |
| ✅ | 3 | Update the app.js Bridge module: window.external.{sendMessage,receiveMessage} → window.chrome.webview.postMessage / addEventListener('message'); keep the no-host fallback; re-enable core.useWorkers (real origin) and confirm the same-origin soundfont path. | src/ChordFlow.App/wwwroot/app.js | 2 | IN7, IN8 |
| ✅ | 4 | Build, run and verify: tablature renders, status reaches "ready · soundfont loaded", Play → audio + synced beat cursor; dotnet test stays green; remove the throwaway spike and any Photino leftovers. | spike/WebView2Spike/*, src/ChordFlow.App/* | 3 | IN7, C8 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### First-time setup
This repo has a git submodule (`external/blazor-dragdrop`, the repo owner's own fork of a Blazor drag-and-drop library, referenced as a source-level `ProjectReference`). Initialize it before building:
```
git submodule update --init --recursive
```
Without this, `dotnet build`/`dotnet restore` fails with a "project file not found" error for `Plk.Blazor.DragDrop.csproj`.

### Build
```
dotnet build LoreBuilder.sln
```

### Run the app (Blazor WebAssembly dev server)
```
dotnet run --project src/LoreBuilder/LoreBuilder.fsproj
```
Serves at `http://localhost:5090` (see `src/LoreBuilder/Properties/launchSettings.json`).

### Tests
```
dotnet test
```
xUnit + Faqt (fluent assertions via the `%expr.Should()...` operator) + FsCheck (property-based tests), across `src/LoreBuilder.Test`, `src/FunSharp.Common.Test`, `src/FunSharp.Components.Test`. Run a single project with `dotnet test src/FunSharp.Common.Test/FunSharp.Common.Test.fsproj`; run a single test by name with `dotnet test --filter "FullyQualifiedName~<TestName>"`.

`src/LoreBuilder.Test/Tests.fs` contains a Playwright E2E test tagged `[<Trait("Category", "OnDemand")>]`. Nothing filters this out automatically (no `.runsettings`, no CI), so a plain `dotnet test` will try to launch a headless browser against `http://localhost:5090` and fail/hang unless the dev server is already running there. Exclude it explicitly when you don't want that: `dotnet test --filter "Category!=OnDemand"`.

## Architecture

LoreBuilder is an F# Blazor WebAssembly app (Bolero + Elmish, .NET 8) for building tabletop/worldbuilding "lore cards" and arranging them into drag-and-drop "lore clusters." It's early-stage; most active work happens in `src/LoreBuilder/Components` and `src/LoreBuilder/Data`.

### Solution layout
- `src/LoreBuilder` — the app itself.
- `src/FunSharp.Common` — general-purpose F# helpers (DU reflection helpers, `Dictionary`/`List` extensions, JSON serializer config, async/HTTP-error helpers) shared across the author's projects; not all of it is used by LoreBuilder.
- `src/FunSharp.Components` — shared Blazor component helpers (currently just `HoverArea.fs`).
- `external/blazor-dragdrop` — git submodule, a fork of a Blazor drag-and-drop library, referenced as a direct `ProjectReference` (not a NuGet package).
- Each library/app has a matching `*.Test` project (xUnit).

### State management is split, not unified
The app nominally uses Elm-architecture state via `Model/Application.fs` (`State`/`Message`) and `Update.fs`, wired up in `Main.fs`'s `Program.mkProgram`. In practice, only `Pages/HoverTest.fs` follows that pattern end-to-end (dispatching `Message`s through `Update.update`). Every other interactive component — `Components/Card.fs`, `Components/LoreCluster.fs`, `Pages/StackTest.fs`, `Pages/LoreClusterTest.fs`, `Pages/DragDropTest.fs` — manages its own state as local mutable fields/`[<Parameter>]` properties and calls `this.StateHasChanged()` directly, bypassing `Application.State`/`Update.fs` entirely. When touching card/cluster interaction logic, check which pattern the specific component already uses rather than assuming Elmish is in play.

### Card domain model (`Model/Card.fs`, `Builders.fs`, `Data/*.fs`)
- A `Card` has a `CardType` (Faction, Figure, Event, Location, Object, Creature, Material, Deity, Emblem, Modifier — plus `Unknown`, used as a sentinel for "no card") and two sides (`PrimarySide`/`SecondarySide: Cues`).
- `Cues` holds up to 4 edge cues (`Bottom`/`Left`/`Top`/`Right`), each an optional `Cue`: `Simple of text`, `Complex of {Header; Text; Expansions}` (with `Expansions: Logical<CardType> option` = `One`/`Any`/`All`), or `Icon of fileName`.
- Sample card data in `Data/*.fs` (one file per `CardType`) is authored using a custom F# computation-expression DSL defined in `Builders.fs` (e.g. `card { primary (cues { bottom "City"; top (background { text "..."; expansion figure }) }) }`).
- `CardType`-specific presentation (theme color, icon, text colors) lives entirely in pattern matches in `Model/Card.fs`; adding a new `CardType` means updating those matches, adding a new `Data/*.fs` file, and manually adding it to the hand-maintained `allCards` list in `Utils.fs` — nothing enforces these stay in sync.

### Lore clusters (`Model/Cluster.fs`, `Components/LoreCluster.fs`)
A `LoreCluster` is a fixed 9-slot layout (`ClusterPosition`: `Primary`, `Inner_*`, `Outer_*` for each of Bottom/Left/Top/Right) built with the `Plk.Blazor.DragDrop` submodule's `Dropzone<Card>` components. Drop-acceptance rules live in `LoreCluster.fs`'s `acceptDrop`: inner slots require matching the primary card's `CardType`; outer-slot rules are still a TODO (currently accept anything).

### Persistence
`Blazored.LocalStorage` is registered in `Startup.fs` but not currently used anywhere — no cluster/card state survives a page refresh yet. `FunSharp.Common/JsonSerializer.fs` configures `FSharp.SystemTextJson` for proper DU serialization, but it isn't wired into the LocalStorage service; if persistence is added, that config needs to be applied, since default `System.Text.Json` doesn't round-trip F# discriminated unions (`CardType`, `Cue`, `Logical<'T>`) correctly on its own.

# Issues & review findings

Tracking doc for a full-repo review (domain model, Blazor components, build/infra, shared
FunSharp libraries, tests). Items are grouped by category, tagged `[auto-fix]` (mechanical,
fixed without discussion) or `[needs decision]` (fix depends on a judgment call). Checked items
link to the commit that resolved them.

## Bugs (things broken today)

- [x] `[auto-fix]` `CardType.Unknown` leaks into "all card types" iteration — `Utils.fs:46-49`
  (`randomCards`) iterates `Union.toList<CardType>()`, which includes `Unknown`, so every demo
  page (`StackTest`, `CardTest`, `DragDropTest`) renders an extra bogus "?" card. Fixed by
  filtering `Unknown` out before mapping to `randomCard`.
- [x] `[needs decision]` Flipped cards silently reset — `Components/Card.fs:197-211` mutated its
  own `[<Parameter>]` fields (`CurrentSide`, `Rotation`) directly in event handlers instead of
  raising events to the parent. `LoreCluster.fs:154-161` re-passed a fixed, position-derived
  `CurrentSide` on every render, so any re-render of the cluster forcibly reset a flipped card
  back to its default side. User chose to lift the state into `LoreCluster`. Implemented:
  `Card` now takes `OnCurrentSideChanged`/`OnRotationChanged` callbacks (still self-manages its
  local field too, so standalone usage in `CardTest`/`StackTest`/`DragDropTest` is unaffected);
  `LoreCluster` now owns a `cardUiStates: Dictionary<ClusterPosition, CardUiState>` (new
  `CardUiState` type in `Model/Card.fs`) as the source of truth, initialized per-position to the
  same defaults the old static logic used, reset on `onDrop`, and passed down explicitly every
  render. Verified with `dotnet build` (0 warnings/errors) and manually in-browser: standalone
  card flip on `/CardTest` still works correctly (regression check). Could not fully exercise
  the LoreCluster drag-and-drop path via browser automation in this session - the app's native
  HTML5 drag-and-drop resisted both mouse-simulated and synthetic-DragEvent automation (the
  likely reason the repo's own `LoreBuilder.Test` Playwright test was left half-written). The
  fix was verified by build + careful trace of the data flow; a manual check is recommended
  (drop a card into a cluster's primary slot, flip it, drag any other card elsewhere on the
  page, confirm the flip persists).
- [ ] `[needs decision - deferred]` Outer cluster slots accept any card type —
  `LoreCluster.fs:73-76`, all four `Outer_*` positions in `acceptDrop` unconditionally return
  `true` (`// TODO: based on inner`), while inner slots correctly enforce
  `card.Type = cards[Primary].Type`. User asked to skip this for now and be reminded later -
  not implemented in this pass.
- [x] `[auto-fix]` PWA manifest still has the scaffold placeholder name — `wwwroot/manifest.json`
  `name`/`short_name` are `"fsharp_pwa3"` instead of "LoreBuilder". Fixed.
- [ ] `[parked]` PWA loses icons offline — `wwwroot/index.html` loads Font Awesome from
  `cdnjs.cloudflare.com`, which isn't part of `self.assetsManifest` and is never cached by the
  service worker. Not touched in this pass (would need a decision on self-hosting the font
  assets vs. accepting the offline gap).

## Architecture risks (will bite later)

- [ ] `[parked]` Elmish is largely decorative — only `Pages/HoverTest.fs` actually flows through
  `Application.Message`/`Update.fs`. Every other interactive component (`Card`, `LoreCluster`,
  `StackTest`, `LoreClusterTest`, `DragDropTest`) manages state as raw mutable fields. No
  undo/persistence/serialization is possible today without retrofitting Elmish onto components
  that were never built for it. Substantial design effort — not attempted without a dedicated
  conversation.
- [ ] `[parked]` Persistence is a trap waiting to spring — `Blazored.LocalStorage` is registered
  (`Startup.fs`) but never used. `FunSharp.Common/JsonSerializer.fs` correctly configures
  `FSharp.SystemTextJson` for DU round-tripping, but it's never wired into the LocalStorage
  service. The moment persistence is added, the DU-heavy domain (`CardType`, `Cue`,
  `Logical<'T>`) will silently fail to round-trip through default STJ unless this gets
  connected.
- [ ] `[needs decision]` Shotgun-surgery footprint on card types — adding a new `CardType`
  requires touching 2 exhaustive + 3 catch-all matches in `Model/Card.fs`, plus manually
  remembering to add the new `Data/X.fs` to the hand-maintained `allCards` list in `Utils.fs`
  (nothing enforces this stays in sync). `ClusterPosition` has a similar footprint (8 match
  sites across `LoreCluster.fs`/`Cluster.fs`). Noted for awareness; no action planned unless
  asked, since a fix would mean redesigning the extensibility mechanism.
- [ ] `[needs decision - deferred]` `Union.toString`/`toList` reflection-per-render cost — used
  uncached in hot render paths (`Card.fs`, `LoreCluster.fs`); roughly 150+ reflection calls per
  `LoreCluster` render. User said not right now.
- [x] `[auto-fix]` `Card.copy` (`Model/Card.fs:177-179`) reads as dead code
  (`{ card with Type = card.Type }`) but isn't actually broken — F# record-update always
  allocates a new reference, which is exactly what its call site in `StackTest.fs`'s
  `Dropzone<Card>` `CopyItem` callback needs. Documented with a comment instead of
  renamed/removed, to stop a future cleanup pass from "simplifying" it away.
- [ ] `[parked]` No `key` on `for` loops rendering `comp<>` (`LoreClusterTest.fs`, `CardTest.fs`,
  `StackTest.fs`). Harmless today since the lists are static; would only matter once
  cluster/card lists become dynamic. Not fixed preemptively.

## Build / infra

- [x] `[auto-fix]` Fresh clone doesn't build — `external/blazor-dragdrop` submodule wasn't
  initialized and README had no setup instructions. Documented
  `git submodule update --init --recursive` plus basic build/run commands in `README.md`.
- [ ] `[parked]` Submodule pin is fragile — the pinned commit is fetchable by SHA today but
  isn't reachable from any branch head in that fork. If that history is ever rewritten/pruned,
  the pin breaks with no local fallback. No action without the user's input on how they want to
  manage their fork.
- [x] `[auto-fix]` Wildcard NuGet versions in `LoreBuilder.fsproj` (`Bolero`, `Bolero.Build`,
  ASP.NET Core WebAssembly DevServer, `System.Net.Http.Json`) made builds non-reproducible and
  conflicted with `FunSharp.Components.fsproj` pinning the same Bolero package to an older exact
  version (`0.24.39` vs. the `0.25.63` LoreBuilder's wildcard was actually resolving to).
  Pinned `LoreBuilder.fsproj` to the versions that were resolving (`Bolero`/`Bolero.Build`
  `0.25.63`, `Microsoft.AspNetCore.Components.WebAssembly.DevServer` `8.0.30`,
  `System.Net.Http.Json` `8.0.1`) and bumped `FunSharp.Components.fsproj`'s Bolero pin to match
  `0.25.63`, so both projects now agree instead of silently unifying. Verified with
  `dotnet build LoreBuilder.sln` (0 warnings, 0 errors).
- [ ] `[needs decision - deferred]` No CI anywhere (no `.github/`, no pipeline config). Nothing
  verifies the 4 test projects before changes land on `main`. User said not right now.
- [x] `[auto-fix]` `Startup.fs` sets `LogLevel.Trace` unconditionally, shipping to a published
  build as-is. Guarded with `#if DEBUG`/`#endif`.

## Test coverage gaps

- [x] `[needs decision]` Zero coverage of the fastest-changing code — `Card`, `Cue`, `Cluster`,
  and `Builders.fs` (touched by 5 of the last 5 commits) had no unit or E2E tests at all. User
  chose "clean up + add basic domain tests." Implemented: added `LoreBuilder.Test/CardTests.fs`
  (`CardEdge.opposite`, `CardType` presentation rules, `Card.empty`/`Card.copy`),
  `ClusterTests.fs` (`ClusterPosition.fromIndex`/`toRotation`/`toString`), and
  `BuildersTests.fs` (the `card`/`cues`/complex-cue computation-expression DSL, including both
  overloads of `bottom`/`left`/`top`/`right` and the `expansion`/`expansions_any`/
  `expansions_all` operations) — 25 tests total. This required adding a `LoreBuilder.fsproj`
  `ProjectReference` to `LoreBuilder.Test.fsproj`, since the test project previously had no way
  to reach the domain code at all. Verified by actually running them (not just compiling) —
  since this sandbox only has the .NET 10 runtime and the app targets net8.0, temporarily
  installed a local net8.0 runtime via Microsoft's official `dotnet-install.ps1` (isolated to
  `%TEMP%`, not a system-wide change) and ran with `DOTNET_ROOT` pointed at it:
  `Passed! - Failed: 0, Passed: 25, Skipped: 0, Total: 25`.
- [x] `[needs decision]` Playwright was set up and abandoned mid-task
  (`LoreBuilder.Test/Tests.fs`) — the actual click/assert logic was commented out, the test was
  tagged `OnDemand` so it never ran by default, and it asserted a redirect to `/TestPage`, a
  route that doesn't exist in the current app (see `Routes.fs`) — confirming it was stale, not
  just unfinished. Removed the file, the `Microsoft.Playwright` package reference from
  `LoreBuilder.Test.fsproj`, and the now-empty `Tests.fs` compile entry, since fixing it would
  mean writing a new E2E test from scratch (a bigger investment than "basic domain tests").
- [x] `[needs decision]` `FunSharp.Components.Test` was a `dotnet new xunit` stub
  (`Assert.True(true)`) despite referencing Playwright — removed the stub test file and the
  unused `Microsoft.Playwright` package reference; the project now has zero tests (honest, not
  fake) since testing `HoverArea.fs` meaningfully would need a Blazor component-testing library
  (e.g. bUnit) that isn't part of the repo today — bringing one in is a separate decision, not
  "cleanup." `LoreBuilder.Test.Common` still has no `.fs` files, but turns out not to be pure
  dead weight: both `LoreBuilder.Test.fsproj` and `FunSharp.Components.Test.fsproj` reference it
  specifically to get `Faqt`/`FsCheck.Xunit` transitively, and the new domain tests above
  confirmed that still works. Left as-is.
- [ ] `[needs decision]` `JsonSerializer.Test.fs` only checks config flags, never an actual DU
  round-trip, so the persistence risk above is untested.
- [x] `[needs decision]` `FunSharp.Common/AsyncResult.fs` and `HttpError.fs` were unused anywhere
  in this app and untested. User confirmed they're not needed elsewhere — deleted both files
  and their `Compile Include` entries in `FunSharp.Common.fsproj`. Verified with
  `dotnet build LoreBuilder.sln`.

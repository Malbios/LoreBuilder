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
- [ ] `[needs decision]` Flipped cards silently reset — `Components/Card.fs:197-211` mutates its
  own `[<Parameter>]` fields (`CurrentSide`, `Rotation`) directly in event handlers instead of
  raising events to the parent. `LoreCluster.fs:154-161` re-passes `CurrentSide` on every
  render, so any re-render of the cluster (e.g. `DropzonesAreActive` toggling while dragging
  anywhere on the page) forcibly resets a card's flip state.
- [ ] `[needs decision]` Outer cluster slots accept any card type — `LoreCluster.fs:73-76`, all
  four `Outer_*` positions in `acceptDrop` unconditionally return `true`
  (`// TODO: based on inner`), while inner slots correctly enforce
  `card.Type = cards[Primary].Type`.
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
- [ ] `[needs decision]` `Union.toString`/`toList` reflection-per-render cost — used uncached in
  hot render paths (`Card.fs`, `LoreCluster.fs`); roughly 150+ reflection calls per
  `LoreCluster` render. Performance concern, not a correctness bug — needs a decision on
  whether to cache now or defer.
- [ ] `[auto-fix]` `Card.copy` (`Model/Card.fs:177-179`) reads as dead code
  (`{ card with Type = card.Type }`) but isn't actually broken — F# record-update always
  allocates a new reference, which is exactly what its call site in `StackTest.fs`'s
  `Dropzone<Card>` `CopyItem` callback needs. Will be documented with a comment instead of
  renamed/removed, to stop a future cleanup pass from "simplifying" it away.
- [ ] `[parked]` No `key` on `for` loops rendering `comp<>` (`LoreClusterTest.fs`, `CardTest.fs`,
  `StackTest.fs`). Harmless today since the lists are static; would only matter once
  cluster/card lists become dynamic. Not fixed preemptively.

## Build / infra

- [ ] `[auto-fix]` Fresh clone doesn't build — `external/blazor-dragdrop` submodule wasn't
  initialized and README had no setup instructions. Will document
  `git submodule update --init --recursive` in `README.md`.
- [ ] `[parked]` Submodule pin is fragile — the pinned commit is fetchable by SHA today but
  isn't reachable from any branch head in that fork. If that history is ever rewritten/pruned,
  the pin breaks with no local fallback. No action without the user's input on how they want to
  manage their fork.
- [ ] `[auto-fix]` Wildcard NuGet versions in `LoreBuilder.fsproj` (`Bolero`, `Bolero.Build`,
  ASP.NET Core WebAssembly DevServer, `System.Net.Http.Json`) make builds non-reproducible and
  conflict with `FunSharp.Components.fsproj` pinning the same Bolero package to an exact
  version. Will pin to currently-resolved versions.
- [ ] `[needs decision]` No CI anywhere (no `.github/`, no pipeline config). Nothing verifies the
  4 test projects before changes land on `main`.
- [ ] `[auto-fix]` `Startup.fs` sets `LogLevel.Trace` unconditionally, shipping to a published
  build as-is. Will guard with `#if DEBUG`.

## Test coverage gaps

- [ ] `[needs decision]` Zero coverage of the fastest-changing code — `Card`, `Cue`, `Cluster`,
  and `Builders.fs` (touched by 5 of the last 5 commits) have no unit or E2E tests at all.
- [ ] `[needs decision]` Playwright was set up and abandoned mid-task
  (`LoreBuilder.Test/Tests.fs`) — the actual click/assert logic is commented out, and the test
  is tagged `OnDemand` so it never runs by default.
- [ ] `[needs decision]` `FunSharp.Components.Test` is a `dotnet new xunit` stub
  (`Assert.True(true)`) despite referencing Playwright; `LoreBuilder.Test.Common` has no `.fs`
  files at all — both are dead scaffolding.
- [ ] `[needs decision]` `JsonSerializer.Test.fs` only checks config flags, never an actual DU
  round-trip, so the persistence risk above is untested.
- [ ] `[needs decision]` `FunSharp.Common/AsyncResult.fs` and `HttpError.fs` are unused anywhere
  in this app (likely carried over from another project using the same shared library) and
  untested. Not deleted without confirming they're not needed elsewhere.

namespace LoreBuilder.Pages

open System
open System.Collections.Generic
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder
open LoreBuilder.Components
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.Extensions.Logging
open Microsoft.JSInterop


type private HomeModel = {
    // The card currently being dragged from the sidebar (if any) - None means no drag is in
    // progress. Carries the actual card (not just a bool) so LoreCluster can tell which of its
    // dropzones would actually accept it, instead of showing every structurally-open dropzone as
    // active regardless of card type.
    DraggedCard: Card option
    IsPanelOpen: bool
    // While true, clicking a removable card deletes it instead of flipping it (see
    // Card.IsDeleteMode) - toggled manually, on and off, via the activity-bar button.
    IsDeleteMode: bool
    // Which canvas (root or a sub-canvas) is currently mounted/visible - the ancestor chain up to
    // root is derived on demand from ParentLink (see CanvasTree.breadcrumbTrail) rather than kept
    // as its own separate navigation stack.
    ActiveCanvasId: Guid
    // The cluster currently being freely repositioned by the user (if any), and the
    // mouse/position it started from - used to compute the live position on every mousemove.
    // Always refers to a cluster on the *currently active* canvas - dragging can't survive a
    // navigation since that's not reachable through normal interaction (dive-in/extraction only
    // fire on a plain click, never mid-drag).
    DraggingClusterId: Guid option
    DragStartMouseX: float
    DragStartMouseY: float
    DragStartX: float
    DragStartY: float
}

type Home() =
    inherit Component()

    // Guid.NewGuid() never produces Guid.Empty, so this is safe as a permanent sentinel for "the
    // one root canvas" - every other canvas id is randomly generated.
    let rootCanvasId = Guid.Empty

    let canvases = Dictionary<Guid, CanvasState>()
    do canvases[rootCanvasId] <- CanvasState.createRoot rootCanvasId

    let mutable model = {
        DraggedCard = None
        IsPanelOpen = true
        IsDeleteMode = false
        ActiveCanvasId = rootCanvasId
        DraggingClusterId = None
        DragStartMouseX = 0.0
        DragStartMouseY = 0.0
        DragStartX = 0.0
        DragStartY = 0.0
    }

    // Each cluster's reserved box size, and the fixed spot every extracted cluster's sub-canvas
    // starts it at. Offset a full "reach" (matching Canvas.fs's own reach = cellSize * 2.0, used
    // there to size a scroll-range spacer around this same cluster) away from the origin in both
    // directions - a native scroll container can only ever reach non-negative scroll positions,
    // so keeping the cluster (and the spacer built around it) entirely on the positive side of
    // (0, 0) is what makes Canvas.fs's own centerOn call able to actually scroll it into view
    // centered, rather than the browser silently clamping toward 0 once the origin itself would've
    // gone negative.
    let cellSize = 550.0
    let startPosition = (cellSize * 2.0, cellSize * 2.0)

    // A freshly drop-anywhere-created cluster's footprint before LoreCluster's own
    // OnAfterRender has had a chance to report its real one (see OnFootprintChanged below) - it
    // always starts with just a primary card, so this matches LoreCluster.ComputeMargin's
    // result for that exact case (270 base + 2*60 primary-only margin).
    let primaryOnlyFootprint = 390.0

    // An extracted cluster's footprint - it starts with a primary plus one auto-attached Inner
    // Modifier card, matching LoreCluster.ComputeMargin's result for that case (270 base +
    // 2*(60 primary margin + 40 an-inner-card-exists margin) = 470).
    let primaryPlusInnerFootprint = 470.0

    // Both positions are each cluster's box top-left corner, but the box's actual drawn content
    // is centered within it (see cellSize's doc comment) - compare true visual centers against
    // the sum of each cluster's own half-footprint, not a flat shared threshold.
    let overlaps (footprintA: float) (xA, yA) (footprintB: float) (xB, yB) =
        let halfSum = (footprintA + footprintB) / 2.0
        abs ((xA + cellSize / 2.0) - (xB + cellSize / 2.0)) < halfSum
        && abs ((yA + cellSize / 2.0) - (yB + cellSize / 2.0)) < halfSum

    let footprintOf (canvas: CanvasState) id =
        match canvas.ClusterFootprints.TryGetValue id with
        | true, footprint -> footprint
        | false, _ -> primaryOnlyFootprint

    let wouldOverlapAny (canvas: CanvasState) (excludeId: Guid option) (candidateFootprint: float) candidate =
        canvas.ClusterPositions
        |> Seq.exists(fun pair ->
            Some pair.Key <> excludeId && overlaps candidateFootprint candidate (footprintOf canvas pair.Key) pair.Value)

    let zoomStep = 0.1

    // One entry per ever-mounted Canvas instance, registered by each one via its own
    // OnZoomHandlerReady callback right after it first mounts (see Components/Canvas.fs) - lets
    // the zoom +/- buttons below, which live in this activity-bar (not floated on the canvas
    // itself), trigger a button-anchored zoom on whichever canvas is currently active without
    // Bolero exposing a direct component-reference mechanism.
    let zoomHandlers = Dictionary<Guid, float -> unit>()

    // Held so JS can call back into this component (loreBuilderCanvas.registerEscapeKey) and
    // disposed of properly when the component goes away - standard Blazor JS-interop hygiene for
    // a reference JS itself holds onto.
    let mutable escapeKeyDotNetRef: DotNetObjectReference<Home> option = None

    override _.CssScope = CssScopes.LoreBuilder

    [<Inject>]
    member val Logger: ILogger<Home> = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val JSRuntime: IJSRuntime = Unchecked.defaultof<_> with get, set

    // Modifier and Emblem are never something the user drags in directly - Modifiers only ever
    // arrive auto-attached by extraction (Utils.randomModifierCard) or picked via a cluster
    // slot's own "Any"/"One" click-to-pick trigger (Utils.randomCandidatesFor), and Emblems have
    // no place in a cluster at all yet. Utils.allCards itself stays the full set (that random-pick
    // machinery still needs every type in it) - only the sidebar's own drag-in list is filtered.
    member this.Cards =
        Utils.allCards
        |> List.filter(fun cards ->
            match cards with
            | card :: _ -> card.Type <> CardType.Modifier && card.Type <> CardType.Emblem
            | [] -> true)

    member this.TriggerReRender() = this.StateHasChanged()

    member private this.ActiveCanvas = canvases[model.ActiveCanvasId]

    // Removing the primary card leaves the cluster with no cards at all (see
    // LoreCluster.OnClusterEmptied's doc comment) - drop the whole reserved position rather
    // than keeping an empty, re-fillable slot around. A now-empty sub-canvas (which, by
    // construction, only ever held that one cluster) is torn down entirely and its parent
    // position unlocked - see CanvasTree.removeEmptySubCanvas.
    member this.OnClusterEmptied (canvasId: Guid) (clusterId: Guid) =

        match canvases.TryGetValue canvasId with
        | false, _ -> ()
        | true, canvas ->
            canvas.ClusterPositions.Remove clusterId |> ignore
            canvas.ClusterFootprints.Remove clusterId |> ignore
            canvas.InitialCards.Remove clusterId |> ignore
            canvas.InitialInnerCards.Remove clusterId |> ignore
            canvas.InitialPrimaryRotations.Remove clusterId |> ignore

            match CanvasTree.removeEmptySubCanvas canvases canvasId model.ActiveCanvasId with
            | Some newActiveCanvasId -> model <- { model with ActiveCanvasId = newActiveCanvasId }
            | None -> ()

            // A sub-canvas that removeEmptySubCanvas just tore down also drops out of the
            // keep-alive render loop for good (see Render()'s own doc comment) - its registered
            // zoom handler would otherwise sit in zoomHandlers forever, unreachable.
            if not (canvases.ContainsKey canvasId) then
                zoomHandlers.Remove canvasId |> ignore

            this.TriggerReRender()

    // A cluster's own footprint doesn't need a re-render just to be recorded - it's only
    // consulted on-demand by the overlap check during a later drag/drop.
    member this.OnFootprintChanged (canvasId: Guid) (clusterId: Guid) (footprint: float) =
        match canvases.TryGetValue canvasId with
        | true, canvas -> canvas.ClusterFootprints[clusterId] <- footprint
        | false, _ -> ()

    member this.StartClusterDrag (clusterId: Guid) (e: MouseEventArgs) =

        match this.ActiveCanvas.ClusterPositions.TryGetValue clusterId with
        | false, _ -> ()
        | true, (x, y) ->
            model <- {
                model with
                    DraggingClusterId = Some clusterId
                    DragStartMouseX = e.ClientX
                    DragStartMouseY = e.ClientY
                    DragStartX = x
                    DragStartY = y
            }

            this.TriggerReRender()

    member this.UpdateClusterDrag (e: MouseEventArgs) =

        match model.DraggingClusterId with
        | None -> ()
        | Some id ->
            let canvas = this.ActiveCanvas

            // e.ClientX/Y deltas are screen pixels - divide by the active canvas's own current
            // Zoom to get the equivalent canvas-space move (a screen-pixel drag covers less
            // canvas-space distance when zoomed in, more when zoomed out).
            let deltaX = (e.ClientX - model.DragStartMouseX) / canvas.Zoom
            let deltaY = (e.ClientY - model.DragStartMouseY) / canvas.Zoom
            let candidate = (model.DragStartX + deltaX, model.DragStartY + deltaY)

            // Overlapping the target simply keeps the cluster at its last valid position for
            // this tick rather than any push/slide resolution - the next mousemove tries again.
            if not (wouldOverlapAny canvas (Some id) (footprintOf canvas id) candidate) then
                canvas.ClusterPositions[id] <- candidate
                this.TriggerReRender()

    member this.EndClusterDrag() =

        if model.DraggingClusterId.IsSome then
            model <- { model with DraggingClusterId = None }
            this.TriggerReRender()

    // Reports the active canvas's own Zoom change back into its CanvasState - no re-render
    // needed, the mounted Canvas already repaints itself.
    member this.OnZoomChanged (canvasId: Guid) (zoom: float) =
        match canvases.TryGetValue canvasId with
        | true, canvas -> canvas.Zoom <- zoom
        | false, _ -> ()

    // Records the given canvas's own button-triggered-zoom closure once it reports itself ready
    // (see Canvas.fs's OnZoomHandlerReady) - looked up by the activity-bar's zoom +/- buttons
    // below, keyed on whichever canvas is currently active.
    member this.OnZoomHandlerReady (canvasId: Guid) (handler: float -> unit) =
        zoomHandlers[canvasId] <- handler

    member this.ZoomActiveCanvas(delta: float) =
        match zoomHandlers.TryGetValue model.ActiveCanvasId with
        | true, handler -> handler delta
        | false, _ -> ()

    // Drop-anywhere: a card dropped onto empty canvas space (i.e. not caught by any existing
    // cluster's own dropzone, which sits above this one) starts a brand new, unconnected
    // cluster right where it landed. Only ever fires for the root canvas - Canvas.fs only renders
    // the background dropzone that triggers this when CanvasState.ParentLink is None.
    member this.OnCanvasDrop (canvasId: Guid) (card: Card, canvasX: float, canvasY: float) =

        match canvases.TryGetValue canvasId with
        | false, _ -> ()
        | true, canvas ->
            // ClusterPositions holds each cluster's box top-left corner, but the drop point is
            // where the card visually landed - center the new box on that point (half a cell
            // size back in each direction) rather than anchoring its corner there, so the
            // cluster actually appears where it was dropped.
            let candidate = (canvasX - cellSize / 2.0, canvasY - cellSize / 2.0)

            if not (wouldOverlapAny canvas None primaryOnlyFootprint candidate) then
                let id = Guid.NewGuid()
                canvas.ClusterPositions[id] <- candidate
                canvas.InitialCards[id] <- card
                this.TriggerReRender()

    // Extraction: copies an eligible Outer card (see LoreCluster's canBeExtracted) into a
    // brand-new cluster on its own dedicated sub-canvas, auto-attaching a random Modifier card to
    // its Inner_Bottom slot (facing back toward the source, matching this cluster's own default
    // tie-break direction) - the source cluster/card is left untouched. Auto-navigates into the
    // new sub-canvas once created.
    member this.OnExtractCard
        (sourceCanvasId: Guid)
        (sourceClusterId: Guid)
        (sourcePosition: ClusterPosition)
        (card: Card)
        (primaryRotation: int)
        =

        match canvases.TryGetValue sourceCanvasId with
        | false, _ -> ()
        | true, sourceCanvas ->
            let newCanvasId = Guid.NewGuid()
            let newClusterId = Guid.NewGuid()
            let innerPosition = ClusterPosition.Inner_Bottom

            let newCanvas =
                CanvasState.createSubCanvas
                    newCanvasId
                    sourceCanvasId
                    sourceClusterId
                    sourcePosition
                    newClusterId
                    startPosition
                    (Card.copy card)
                    primaryRotation
                    innerPosition
                    (Utils.randomModifierCard ())
                    primaryPlusInnerFootprint

            canvases[newCanvasId] <- newCanvas
            sourceCanvas.ChildCanvasOf[(sourceClusterId, sourcePosition)] <- newCanvasId

            model <- { model with ActiveCanvasId = newCanvasId }

            this.TriggerReRender()

    // Navigates into the sub-canvas that was spawned from this exact (clusterId, position) - see
    // LoreCluster's canDiveIn.
    member this.OnDiveIn (canvasId: Guid) (clusterId: Guid) (position: ClusterPosition) =

        match canvases.TryGetValue canvasId with
        | false, _ -> ()
        | true, canvas ->
            match canvas.ChildCanvasOf.TryGetValue((clusterId, position)) with
            | true, childCanvasId ->
                model <- { model with ActiveCanvasId = childCanvasId }
                this.TriggerReRender()
            | false, _ -> ()

    member this.NavigateTo (canvasId: Guid) =
        if canvases.ContainsKey canvasId && model.ActiveCanvasId <> canvasId then
            model <- { model with ActiveCanvasId = canvasId }
            this.TriggerReRender()

    // The breadcrumb label for one canvas - root shows a fixed literal; every other level shows
    // its CardType icon plus whichever cue was active on the source Outer card at the moment of
    // extraction (same active-cue computation LoreCluster.fs already does for Primary), falling
    // back to just the type name if that cue has no Simple/Complex text to show.
    member private this.BreadcrumbLabel(canvasId: Guid) =

        match canvases.TryGetValue canvasId with
        | false, _ -> CardType.Unknown, "?"
        | true, canvas ->
            match canvas.SpawnedFromCard, canvas.SpawnedFromRotation with
            | Some card, Some rotation ->
                let text =
                    match CardHelpers.activeCue card.PrimarySide CardEdge.Bottom rotation with
                    | Some(Cue.Simple s) -> s
                    | Some(Cue.Complex c) -> c.Text
                    | _ -> Union.toString card.Type

                card.Type, text
            | _ -> CardType.Unknown, "Root"

    [<JSInvokable>]
    member this.OnEscapePressed() =
        match this.ActiveCanvas.ParentLink with
        | Some(parentCanvasId, _, _) ->
            model <- { model with ActiveCanvasId = parentCanvasId }
            this.TriggerReRender()
        | None -> ()

    override this.OnAfterRenderAsync(firstRender: bool) =
        task {
            if firstRender then
                let dotNetRef = DotNetObjectReference.Create(this)
                escapeKeyDotNetRef <- Some dotNetRef

                do!
                    this.JSRuntime
                        .InvokeVoidAsync("loreBuilderCanvas.registerEscapeKey", dotNetRef)
                        .AsTask()
        }
        :> System.Threading.Tasks.Task

    interface IDisposable with
        member _.Dispose() =
            escapeKeyDotNetRef |> Option.iter (fun r -> r.Dispose())

    override this.Render() =

        let activeCanvasId = model.ActiveCanvasId
        let trail = CanvasTree.breadcrumbTrail canvases activeCanvasId

        div {
            attr.``class`` "home-layout"

            // Listening this high up (rather than just on the mounted Canvas's own element) means
            // a fast drag that briefly carries the cursor over the sidebar/activity-bar still
            // keeps tracking - only leaving the browser window entirely would lose it.
            on.mousemove (fun e -> this.UpdateClusterDrag e)
            on.mouseup (fun _ -> this.EndClusterDrag())

            div {
                attr.``class`` "activity-bar"

                div {
                    attr.``class`` (if model.IsPanelOpen then "activity-bar-icon active" else "activity-bar-icon")

                    on.click (fun _ ->
                        model <- { model with IsPanelOpen = not model.IsPanelOpen }
                        this.TriggerReRender())

                    i { attr.``class`` "fa-solid fa-layer-group" }
                }

                div {
                    attr.``class`` (if model.IsDeleteMode then "activity-bar-icon active" else "activity-bar-icon")

                    on.click (fun _ ->
                        model <- { model with IsDeleteMode = not model.IsDeleteMode }
                        this.TriggerReRender())

                    i { attr.``class`` "fa-solid fa-trash" }
                }

                div {
                    attr.``class`` "activity-bar-icon"
                    on.click (fun _ -> this.ZoomActiveCanvas zoomStep)

                    i { attr.``class`` "fa-solid fa-magnifying-glass-plus" }
                }

                div {
                    attr.``class`` "activity-bar-icon"
                    on.click (fun _ -> this.ZoomActiveCanvas -zoomStep)

                    i { attr.``class`` "fa-solid fa-magnifying-glass-minus" }
                }
            }

            div {
                attr.``class`` "side-panel"
                attr.style (if model.IsPanelOpen then "grid-template-columns: 1fr;" else "")

                div {
                    attr.``class`` "side-panel-content"

                    div {
                        attr.``class`` "card-stack"

                        for cards in this.Cards do
                            comp<CardStack> {
                                attr.key (List.head cards).Type
                                "Size" => 110
                                "Cards" => cards
                                "OnDragStart" => fun (card: Card) ->
                                    model <- { model with DraggedCard = Some card }
                                    this.TriggerReRender()
                                "OnDragEnd" => fun () ->
                                    model <- { model with DraggedCard = None }
                                    this.TriggerReRender()
                            }
                    }
                }
            }

            // Always rendered (visibility toggled via CSS) rather than structurally
            // included/excluded with a bare `if` - Blazor's diffing matches the canvas keep-alive
            // loop below by key, but that matching only works if this loop lands at a *stable*
            // tree position across renders. A conditional sibling ahead of it that comes and goes
            // (as this would if gated by `if trail.Length > 1 then`) shifts that position exactly
            // when navigation state changes, which is exactly when the loop most needs to still
            // match up correctly - so every ever-visited canvas's own LoreCluster state (every
            // card tugged onto it) got silently destroyed and recreated on the very navigation
            // this whole keep-alive scheme exists to survive. Hidden at root (trail length 1) so
            // root's own UI still looks pixel-identical to before this feature existed.
            div {
                attr.``class`` "breadcrumb-bar"
                attr.style (if trail.Length > 1 then "" else "display: none;")

                trail
                    |> List.mapi(fun idx canvasId ->
                        let cardType, label = this.BreadcrumbLabel canvasId
                        let isLast = idx = trail.Length - 1

                        div {
                            attr.key canvasId
                            attr.``class`` (if isLast then "breadcrumb-crumb current" else "breadcrumb-crumb")
                            attr.title label

                            // Always attached rather than only when not isLast - clicking the
                            // current (last) crumb just navigates to itself, a no-op already
                            // handled by NavigateTo's own guard, so there's no need for a
                            // conditional Attr here (which Bolero's div CE can't type-check
                            // without a matching else branch).
                            on.click (fun _ -> this.NavigateTo canvasId)

                            if cardType <> CardType.Unknown then
                                i { attr.``class`` $"fa-solid {CardType.icon cardType}" }

                            text label

                            if not isLast then
                                i { attr.``class`` "fa-solid fa-chevron-right breadcrumb-separator" }
                        }
                    )
                    |> Utils.renderList
                }

            // Every ever-visited canvas stays mounted permanently (never remounted by attr.key on
            // navigation) - each one's own LoreCluster instances hold significant local state
            // (every card tugged onto them) that only lives in that component instance, so
            // destroying and recreating it on every dive-in/breadcrumb navigation would silently
            // lose it. Only the active one is actually visible; the rest sit at display:none,
            // which stops their content from painting or receiving input but leaves their
            // component instances (and this component's own JS registrations) alive. A canvas is
            // only ever truly unmounted - dropping out of this loop entirely - when its
            // CanvasState is removed from `canvases` for real, i.e. actual deletion (see
            // OnClusterEmptied).
            for pair in canvases do
                let canvasId = pair.Key
                let canvasState = pair.Value

                div {
                    attr.key canvasId
                    attr.style (if canvasId = activeCanvasId then "" else "display: none;")

                    comp<Canvas> {
                        "CanvasState" => canvasState
                        "DraggedCard" => model.DraggedCard
                        "IsDeleteMode" => model.IsDeleteMode
                        "InitialZoom" => canvasState.Zoom
                        "OnZoomHandlerReady" => fun (handler: float -> unit) -> this.OnZoomHandlerReady canvasId handler
                        "OnZoomChanged" => fun (zoom: float) -> this.OnZoomChanged canvasId zoom
                        "OnCanvasDrop" => fun (card: Card, x: float, y: float) -> this.OnCanvasDrop canvasId (card, x, y)
                        "OnClusterEmptied" => fun (clusterId: Guid) -> this.OnClusterEmptied canvasId clusterId
                        "OnFootprintChanged" =>
                            fun (clusterId: Guid) (footprint: float) -> this.OnFootprintChanged canvasId clusterId footprint
                        "OnPrimaryMouseDown" => fun (clusterId: Guid) (e: MouseEventArgs) -> this.StartClusterDrag clusterId e
                        "OnExtractCard" =>
                            fun (clusterId: Guid) (position: ClusterPosition) (card: Card) (rotation: int) ->
                                this.OnExtractCard canvasId clusterId position card rotation
                        "OnDiveIn" => fun (clusterId: Guid) (position: ClusterPosition) -> this.OnDiveIn canvasId clusterId position
                    }
                }
        }

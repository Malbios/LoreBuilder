namespace LoreBuilder.Pages

open System
open System.Collections.Generic
open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Components
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.Extensions.Logging
open Microsoft.JSInterop
open Plk.Blazor.DragDrop


type private HomeModel = {
    // The card currently being dragged from the sidebar (if any) - None means no drag is in
    // progress. Carries the actual card (not just a bool) so LoreCluster can tell which of its
    // dropzones would actually accept it, instead of showing every structurally-open dropzone as
    // active regardless of card type.
    DraggedCard: Card option
    IsPanelOpen: bool
    // While true, clicking a removable card deletes it instead of flipping it (see
    // Card.IsDeleteMode) - toggled manually, on and off, via the activity-bar button. Mutually
    // exclusive with IsExtractionMode (turning one on turns the other off).
    IsDeleteMode: bool
    // While true, clicking an eligible (Outer) card extracts it into a brand-new cluster of its
    // own (see Card.IsExtractionMode) - toggled via its own activity-bar button.
    IsExtractionMode: bool
    // The cluster currently being freely repositioned by the user (if any), and the
    // mouse/position it started from - used to compute the live position on every mousemove.
    DraggingClusterId: Guid option
    DragStartMouseX: float
    DragStartMouseY: float
    DragStartX: float
    DragStartY: float
    // The canvas's current zoom level (1.0 = 100%) - applied as a CSS transform:scale() on
    // .canvas-content, so every other canvas-space calculation (cluster positions, drag deltas,
    // drop-anywhere positioning) stays in one consistent, zoom-independent pixel space and only
    // needs converting where it meets real screen pixels (see UpdateClusterDrag/OnCanvasDrop).
    Zoom: float
    // The screen point (and zoom level it was set at) a just-applied zoom change should keep
    // visually anchored - set by ZoomBy, consumed by OnAfterRenderAsync once the DOM actually
    // reflects the new Zoom's CSS transform (adjusting scroll position any earlier would get
    // clamped to the stale, pre-zoom scrollable range).
    PendingZoomAnchor: (float * float * float) option
}

type Home() =
    inherit Component()

    let mutable model = {
        DraggedCard = None
        IsPanelOpen = true
        IsDeleteMode = false
        IsExtractionMode = false
        DraggingClusterId = None
        DragStartMouseX = 0.0
        DragStartMouseY = 0.0
        DragStartX = 0.0
        DragStartY = 0.0
        Zoom = 1.0
        PendingZoomAnchor = None
    }

    let minZoom = 0.25
    let maxZoom = 2.0
    let zoomStep = 0.1

    // Each cluster's reserved box size, and the point the empty canvas is centered on before
    // any cluster exists (used only as a fallback for sizing the background dropzone - see the
    // minX/maxX/minY/maxY fallback in Render()).
    let cellSize = 550.0
    let startPosition = (cellSize, cellSize)

    // A freshly drop-anywhere-created cluster's footprint before LoreCluster's own
    // OnAfterRender has had a chance to report its real one (see clusterFootprints below) - it
    // always starts with just a primary card, so this matches LoreCluster.ComputeMargin's
    // result for that exact case (270 base + 2*60 primary-only margin).
    let primaryOnlyFootprint = 390.0

    // An extracted cluster's footprint - it starts with a primary plus one auto-attached Inner
    // Modifier card, matching LoreCluster.ComputeMargin's result for that case (270 base +
    // 2*(60 primary margin + 40 an-inner-card-exists margin) = 470).
    let primaryPlusInnerFootprint = 470.0

    // Every known cluster's absolute pixel position (its cellSize x cellSize reserved box's
    // top-left corner), keyed by a stable id rather than a grid cell - dragging and
    // drop-anywhere both produce arbitrary free-form coordinates. Kept outside the HomeModel
    // record (like LoreCluster's own `cards`/`cardUiStates`) since it's mutated on every
    // mousemove while dragging. Starts empty - every cluster is created via drop-anywhere
    // (OnCanvasDrop), never auto-spawned.
    let clusterPositions = Dictionary<Guid, float * float>()

    // Each known cluster's actual current visual footprint in pixels, reported by its own
    // LoreCluster via OnFootprintChanged - lets the overlap check below react to what's really
    // drawn (a bare primary card vs. one fully decorated with inner/outer cards) instead of
    // reserving every cluster's full cellSize box regardless of its content.
    let clusterFootprints = Dictionary<Guid, float>()

    // Seeds a newly drop-anywhere-created cluster's primary card, read once by LoreCluster at
    // its own initialization (LoreCluster.InitialPrimaryCard).
    let initialCards = Dictionary<Guid, Card>()

    // Seeds an extracted cluster's one auto-attached Inner Modifier card and which direction it
    // faces, read once by LoreCluster at its own initialization (LoreCluster.InitialInnerCard).
    let initialInnerCards = Dictionary<Guid, ClusterPosition * Card>()

    // Seeds an extracted cluster's Primary Rotation (preserving whichever cue was active on the
    // source Outer card), read once by LoreCluster at its own initialization
    // (LoreCluster.InitialPrimaryRotation) - see LoreCluster.fs's OnExtractCard doc comment.
    let initialPrimaryRotations = Dictionary<Guid, int>()

    // Maps an extracted cluster's id to the (source cluster id, source position) it was
    // extracted from - lets lockedPositionsFor (below) tell a source cluster which of its own
    // positions still have a *live* extraction, so LoreCluster can keep that position's rotation
    // locked (see its own LockedPositions doc comment) for exactly as long as the extracted
    // cluster still exists. Removed in OnClusterEmptied when the extracted cluster is deleted -
    // nothing else needs to happen for the source to unlock again, since every LoreCluster gets
    // handed a freshly-recomputed LockedPositions on the very next render regardless.
    let extractionSources = Dictionary<Guid, Guid * ClusterPosition>()

    let lockedPositionsFor sourceId =
        extractionSources.Values
        |> Seq.choose (fun (sid, position) -> if sid = sourceId then Some position else None)
        |> Set.ofSeq

    let footprintOf id =
        match clusterFootprints.TryGetValue id with
        | true, footprint -> footprint
        | false, _ -> primaryOnlyFootprint

    // Both positions are each cluster's box top-left corner, but the box's actual drawn content
    // is centered within it (see cellSize's doc comment) - compare true visual centers against
    // the sum of each cluster's own half-footprint, not a flat shared threshold.
    let overlaps (footprintA: float) (xA, yA) (footprintB: float) (xB, yB) =
        let halfSum = (footprintA + footprintB) / 2.0
        abs ((xA + cellSize / 2.0) - (xB + cellSize / 2.0)) < halfSum
        && abs ((yA + cellSize / 2.0) - (yB + cellSize / 2.0)) < halfSum

    let wouldOverlapAny (excludeId: Guid option) (candidateFootprint: float) candidate =
        clusterPositions
        |> Seq.exists(fun pair ->
            Some pair.Key <> excludeId && overlaps candidateFootprint candidate (footprintOf pair.Key) pair.Value)

    // Bound to .canvas-area (the scrollable container) so canvas.js can convert a drop's
    // viewport-relative coordinates into coordinates relative to that container's own content.
    let canvasRef = HtmlRef()

    // Held so JS can call back into this component (loreBuilderCanvas.registerWheelZoom) and
    // disposed of properly when the component goes away - standard Blazor JS-interop hygiene for
    // a reference JS itself holds onto.
    let mutable wheelZoomDotNetRef: DotNetObjectReference<Home> option = None

    override _.CssScope = CssScopes.LoreBuilder

    [<Inject>]
    member val Logger: ILogger<Home> = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val JSRuntime: IJSRuntime = Unchecked.defaultof<_> with get, set

    member this.Cards = Utils.allCards

    member this.TriggerReRender() = this.StateHasChanged()

    // Removing the primary card leaves the cluster with no cards at all (see
    // LoreCluster.OnClusterEmptied's doc comment) - drop the whole reserved position rather
    // than keeping an empty, re-fillable slot around.
    member this.OnClusterEmptied(id: Guid) =

        clusterPositions.Remove id |> ignore
        clusterFootprints.Remove id |> ignore
        initialCards.Remove id |> ignore
        initialInnerCards.Remove id |> ignore
        initialPrimaryRotations.Remove id |> ignore
        extractionSources.Remove id |> ignore
        this.TriggerReRender()

    // A cluster's own footprint doesn't need a re-render just to be recorded - it's only
    // consulted on-demand by the overlap check during a later drag/drop.
    member this.OnFootprintChanged (id: Guid) (footprint: float) =
        clusterFootprints[id] <- footprint

    member this.StartClusterDrag (id: Guid) (e: MouseEventArgs) =

        match clusterPositions.TryGetValue id with
        | false, _ -> ()
        | true, (x, y) ->
            model <- {
                model with
                    DraggingClusterId = Some id
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
            // e.ClientX/Y deltas are screen pixels - divide by Zoom to get the equivalent
            // canvas-space move (a screen-pixel drag covers less canvas-space distance when
            // zoomed in, more when zoomed out).
            let deltaX = (e.ClientX - model.DragStartMouseX) / model.Zoom
            let deltaY = (e.ClientY - model.DragStartMouseY) / model.Zoom
            let candidate = (model.DragStartX + deltaX, model.DragStartY + deltaY)

            // Overlapping the target simply keeps the cluster at its last valid position for
            // this tick rather than any push/slide resolution - the next mousemove tries again.
            if not (wouldOverlapAny (Some id) (footprintOf id) candidate) then
                clusterPositions[id] <- candidate
                this.TriggerReRender()

    member this.EndClusterDrag() =

        if model.DraggingClusterId.IsSome then
            model <- { model with DraggingClusterId = None }
            this.TriggerReRender()

    // Changes Zoom by delta (clamped to [minZoom, maxZoom]), anchored so (clientX, clientY) -
    // a screen point, from either a wheel event's cursor position or a zoom button's computed
    // viewport-center - keeps pointing at the same canvas-space location once OnAfterRenderAsync
    // applies the compensating scroll adjustment.
    member this.ZoomBy (delta: float) (clientX: float) (clientY: float) =

        let oldZoom = model.Zoom
        let newZoom = System.Math.Clamp(oldZoom + delta, minZoom, maxZoom)

        if newZoom <> oldZoom then
            model <- { model with Zoom = newZoom; PendingZoomAnchor = Some(clientX, clientY, oldZoom) }
            this.TriggerReRender()

    // A zoom button click has no cursor position of its own to anchor on - use the canvas
    // viewport's own center instead, so it zooms toward whatever's currently in view.
    member this.ZoomButtonClicked(delta: float) =

        match canvasRef.Value with
        | None -> ()
        | Some element ->
            task {
                let! center =
                    this.JSRuntime.InvokeAsync<float[]>("loreBuilderCanvas.getCenter", element).AsTask()

                this.ZoomBy delta center.[0] center.[1]
            }
            |> ignore

    override this.OnAfterRenderAsync(firstRender: bool) =
        task {
            if firstRender then
                match canvasRef.Value with
                | Some element ->
                    let dotNetRef = DotNetObjectReference.Create(this)
                    wheelZoomDotNetRef <- Some dotNetRef

                    do!
                        this.JSRuntime
                            .InvokeVoidAsync("loreBuilderCanvas.registerWheelZoom", element, dotNetRef)
                            .AsTask()
                | None -> ()

            match model.PendingZoomAnchor, canvasRef.Value with
            | Some(clientX, clientY, oldZoom), Some element ->
                model <- { model with PendingZoomAnchor = None }

                do!
                    this.JSRuntime
                        .InvokeVoidAsync("loreBuilderCanvas.zoomAt", element, clientX, clientY, oldZoom, model.Zoom)
                        .AsTask()
            | Some _, None -> model <- { model with PendingZoomAnchor = None }
            | None, _ -> ()
        }
        :> System.Threading.Tasks.Task

    // Called from JS (loreBuilderCanvas.registerWheelZoom) whenever a Ctrl+wheel event lands on
    // the canvas - the actual preventDefault() happens synchronously in JS, since Blazor's own
    // event dispatch is too slow to reliably beat the browser's native page-zoom handling.
    [<JSInvokable>]
    member this.OnCanvasWheelZoom(deltaY: float, clientX: float, clientY: float) =
        this.ZoomBy (if deltaY < 0.0 then zoomStep else -zoomStep) clientX clientY

    interface IDisposable with
        member _.Dispose() =
            wheelZoomDotNetRef |> Option.iter (fun r -> r.Dispose())

    // Drop-anywhere: a card dropped onto empty canvas space (i.e. not caught by any existing
    // cluster's own dropzone, which sits above this one) starts a brand new, unconnected
    // cluster right where it landed.
    member this.OnCanvasDrop (card: Card, clientX: float, clientY: float) =

        match canvasRef.Value with
        | None -> ()
        | Some element ->
            task {
                try
                    let! point =
                        this.JSRuntime
                            .InvokeAsync<float[]>("loreBuilderCanvas.toContentRelative", element, clientX, clientY)
                            .AsTask()

                    // point is content-relative in screen (post-scale) pixels - divide by Zoom to
                    // get the equivalent canvas-space position.
                    let canvasX = point.[0] / model.Zoom
                    let canvasY = point.[1] / model.Zoom

                    // clusterPositions holds each cluster's box top-left corner, but the drop
                    // point is where the card visually landed - center the new box on that
                    // point (half a cell size back in each direction) rather than anchoring its
                    // corner there, so the cluster actually appears where it was dropped.
                    let candidate = (canvasX - cellSize / 2.0, canvasY - cellSize / 2.0)

                    if not (wouldOverlapAny None primaryOnlyFootprint candidate) then
                        let id = Guid.NewGuid()
                        clusterPositions[id] <- candidate
                        initialCards[id] <- card
                        this.TriggerReRender()
                with ex ->
                    // Not expected to fail in normal operation - logged rather than silently
                    // swallowed since this task is fire-and-forget from the caller's side.
                    this.Logger.LogError(ex, "OnCanvasDrop failed")
            }
            |> ignore

    // Extraction: copies an eligible Outer card (see LoreCluster's canBeExtracted) into a
    // brand-new, independent cluster placed nearby - the source cluster/card is left untouched.
    // The new cluster also gets a random Modifier card auto-attached to whichever Inner slot
    // faces back toward the source, when a nearby free spot allows it (see
    // ClusterPlacement.findExtractionSpot).
    member this.OnExtractCard (sourceId: Guid) (sourcePosition: ClusterPosition) (card: Card) (primaryRotation: int) =

        match clusterPositions.TryGetValue sourceId with
        | false, _ -> ()
        | true, sourcePos ->
            match ClusterPlacement.findExtractionSpot (wouldOverlapAny None) cellSize primaryPlusInnerFootprint sourcePos with
            | None -> ()
            | Some(candidate, innerPosition) ->
                let id = Guid.NewGuid()
                clusterPositions[id] <- candidate
                clusterFootprints[id] <- primaryPlusInnerFootprint
                initialCards[id] <- Card.copy card
                initialInnerCards[id] <- (innerPosition, Utils.randomModifierCard ())
                initialPrimaryRotations[id] <- primaryRotation
                extractionSources[id] <- (sourceId, sourcePosition)
                model <- { model with IsExtractionMode = false }
                this.TriggerReRender()

    override this.Render() =

        div {
            attr.``class`` "home-layout"

            // Listening this high up (rather than just on .canvas-area) means a fast drag that
            // briefly carries the cursor over the sidebar/activity-bar still keeps tracking -
            // only leaving the browser window entirely would lose it.
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
                        model <- {
                            model with
                                IsDeleteMode = not model.IsDeleteMode
                                IsExtractionMode = false
                        }
                        this.TriggerReRender())

                    i { attr.``class`` "fa-solid fa-trash" }
                }

                div {
                    attr.``class`` (if model.IsExtractionMode then "activity-bar-icon active" else "activity-bar-icon")

                    on.click (fun _ ->
                        model <- {
                            model with
                                IsExtractionMode = not model.IsExtractionMode
                                IsDeleteMode = false
                        }
                        this.TriggerReRender())

                    i { attr.``class`` "fa-solid fa-clone" }
                }

                div {
                    attr.``class`` "activity-bar-icon"
                    on.click (fun _ -> this.ZoomButtonClicked zoomStep)

                    i { attr.``class`` "fa-solid fa-magnifying-glass-plus" }
                }

                div {
                    attr.``class`` "activity-bar-icon"
                    on.click (fun _ -> this.ZoomButtonClicked -zoomStep)

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

            div {
                attr.``class`` "canvas-area"

                // Ctrl+wheel zoom is wired up via a raw JS listener (loreBuilderCanvas.registerWheelZoom,
                // registered in OnAfterRenderAsync) instead of Bolero's on.wheel/on.preventDefault -
                // see OnCanvasWheelZoom's doc comment for why.
                canvasRef

                let pointerEventsClass = if model.DraggedCard.IsSome then " auto-pointer" else " no-pointer"

                // Sized to the actual extent of the known clusters (plus one cellSize of margin
                // on every side, enough to catch a drop-anywhere placed just outside them) rather
                // than a large fixed area - a fixed size would force .canvas-area's scrollable
                // region to that size regardless of how few clusters actually exist. Falls back
                // to startPosition when the last cluster has just been deleted (OnClusterEmptied
                // can leave clusterPositions empty), so there's still a background dropzone to
                // drop a card on and start over.
                let minX, maxX, minY, maxY =
                    if clusterPositions.Count = 0 then
                        let x, y = startPosition
                        x, x, y, y
                    else
                        clusterPositions.Values |> Seq.map fst |> Seq.min,
                        clusterPositions.Values |> Seq.map fst |> Seq.max,
                        clusterPositions.Values |> Seq.map snd |> Seq.min,
                        clusterPositions.Values |> Seq.map snd |> Seq.max

                div {
                    attr.``class`` "canvas-content"
                    attr.style $"transform: scale({model.Zoom}); transform-origin: 0 0;"

                    div {
                        attr.``class`` $"canvas-background-dropzone{pointerEventsClass}"
                        attr.style
                            $"left: {minX - cellSize}px; top: {minY - cellSize}px; width: {maxX - minX + cellSize * 3.0}px; height: {maxY - minY + cellSize * 3.0}px;"

                        comp<Dropzone<Card>> {
                            "Items" => List<Card>()
                            "Accepts" => Func<Card, Card, bool>(fun _ _ -> true)
                            "OnItemDropAt" => Action<Card, double, double>(fun card x y -> this.OnCanvasDrop(card, x, y))
                        }
                    }

                    for pair in clusterPositions do
                        let id = pair.Key
                        let x, y = pair.Value

                        div {
                            attr.key id
                            attr.``class`` "canvas-cell"
                            attr.style $"left: {int x}px; top: {int y}px; width: {cellSize}px; height: {cellSize}px;"

                            comp<LoreCluster> {
                                "DropzonesAreActive" => model.DraggedCard.IsSome
                                "DraggedCard" => model.DraggedCard
                                "IsDeleteMode" => model.IsDeleteMode
                                "IsExtractionMode" => model.IsExtractionMode
                                "InitialPrimaryCard" =>
                                    (match initialCards.TryGetValue id with
                                     | true, card -> Some card
                                     | false, _ -> None)
                                "InitialInnerCard" =>
                                    (match initialInnerCards.TryGetValue id with
                                     | true, positionAndCard -> Some positionAndCard
                                     | false, _ -> None)
                                "InitialPrimaryRotation" =>
                                    (match initialPrimaryRotations.TryGetValue id with
                                     | true, rotation -> Some rotation
                                     | false, _ -> None)
                                "LockedPositions" => lockedPositionsFor id
                                "OnClusterEmptied" => fun () -> this.OnClusterEmptied id
                                "OnFootprintChanged" => fun (footprint: float) -> this.OnFootprintChanged id footprint
                                "OnPrimaryMouseDown" => fun (e: MouseEventArgs) -> this.StartClusterDrag id e
                                "OnExtractCard" =>
                                    fun (position: ClusterPosition) (card: Card) (rotation: int) ->
                                        this.OnExtractCard id position card rotation
                            }
                        }
                }
            }
        }

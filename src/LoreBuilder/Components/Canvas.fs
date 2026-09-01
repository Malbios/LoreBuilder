namespace LoreBuilder.Components

open System
open System.Collections.Generic
open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.JSInterop
open Plk.Blazor.DragDrop

// Renders exactly one canvas (root or a sub-canvas) worth of clusters, and owns the zoom/scroll
// JS-interop lifecycle tied to its own .canvas-area element. Pages/Home.fs keeps every
// ever-visited canvas's own Canvas instance mounted permanently (toggling CSS visibility on
// navigation, not attr.key remounting) - every LoreCluster inside carries its own significant
// local state (every card tugged onto it) that only lives in that component instance and would
// otherwise be lost the moment its canvas was navigated away from and back to. A canvas is only
// ever actually unmounted (and this component's own JS registration disposed) when its
// CanvasState is genuinely removed from Home.fs's own dictionary, i.e. real deletion.
//
// State mutation (cluster positions, extraction, deletion) stays centralized in Home.fs, which
// owns every CanvasState - this component only renders from the CanvasState it's given and
// bubbles raw/converted events up via callbacks, the same "child reports, parent decides" shape
// LoreCluster already uses toward Home.fs.
type Canvas() =
    inherit Component()

    let minZoom = 0.25
    let maxZoom = 2.0
    let zoomStep = 0.1

    // Matches Pages/Home.fs's own cellSize - kept in sync there rather than shared, since moving
    // it to a common module for one constant isn't worth the indirection.
    let cellSize = 550.0

    // Bound to .canvas-area (the scrollable container) so canvas.js can convert a drop's
    // viewport-relative coordinates into coordinates relative to that container's own content.
    let canvasRef = HtmlRef()

    // Held so JS can call back into this component (loreBuilderCanvas.registerWheelZoom) and
    // disposed of properly when the component goes away - standard Blazor JS-interop hygiene for
    // a reference JS itself holds onto.
    let mutable wheelZoomDotNetRef: DotNetObjectReference<Canvas> option = None

    // The screen point (and zoom level it was set at) a just-applied zoom change should keep
    // visually anchored - set by ZoomBy, consumed by OnAfterRenderAsync once the DOM actually
    // reflects the new zoom's CSS transform. Transient/local, not part of CanvasState - it's
    // only ever meaningful within the render pass right after it's set.
    let mutable pendingZoomAnchor: (float * float * float) option = None

    override _.CssScope = CssScopes.Canvas

    [<Inject>]
    member val JSRuntime: IJSRuntime = Unchecked.defaultof<_> with get, set

    [<Parameter>]
    member val CanvasState: CanvasState = Unchecked.defaultof<_> with get, set

    [<Parameter>]
    member val DraggedCard: Card option = None with get, set

    [<Parameter>]
    member val IsDeleteMode = false with get, set

    // Seeds this canvas's own current Zoom at creation time only (read once in OnInitialized) -
    // reported back up via OnZoomChanged whenever it changes, so Home.fs's CanvasState.Zoom
    // stays current for the next time this canvas is (re-)mounted. Same "seed once, report
    // changes up" pattern LoreCluster's own Initial* parameters already use.
    [<Parameter>]
    member val InitialZoom = 1.0 with get, set

    [<Parameter>]
    member val OnZoomChanged: float -> unit = ignore with get, set

    // Fired with canvas-space (zoom-independent) coordinates already converted from the raw
    // drop event - Home.fs doesn't need canvasRef/JS interop to react to a drop-anywhere.
    [<Parameter>]
    member val OnCanvasDrop: Card * float * float -> unit = ignore with get, set

    [<Parameter>]
    member val OnClusterEmptied: Guid -> unit = ignore with get, set

    [<Parameter>]
    member val OnFootprintChanged: Guid -> float -> unit = (fun _ _ -> ()) with get, set

    [<Parameter>]
    member val OnPrimaryMouseDown: Guid -> MouseEventArgs -> unit = (fun _ _ -> ()) with get, set

    [<Parameter>]
    member val OnExtractCard: Guid -> ClusterPosition -> Card -> int -> unit = (fun _ _ _ _ -> ()) with get, set

    [<Parameter>]
    member val OnDiveIn: Guid -> ClusterPosition -> unit = (fun _ _ -> ()) with get, set

    // Called once, right after this canvas's first real mount, handing Pages/Home.fs a closure
    // that triggers a button-anchored zoom on this exact canvas instance (see ZoomButtonClicked
    // below). The zoom +/- buttons themselves live in Home.fs's own top-left activity-bar (not
    // floated on the canvas), but actually performing a zoom needs this component's own
    // canvasRef/JSRuntime - this callback is how Home.fs reaches in without Bolero exposing a
    // direct component-reference mechanism, the same "child hands the parent what it needs"
    // shape every other cross-component wiring in this app already uses.
    [<Parameter>]
    member val OnZoomHandlerReady: (float -> unit) -> unit = ignore with get, set

    member val private Zoom = 1.0 with get, set

    // Canvas-space size of the "no clusters yet" background dropzone (see Render()) - defaults to
    // one cellSize per side (a reasonable guess for the very first paint) until OnAfterRenderAsync
    // replaces it with a real measurement of this canvas's own visible viewport, so a card can be
    // dropped anywhere currently on screen rather than only within a small fixed corner.
    member val private EmptyDropzoneSize = (cellSize, cellSize) with get, set

    override this.OnInitialized() =
        this.Zoom <- this.InitialZoom

    // StateHasChanged is protected and can't be called directly from within a lambda/task CE -
    // this member wrapper is the standard F#/Blazor workaround (same precedent as Card.fs's own
    // NotifyStateChanged).
    member private this.NotifyStateChanged() =
        this.StateHasChanged()

    // Changes Zoom by delta (clamped to [minZoom, maxZoom]), anchored so (clientX, clientY) -
    // a screen point, from either a wheel event's cursor position or a zoom button's computed
    // viewport-center - keeps pointing at the same canvas-space location once OnAfterRenderAsync
    // applies the compensating scroll adjustment.
    member this.ZoomBy (delta: float) (clientX: float) (clientY: float) =

        let oldZoom = this.Zoom
        let newZoom = Math.Clamp(oldZoom + delta, minZoom, maxZoom)

        if newZoom <> oldZoom then
            this.Zoom <- newZoom
            pendingZoomAnchor <- Some(clientX, clientY, oldZoom)
            this.OnZoomChanged newZoom
            this.StateHasChanged()

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
                this.OnZoomHandlerReady(fun delta -> this.ZoomButtonClicked delta)

                match canvasRef.Value with
                | Some element ->
                    let dotNetRef = DotNetObjectReference.Create(this)
                    wheelZoomDotNetRef <- Some dotNetRef

                    do!
                        this.JSRuntime
                            .InvokeVoidAsync("loreBuilderCanvas.registerWheelZoom", element, dotNetRef)
                            .AsTask()

                    // Centers the viewport on this sub-canvas's own lone cluster the moment it's
                    // first shown - both right after extraction and on returning to an
                    // already-existing sub-canvas, since native scroll position isn't otherwise
                    // preserved across a navigation-hide/show. Root is left at its default
                    // top-left scroll (it can hold many clusters with no single obvious center) -
                    // see this.CanvasState's own ParentLink doc comment for what distinguishes a
                    // sub-canvas.
                    match this.CanvasState.ParentLink, this.CanvasState.ClusterPositions.Values |> Seq.tryHead with
                    | Some _, Some(x, y) ->
                        do!
                            this.JSRuntime
                                .InvokeVoidAsync(
                                    "loreBuilderCanvas.centerOn",
                                    element,
                                    x + cellSize / 2.0,
                                    y + cellSize / 2.0,
                                    this.Zoom
                                )
                                .AsTask()
                    | None, None ->
                        // A fresh, empty root - size its one background dropzone to this canvas's
                        // own actual visible viewport (converted from screen pixels back to
                        // canvas-space by dividing out Zoom) rather than the placeholder cellSize
                        // guess, so a card can be dropped anywhere currently on screen instead of
                        // only within that placeholder's corner.
                        let! size =
                            this.JSRuntime.InvokeAsync<float[]>("loreBuilderCanvas.getViewportSize", element).AsTask()

                        this.EmptyDropzoneSize <- (size.[0] / this.Zoom, size.[1] / this.Zoom)
                        this.NotifyStateChanged()
                    | _ -> ()
                | None -> ()

            match pendingZoomAnchor, canvasRef.Value with
            | Some(clientX, clientY, oldZoom), Some element ->
                pendingZoomAnchor <- None

                do!
                    this.JSRuntime
                        .InvokeVoidAsync("loreBuilderCanvas.zoomAt", element, clientX, clientY, oldZoom, this.Zoom)
                        .AsTask()
            | Some _, None -> pendingZoomAnchor <- None
            | None, _ -> ()
        }
        :> Threading.Tasks.Task

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
    // cluster right where it landed. Only ever wired up for the root canvas - see Render()'s own
    // ParentLink check - so this never needs to run for a sub-canvas.
    member this.OnCanvasDropped (card: Card, clientX: float, clientY: float) =

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
                    let canvasX = point.[0] / this.Zoom
                    let canvasY = point.[1] / this.Zoom

                    this.OnCanvasDrop(card, canvasX, canvasY)
                with ex ->
                    // Not expected to fail in normal operation - not logged (no Logger injected
                    // here) since it's fire-and-forget from the caller's side either way.
                    ()
            }
            |> ignore

    override this.Render() =

        let canvasState = this.CanvasState

        div {
            attr.``class`` "canvas-area"

            // Ctrl+wheel zoom is wired up via a raw JS listener (loreBuilderCanvas.registerWheelZoom,
            // registered in OnAfterRenderAsync) instead of Bolero's on.wheel/on.preventDefault -
            // see OnCanvasWheelZoom's doc comment for why.
            canvasRef

            let pointerEventsClass = if this.DraggedCard.IsSome then " auto-pointer" else " no-pointer"

            div {
                attr.``class`` "canvas-content"
                attr.style $"transform: scale({this.Zoom}); transform-origin: 0 0;"

                // Sub-canvases never support drag-and-drop of new clusters - they only ever hold
                // the one cluster they were created for.
                if canvasState.ParentLink.IsNone then
                    if canvasState.ClusterPositions.Count = 0 then
                        // No clusters yet - one dropzone at the origin sized to fill exactly this
                        // canvas's own visible viewport (see EmptyDropzoneSize/OnAfterRenderAsync),
                        // so a card can be dropped anywhere currently on screen, without being any
                        // bigger than that and forcing a scrollbar where none is needed yet.
                        let emptyWidth, emptyHeight = this.EmptyDropzoneSize

                        div {
                            attr.``class`` $"canvas-background-dropzone{pointerEventsClass}"
                            attr.style $"left: 0px; top: 0px; width: {emptyWidth}px; height: {emptyHeight}px;"

                            comp<Dropzone<Card>> {
                                "Items" => List<Card>()
                                "Accepts" => Func<Card, Card, bool>(fun _ _ -> true)
                                "OnItemDropAt" => Action<Card, double, double>(fun card x y -> this.OnCanvasDropped(card, x, y))
                            }
                        }
                    else
                        // Reaches two full cellSizes past each cluster's own *visual center* on
                        // every side - not their stored position, which is each box's top-left
                        // corner, not its center (a margin measured from the corners alone was
                        // tried and confirmed, via DevTools element-picker, to leave the dropzone
                        // not even reaching a single existing cluster's own edge in some
                        // directions, let alone past it - nowhere nearby to drop a second card).
                        // Generous on purpose: failing to catch a drop-anywhere is a much worse
                        // failure mode than the canvas being somewhat larger than strictly needed.
                        let reach = cellSize * 2.0

                        let centerXs =
                            canvasState.ClusterPositions.Values |> Seq.map(fun (x, _) -> x + cellSize / 2.0)
                        let centerYs =
                            canvasState.ClusterPositions.Values |> Seq.map(fun (_, y) -> y + cellSize / 2.0)

                        let minCenterX = Seq.min centerXs
                        let maxCenterX = Seq.max centerXs
                        let minCenterY = Seq.min centerYs
                        let maxCenterY = Seq.max centerYs

                        div {
                            attr.``class`` $"canvas-background-dropzone{pointerEventsClass}"
                            attr.style
                                $"left: {minCenterX - reach}px; top: {minCenterY - reach}px; width: {maxCenterX - minCenterX + reach * 2.0}px; height: {maxCenterY - minCenterY + reach * 2.0}px;"

                            comp<Dropzone<Card>> {
                                "Items" => List<Card>()
                                "Accepts" => Func<Card, Card, bool>(fun _ _ -> true)
                                "OnItemDropAt" => Action<Card, double, double>(fun card x y -> this.OnCanvasDropped(card, x, y))
                            }
                        }
                else
                    // A sub-canvas's lone cluster is only ever 550x550 - smaller than most actual
                    // viewports - so .canvas-content never grows past it and there's nothing to
                    // scroll into on any side. That leaves OnAfterRenderAsync's own centerOn call
                    // physically unable to do its job (a scroll container clamps scrollLeft/Top
                    // back to 0 once content already fits inside the viewport), so the cluster
                    // just sits pinned at its raw canvas-space origin instead of centering. An
                    // invisible, inert spacer (same generous reach as the populated-root dropzone
                    // above, just with no Dropzone underneath - sub-canvases never accept drops)
                    // gives the scroll container enough real estate in every direction for
                    // centering to actually be reachable - as long as none of it lands at a
                    // negative canvas-space coordinate, which a native scroll container can never
                    // actually scroll to (see Pages/Home.fs's own startPosition doc comment, which
                    // keeps this spacer's own left/top comfortably non-negative).
                    for pair in canvasState.ClusterPositions do
                        let x, y = pair.Value
                        let reach = cellSize * 2.0
                        let centerX = x + cellSize / 2.0
                        let centerY = y + cellSize / 2.0

                        div {
                            attr.``class`` "canvas-background-dropzone no-pointer"
                            attr.style
                                $"left: {centerX - reach}px; top: {centerY - reach}px; width: {reach * 2.0}px; height: {reach * 2.0}px;"
                        }

                for pair in canvasState.ClusterPositions do
                    let id = pair.Key
                    let x, y = pair.Value

                    div {
                        attr.key id
                        attr.``class`` "canvas-cell"
                        attr.style $"left: {int x}px; top: {int y}px; width: {cellSize}px; height: {cellSize}px;"

                        comp<LoreCluster> {
                            "DropzonesAreActive" => this.DraggedCard.IsSome
                            "DraggedCard" => this.DraggedCard
                            "IsDeleteMode" => this.IsDeleteMode
                            "InitialPrimaryCard" =>
                                (match canvasState.InitialCards.TryGetValue id with
                                 | true, card -> Some card
                                 | false, _ -> None)
                            "InitialInnerCard" =>
                                (match canvasState.InitialInnerCards.TryGetValue id with
                                 | true, positionAndCard -> Some positionAndCard
                                 | false, _ -> None)
                            "InitialPrimaryRotation" =>
                                (match canvasState.InitialPrimaryRotations.TryGetValue id with
                                 | true, rotation -> Some rotation
                                 | false, _ -> None)
                            "LockedPositions" => CanvasTree.lockedPositionsFor canvasState id
                            "OnClusterEmptied" => fun () -> this.OnClusterEmptied id
                            "OnFootprintChanged" => fun (footprint: float) -> this.OnFootprintChanged id footprint
                            "OnPrimaryMouseDown" => fun (e: MouseEventArgs) -> this.OnPrimaryMouseDown id e
                            "OnExtractCard" =>
                                fun (position: ClusterPosition) (card: Card) (rotation: int) ->
                                    this.OnExtractCard id position card rotation
                            "OnDiveIn" => fun (position: ClusterPosition) -> this.OnDiveIn id position
                        }
                    }
            }
        }

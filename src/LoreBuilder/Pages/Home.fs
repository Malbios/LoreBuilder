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

// Shape returned by wwwroot/js/canvas.js's toContentRelative - Blazor's JS interop uses
// camelCase JSON by default, so this maps back from the JS object's lowercase x/y.
type private JsPoint = { X: float; Y: float }

type private HomeModel = {
    IsDragging: bool
    IsPanelOpen: bool
    // The cluster currently being freely repositioned by the user (if any), and the
    // mouse/position it started from - used to compute the live position on every mousemove.
    DraggingClusterId: Guid option
    DragStartMouseX: float
    DragStartMouseY: float
    DragStartX: float
    DragStartY: float
}

type Home() =
    inherit Component()

    let mutable model = {
        IsDragging = false
        IsPanelOpen = false
        DraggingClusterId = None
        DragStartMouseX = 0.0
        DragStartMouseY = 0.0
        DragStartX = 0.0
        DragStartY = 0.0
    }

    // The pixel footprint of one cluster (used both for cell sizing and as the simple
    // axis-aligned overlap footprint below) and the headroom the canvas starts with before the
    // first cluster, so it's visible without scrolling on first load.
    let cellSize = 550.0
    let startPosition = (cellSize, cellSize)

    // Every known cluster's absolute pixel position, keyed by a stable id rather than a grid
    // cell - dragging and drop-anywhere both produce arbitrary free-form coordinates. Kept
    // outside the HomeModel record (like LoreCluster's own `cards`/`cardUiStates`) since it's
    // mutated on every mousemove while dragging.
    let clusterPositions = Dictionary<Guid, float * float>()

    // Seeds a newly drop-anywhere-created cluster's primary card, read once by LoreCluster at
    // its own initialization (LoreCluster.InitialPrimaryCard).
    let initialCards = Dictionary<Guid, Card>()

    do clusterPositions[Guid.NewGuid()] <- startPosition

    let overlaps (x1, y1) (x2, y2) =
        abs (x1 - x2) < cellSize && abs (y1 - y2) < cellSize

    let wouldOverlapAny (excludeId: Guid option) candidate =
        clusterPositions
        |> Seq.exists(fun pair -> Some pair.Key <> excludeId && overlaps candidate pair.Value)

    // Bound to .canvas-area (the scrollable container) so canvas.js can convert a drop's
    // viewport-relative coordinates into coordinates relative to that container's own content.
    let canvasRef = HtmlRef()

    override _.CssScope = CssScopes.LoreBuilder

    [<Inject>]
    member val Logger: ILogger<Home> = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val JSRuntime: IJSRuntime = Unchecked.defaultof<_> with get, set

    member this.Cards = Utils.allCards

    member this.TriggerReRender() = this.StateHasChanged()

    member this.OnClusterStarted(id: Guid) =

        match clusterPositions.TryGetValue id with
        | false, _ -> ()
        | true, (x, y) ->
            let candidates = [ (x - cellSize, y); (x + cellSize, y); (x, y - cellSize); (x, y + cellSize) ]
            let mutable addedAny = false

            for candidate in candidates do
                if not (wouldOverlapAny None candidate) then
                    clusterPositions[Guid.NewGuid()] <- candidate
                    addedAny <- true

            if addedAny then this.TriggerReRender()

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
            let deltaX = e.ClientX - model.DragStartMouseX
            let deltaY = e.ClientY - model.DragStartMouseY
            let candidate = (model.DragStartX + deltaX, model.DragStartY + deltaY)

            // Overlapping the target simply keeps the cluster at its last valid position for
            // this tick rather than any push/slide resolution - the next mousemove tries again.
            if not (wouldOverlapAny (Some id) candidate) then
                clusterPositions[id] <- candidate
                this.TriggerReRender()

    member this.EndClusterDrag() =

        if model.DraggingClusterId.IsSome then
            model <- { model with DraggingClusterId = None }
            this.TriggerReRender()

    // Drop-anywhere: a card dropped onto empty canvas space (i.e. not caught by any existing
    // cluster's own dropzone, which sits above this one) starts a brand new, unconnected
    // cluster right where it landed.
    member this.OnCanvasDrop (card: Card, clientX: float, clientY: float) =

        match canvasRef.Value with
        | None -> ()
        | Some element ->
            task {
                let! point =
                    this.JSRuntime
                        .InvokeAsync<JsPoint>("loreBuilderCanvas.toContentRelative", element, clientX, clientY)
                        .AsTask()

                let candidate = (point.X, point.Y)

                if not (wouldOverlapAny None candidate) then
                    let id = Guid.NewGuid()
                    clusterPositions[id] <- candidate
                    initialCards[id] <- card
                    this.OnClusterStarted id
                    this.TriggerReRender()
            }
            |> ignore

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
            }

            div {
                attr.``class`` "side-panel"
                attr.style (if model.IsPanelOpen then "width: 280px;" else "width: 0;")

                div {
                    attr.``class`` "side-panel-content"

                    div {
                        attr.``class`` "card-stack"

                        for cards in this.Cards do
                            comp<CardStack> {
                                attr.key (List.head cards).Type
                                "Size" => 110
                                "Cards" => cards
                                "OnDragStart" => fun () ->
                                    model <- { model with IsDragging = true }
                                    this.TriggerReRender()
                                "OnDragEnd" => fun () ->
                                    model <- { model with IsDragging = false }
                                    this.TriggerReRender()
                            }
                    }
                }
            }

            div {
                attr.``class`` "canvas-area"
                canvasRef

                let pointerEventsClass = if model.IsDragging then " auto-pointer" else " no-pointer"

                div {
                    attr.``class`` $"canvas-background-dropzone{pointerEventsClass}"

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
                            "DropzonesAreActive" => model.IsDragging
                            "InitialPrimaryCard" =>
                                (match initialCards.TryGetValue id with
                                 | true, card -> Some card
                                 | false, _ -> None)
                            "OnClusterStarted" => fun () -> this.OnClusterStarted id
                            "OnPrimaryMouseDown" => fun (e: MouseEventArgs) -> this.StartClusterDrag id e
                        }
                    }
            }
        }

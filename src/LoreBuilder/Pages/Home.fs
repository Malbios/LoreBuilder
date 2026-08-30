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
    // Card.IsDeleteMode) - toggled manually, on and off, via the activity-bar button.
    IsDeleteMode: bool
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
        DraggedCard = None
        IsPanelOpen = true
        IsDeleteMode = false
        DraggingClusterId = None
        DragStartMouseX = 0.0
        DragStartMouseY = 0.0
        DragStartX = 0.0
        DragStartY = 0.0
    }

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
            let deltaX = e.ClientX - model.DragStartMouseX
            let deltaY = e.ClientY - model.DragStartMouseY
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

                    // clusterPositions holds each cluster's box top-left corner, but the drop
                    // point is where the card visually landed - center the new box on that
                    // point (half a cell size back in each direction) rather than anchoring its
                    // corner there, so the cluster actually appears where it was dropped.
                    let candidate = (point.[0] - cellSize / 2.0, point.[1] - cellSize / 2.0)

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
                        model <- { model with IsDeleteMode = not model.IsDeleteMode }
                        this.TriggerReRender())

                    i { attr.``class`` "fa-solid fa-trash" }
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
                            "InitialPrimaryCard" =>
                                (match initialCards.TryGetValue id with
                                 | true, card -> Some card
                                 | false, _ -> None)
                            "OnClusterEmptied" => fun () -> this.OnClusterEmptied id
                            "OnFootprintChanged" => fun (footprint: float) -> this.OnFootprintChanged id footprint
                            "OnPrimaryMouseDown" => fun (e: MouseEventArgs) -> this.StartClusterDrag id e
                        }
                    }
            }
        }

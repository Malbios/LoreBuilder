namespace LoreBuilder.Pages

open System.Collections.Generic
open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Components
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.Extensions.Logging

type private HomeModel = {
    GridPositions: HashSet<GridPosition>
    IsDragging: bool
    IsPanelOpen: bool
    // The grid position currently being freely repositioned by the user (if any), and the
    // mouse/offset it started from - used to compute the live offset on every mousemove.
    DraggingCluster: GridPosition option
    DragStartMouseX: float
    DragStartMouseY: float
    DragStartOffsetX: float
    DragStartOffsetY: float
}

type Home() =
    inherit Component()

    let mutable model = {
        GridPositions = HashSet<GridPosition>([ GridPosition.origin ])
        IsDragging = false
        IsPanelOpen = false
        DraggingCluster = None
        DragStartMouseX = 0.0
        DragStartMouseY = 0.0
        DragStartOffsetX = 0.0
        DragStartOffsetY = 0.0
    }

    // The pixel size of one grid cell, and how much headroom (in cells) the canvas starts with
    // before the origin cluster - enough that it's visible without scrolling on first load, and
    // growth by one ring in any direction stays on-screen too. Growing further than that in a
    // single direction will need manual scrolling - see ISSUES.md / the Home page's kanban card.
    let cellSize = 550
    let offset = cellSize

    // Freeform pixel offset a cluster has been dragged to, on top of its natural grid position.
    // Absent entry means "still at its natural grid position." Kept outside the HomeModel record
    // (like LoreCluster's own `cards`/`cardUiStates`) since it's mutated on every mousemove
    // while dragging - reconstructing the whole record that often would be wasteful.
    let clusterOffsets = Dictionary<GridPosition, float * float>()

    let offsetFor position =
        match clusterOffsets.TryGetValue position with
        | true, value -> value
        | false, _ -> (0.0, 0.0)

    override _.CssScope = CssScopes.LoreBuilder

    [<Inject>]
    member val Logger: ILogger<Home> = Unchecked.defaultof<_> with get, set

    member this.Cards = Utils.allCards

    member this.TriggerReRender() = this.StateHasChanged()

    member this.OnClusterStarted(position: GridPosition) =

        let mutable addedAny = false

        for neighbor in GridPosition.neighbors position do
            if model.GridPositions.Add neighbor then
                addedAny <- true

        if addedAny then this.TriggerReRender()

    member this.StartClusterDrag (position: GridPosition) (e: MouseEventArgs) =

        let currentOffsetX, currentOffsetY = offsetFor position

        model <- {
            model with
                DraggingCluster = Some position
                DragStartMouseX = e.ClientX
                DragStartMouseY = e.ClientY
                DragStartOffsetX = currentOffsetX
                DragStartOffsetY = currentOffsetY
        }

        this.TriggerReRender()

    member this.UpdateClusterDrag (e: MouseEventArgs) =

        match model.DraggingCluster with
        | None -> ()
        | Some position ->
            let deltaX = e.ClientX - model.DragStartMouseX
            let deltaY = e.ClientY - model.DragStartMouseY

            clusterOffsets[position] <- (model.DragStartOffsetX + deltaX, model.DragStartOffsetY + deltaY)

            this.TriggerReRender()

    member this.EndClusterDrag() =

        if model.DraggingCluster.IsSome then
            model <- { model with DraggingCluster = None }
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

                for position in model.GridPositions do
                    let offsetX, offsetY = offsetFor position

                    div {
                        attr.key position
                        attr.``class`` "canvas-cell"

                        attr.style
                            $"left: {offset + position.X * cellSize + int offsetX}px; top: {offset + position.Y * cellSize + int offsetY}px; width: {cellSize}px; height: {cellSize}px;"

                        comp<LoreCluster> {
                            "DropzonesAreActive" => model.IsDragging
                            "OnClusterStarted" => fun () -> this.OnClusterStarted position
                            "OnPrimaryMouseDown" => fun (e: MouseEventArgs) -> this.StartClusterDrag position e
                        }
                    }
            }
        }

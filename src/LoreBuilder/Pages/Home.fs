namespace LoreBuilder.Pages

open System.Collections.Generic
open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Components
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging

type private HomeModel = {
    GridPositions: HashSet<GridPosition>
    IsDragging: bool
    IsPanelOpen: bool
}

type Home() =
    inherit Component()

    let mutable model = {
        GridPositions = HashSet<GridPosition>([ GridPosition.origin ])
        IsDragging = false
        IsPanelOpen = false
    }

    // The pixel size of one grid cell, and how much headroom (in cells) the canvas starts with
    // before the origin cluster - enough that it's visible without scrolling on first load, and
    // growth by one ring in any direction stays on-screen too. Growing further than that in a
    // single direction will need manual scrolling - see ISSUES.md / the Home page's kanban card.
    let cellSize = 550
    let offset = cellSize

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

    override this.Render() =

        div {
            attr.``class`` "home-layout"

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
                    div {
                        attr.key position
                        attr.``class`` "canvas-cell"

                        attr.style
                            $"left: {offset + position.X * cellSize}px; top: {offset + position.Y * cellSize}px; width: {cellSize}px; height: {cellSize}px;"

                        comp<LoreCluster> {
                            "DropzonesAreActive" => model.IsDragging
                            "OnClusterStarted" => fun () -> this.OnClusterStarted position
                        }
                    }
            }
        }

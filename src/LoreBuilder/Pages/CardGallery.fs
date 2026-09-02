namespace LoreBuilder.Pages

open System.Collections.Generic
open System.Net.Http
open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Components
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging

// Read-only inspection view - every card in the current pool (wwwroot/data/cards/*.json) in one
// grid, front-side-up, so it can be eyeballed at a glance instead of hunting through clusters.
// Double-click a card to flip it in place. Loads the pool itself (see OnInitializedAsync) rather
// than assuming Pages/Home.fs already has - this page can be the first one a user lands on
// directly by URL.
type CardGallery() =
    inherit Component()

    // Both keyed by list index, index-aligned with the flattened card list computed fresh each
    // render - nothing here depends on either staying put (this is a read-only inspection view,
    // not a real cluster), so free rotation/flipping via the same interactions a cluster's own
    // cards have is purely a preview aid, same reasoning as the picker popover's own
    // PickerRotations (see LoreCluster.fs). AllowFlip (see Card.fs) is what scopes flip-by-
    // double-click to just this page - every other card everywhere else keeps its own
    // Modifier-only default.
    let rotations = Dictionary<int, int>()
    let sides = Dictionary<int, CardSide>()

    override _.CssScope = CssScopes.LoreBuilder

    [<Inject>]
    member val Logger: ILogger<CardGallery> = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val Http: HttpClient = Unchecked.defaultof<_> with get, set

    // StateHasChanged is protected and can't be called directly from within a lambda - same
    // wrapper convention Card.fs/LoreCluster.fs already use for their own OnRotationChanged.
    member private this.NotifyStateChanged() = this.StateHasChanged()

    // Idempotent (see CardData.loadAsync) - harmless even if another page already loaded the pool.
    override this.OnInitializedAsync() =
        task { do! CardData.loadAsync this.Http this.Logger }
        :> System.Threading.Tasks.Task

    override this.Render() =

        let cards = Utils.allCards () |> List.collect id

        div {
            attr.``class`` "home-layout"

            div {
                attr.``class`` "activity-bar"

                comp<PageNav> { "ActivePage" => Page.CardGallery }
            }

            div {
                attr.``class`` "card-gallery-grid"

                for index, card in List.indexed cards do
                    let rotation =
                        match rotations.TryGetValue index with
                        | true, value -> value
                        | false, _ -> 0

                    let side =
                        match sides.TryGetValue index with
                        | true, value -> value
                        | false, _ -> CardSide.Primary

                    div {
                        attr.key index
                        attr.``class`` "card-gallery-cell"

                        comp<LoreBuilder.Components.Card> {
                            "Data" => card
                            "Size" => 270
                            "CurrentSide" => side
                            "CanBeRotated" => true
                            "Rotation" => rotation
                            "AllowFlip" => true
                            "OnRotationChanged" =>
                                fun (newRotation: int) ->
                                    rotations[index] <- newRotation
                                    this.NotifyStateChanged()
                            "OnCurrentSideChanged" =>
                                fun (newSide: CardSide) ->
                                    sides[index] <- newSide
                                    this.NotifyStateChanged()
                        }
                    }
            }
        }

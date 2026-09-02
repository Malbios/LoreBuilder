namespace LoreBuilder.Components

open System
open Bolero
open Bolero.Html
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Plk.Blazor.DragDrop

type CardStack() =
    inherit Component()
    
    override _.CssScope = CssScopes.CardStack
    
    [<Parameter>]
    member val Cards: Card list = List.empty with get, set
    
    [<Parameter>]
    member val Size: int = 0 with get, set
    
    [<Parameter>]
    member val OnDragStart: Card -> unit = ignore with get, set
    
    [<Parameter>]
    member val OnDragEnd: unit -> unit = ignore with get, set

    // Fired on a plain click (not a drag) - lets Pages/Home.fs open its own card-type picker
    // modal as an alternative to the random card a drag hands over. No pointer-events trickery
    // needed to keep this from fighting the drag below: a completed drag never also fires click
    // on its source element (standard HTML5 DnD behavior), so a plain wrapping on.click coexists
    // with the Dropzone's own native drag handling with nothing extra required.
    [<Parameter>]
    member val OnPick: unit -> unit = ignore with get, set

    override this.Render() =

        // A fresh pick each render, not just once per mount - every caller's own OnDragEnd
        // re-renders after a drag finishes, so the next drag off this same stack draws again
        // rather than always handing out the same one card.
        let topCard =
            match this.Cards with
            | [] -> Card.empty
            | cards -> LoreBuilder.Utils.pickRandom cards

        let cardsForDropzone =
            [topCard]
            |> ResizeArray
            |> System.Collections.Generic.List

        div {
            on.click (fun _ -> this.OnPick())

            comp<Dropzone<Card>> {
                "Items" => cardsForDropzone
                "Accepts" => Func<Card, Card, bool>(fun _ _ -> false)
                "DragStart" => Action<Card>(fun card -> this.OnDragStart card)
                "DragEnd" => Action<Card>(fun _ -> this.OnDragEnd())

                attr.fragmentWith "ChildContent" (fun (card: Card) ->
                    comp<HiddenCard> {
                        "Data" => card
                        "Size" => this.Size
                    }
                )
            }
        }

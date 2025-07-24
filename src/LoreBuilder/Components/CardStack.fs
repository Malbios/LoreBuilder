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
    member val OnDragStart: unit -> unit = ignore with get, set
    
    [<Parameter>]
    member val OnDragEnd: unit -> unit = ignore with get, set

    override this.Render() =
        
        let topCard = this.Cards |> List.tryHead |> Option.defaultValue Card.empty
        
        let cardsForDropzone =
            [topCard]
            |> ResizeArray
            |> System.Collections.Generic.List
        
        comp<Dropzone<Card>> {
            "Items" => cardsForDropzone
            "Accepts" => Func<Card, Card, bool>(fun _ _ -> false)
            "DragStart" => Action<Card>(fun _ -> this.OnDragStart())
            "DragEnd" => Action<Card>(fun _ -> this.OnDragEnd())
            
            attr.fragmentWith "ChildContent" (fun (card: Card) ->
                comp<HiddenCard> {
                    "Data" => card
                    "Size" => this.Size
                }
            )
        }

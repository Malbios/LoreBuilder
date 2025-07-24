namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Model
open Radzen
open Radzen.Blazor
open Plk.Blazor.DragDrop

type DragDropTest() =
    inherit Component()
    
    let cardList: System.Collections.Generic.List<Card> =
        Utils.randomCards
        |> ResizeArray
        |> System.Collections.Generic.List
        
    let cardStack: System.Collections.Generic.List<Card> =
        System.Collections.Generic.List ()
    
    override _.CssScope = CssScopes.LoreBuilder
        
    override this.Render() =
        
        comp<RadzenStack> {
            "Orientation" => Orientation.Vertical
            
            comp<Dropzone<Card>> {
                "Items" => cardList
                
                attr.fragmentWith "ChildContent" (fun (card: Card) ->
                    comp<Components.Card> {
                        "Data" => card
                    }
                )
            }
            
            comp<Dropzone<Card>> {
                "Class" => "card-stack"
                "MaxItems" => 1
                "Items" => cardStack
                
                attr.fragmentWith "ChildContent" (fun (card: Card) ->
                    comp<Components.Card> {
                        "Data" => card
                    }
                )
            }
        }

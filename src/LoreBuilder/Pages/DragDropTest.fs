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
        Utils.cards
        |> List.map (fun cardData -> { IsFlipped = false; Data = cardData })
        |> ResizeArray
        |> System.Collections.Generic.List
        
    let cardStack: System.Collections.Generic.List<Card> =
        System.Collections.Generic.List ()
        
    override this.Render() =
        
        div {
            attr.``class`` "content"
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Vertical
                
                comp<Dropzone<Card>> {
                    "Items" => cardList
                    
                    attr.fragmentWith "ChildContent" (fun (item: Card) ->
                        comp<Components.Card> {
                            "IsFlipped" => item.IsFlipped
                            "Data" => item.Data
                        }
                    )
                }
                
                comp<Dropzone<Card>> {
                    "Class" => "single-card-drop"
                    "MaxItems" => 1
                    "Items" => cardStack
                    
                    attr.fragmentWith "ChildContent" (fun (item: Card) ->
                        comp<Components.Card> {
                            "Data" => item.Data
                        }
                    )
                }
            }
        }

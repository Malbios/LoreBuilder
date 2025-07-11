namespace LoreBuilder

open Bolero
open Bolero.Html
open Plk.Blazor.DragDrop
open LoreBuilder.Model
open Radzen
open Radzen.Blazor

[<RequireQualifiedAccess>]
module StackTest =

    let private cardList: System.Collections.Generic.List<Card> =
        Utils.cards
        |> List.map (fun cardData -> { IsFlipped = false; Data = cardData })
        |> ResizeArray
        |> System.Collections.Generic.List
        
    let private cardStack = [] : Card list
    
    let view : Node =
        
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
                
                comp<Dropzone<CardData>> {
                    "Class" => "single-card-drop"
                    "MaxItems" => 1
                    "Items" => cardStack
                    
                }
            }
        }

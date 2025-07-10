namespace LoreBuilder

open Bolero
open Bolero.Html
open Elmish
open Microsoft.Extensions.Logging
open Microsoft.JSInterop
open Plk.Blazor.DragDrop
open LoreBuilder.Components
open LoreBuilder.Model
open Radzen
open Radzen.Blazor

[<RequireQualifiedAccess>]
module DragDropTest =

    let private cardList: System.Collections.Generic.List<CardData> =
        Utils.cards |> ResizeArray |> System.Collections.Generic.List
        
    let private cardStack: System.Collections.Generic.List<CardData> =
        System.Collections.Generic.List ()
    
    let update (logger: ILogger) message (model: DragDropTest.State) =
        
        match message with
        | DragDropTest.Message.DropOff id ->
            logger.LogInformation $"DropOff for {id}!"
            model, Cmd.none
            
    let view (_: IJSRuntime) (_: ILogger) (_: DragDropTest.State) _ : Node =
        
        div {
            attr.``class`` "content"
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Vertical
                
                comp<Dropzone<CardData>> {
                    "Items" => cardList
                    
                    attr.fragmentWith "ChildContent" (fun (item: CardData) ->
                        comp<Card> {
                            "Data" => item
                        }
                    )
                }
                
                comp<Dropzone<CardData>> {
                    "Class" => "single-card-drop"
                    "MaxItems" => 1
                    "Items" => cardStack
                    
                    attr.fragmentWith "ChildContent" (fun (item: CardData) ->
                        comp<Card> {
                            "Data" => item
                        }
                    )
                }
            }
        }

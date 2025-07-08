namespace LoreBuilder

open System
open Bolero
open Bolero.Html
open Elmish
open Microsoft.Extensions.Logging
open Microsoft.JSInterop
open Plk.Blazor.DragDrop
open FunSharp.Common
open LoreBuilder.Components
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module DragDropTest =

    let private cardType cardType = {
        Id = Guid.NewGuid()
        Type = cardType
        Top = "A Writer"
        Right = "A Blademaster"
        Bottom = "A Storyteller"
        Left = "A Scion"
    }
    
    let private cards =
        Union.toList<CardType>()
        |> List.map cardType
        
    let private cardList: System.Collections.Generic.List<CardData> =
        cards |> ResizeArray |> System.Collections.Generic.List
    
    let update (logger: ILogger) message (model: DragDropTest.State) =
        
        match message with
        | DragDropTest.Message.DropOff id ->
            logger.LogInformation $"DropOff for {id}!"
            model, Cmd.none
            
    let view (_: IJSRuntime) (_: ILogger) (_: DragDropTest.State) _ : Node =
        
        div {
            attr.``class`` "content"
            
            comp<Dropzone<CardData>> {
                "Items" => cardList
                
                attr.fragmentWith "ChildContent" (fun (item: CardData) ->
                    comp<Card> {
                        "Data" => item
                    }
                )
            }
        }

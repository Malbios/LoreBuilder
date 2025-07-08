namespace LoreBuilder

open System
open Bolero
open Bolero.Html
open Elmish
open Microsoft.Extensions.Logging
open Microsoft.JSInterop
open Radzen
open Radzen.Blazor
open FunSharp.Common
open LoreBuilder.Components
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module HoverTest =

    let private getDragData (js: IJSRuntime) (e: obj) =
        js.InvokeAsync<string>("dragDropHelper.getData", e).AsTask().Result

    let private allowDrop (js: IJSRuntime) (e: obj) =
        js.InvokeVoidAsync("dragDropHelper.allowDrop", e)
        |> ignore

    let private cardType cardType = {
        Id = Guid.NewGuid()
        Type = cardType
        Top = "A Writer"
        Right = "A Blademaster"
        Bottom = "A Storyteller"
        Left = "A Scion"
    }
    
    let private dropzone (jsRuntime: IJSRuntime) (logger: ILogger) dispatch =
        
        div {
            attr.``class`` "dropzone"
            
            on.dragover (fun e ->
                logger.LogInformation $"dropzone dragover"
                
                allowDrop jsRuntime e
            )
            
            on.drop (fun e ->
                logger.LogInformation "dropzone drop"
                
                let idStr = getDragData jsRuntime e
                
                match Guid.TryParse(idStr) with
                | true, id -> dispatch (TestPage.Message.DropOff id)
                | _ -> ()
            )
        }
    
    let update (logger: ILogger) message (model: HoverTest.State) =
        
        match message with
        | HoverTest.Message.SetHoverText text -> { model with HoverText = text }, Cmd.none
        | HoverTest.Message.ClearHoverText -> { model with HoverText = String.empty }, Cmd.none
        | HoverTest.Message.DropOff id ->
            logger.LogInformation $"DropOff for {id}!"
            model, Cmd.none
            
    let view (jsRuntime: IJSRuntime) (logger: ILogger) (_: HoverTest.State) dispatch : Node =
        
        div {
            attr.``class`` "content"
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Vertical
                "Gap" => "1rem"
                
                dropzone jsRuntime logger dispatch
            
                comp<RadzenStack> {
                    "Orientation" => Orientation.Horizontal
                    "Wrap" => FlexWrap.Wrap
                    "Gap" => "1rem"
                    
                    for entry in Union.toList<CardType>() do
                        comp<Card> {
                            "Data" => (cardType entry)
                        }
                }
            }
        }

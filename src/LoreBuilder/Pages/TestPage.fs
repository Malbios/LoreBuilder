namespace LoreBuilder

open Bolero
open Bolero.Html
open Elmish
open Microsoft.FSharp.Reflection
open Radzen
open Radzen.Blazor
open FunSharp.Common
open LoreBuilder.Components
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module TestPage =
        
    let private cardType cardType = {
        Type = cardType
        Top = "A Writer"
        Right = "A Blademaster"
        Bottom = "A Storyteller"
        Left = "A Scion"
    }
    
    let update message (model: TestPage.State) =
        
        match message with
        | TestPage.Message.SetHoverText text -> { model with HoverText = text }, Cmd.none
        | TestPage.Message.ClearHoverText -> { model with HoverText = String.empty }, Cmd.none
            
    let view (_: TestPage.State) _ : Node =
        
        div {
            attr.``class`` "content"
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Horizontal
                "Wrap" => FlexWrap.Wrap
                "Gap" => "1rem"
                
                for entry in Union.toList<CardType>() do
                    comp<Card> {
                        "Data" => cardType entry
                    }
            }
        }

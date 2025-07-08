namespace LoreBuilder

open System
open Bolero
open Bolero.Html
open Radzen
open Radzen.Blazor
open FunSharp.Common
open LoreBuilder.Components
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module CardTest =

    let private cardType cardType = {
        Id = Guid.NewGuid()
        Type = cardType
        Top = "A Writer"
        Right = "A Blademaster"
        Bottom = "A Storyteller"
        Left = "A Scion"
    }
            
    let view : Node =
        
        div {
            attr.``class`` "content"
            
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

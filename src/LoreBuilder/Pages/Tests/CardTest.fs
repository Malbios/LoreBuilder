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
            
    let view : Node =
        
        div {
            attr.``class`` "content"
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Horizontal
                "Wrap" => FlexWrap.Wrap
                "Gap" => "1rem"
                
                for card in Utils.cards do
                    comp<Card> {
                        "Data" => card
                    }
            }
        }

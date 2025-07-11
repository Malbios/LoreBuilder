namespace LoreBuilder

open Bolero
open Bolero.Html
open Elmish
open FunSharp.Common
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module HoverTest =
    
    let update message (model: HoverTest.State) =
        
        match message with
        | HoverTest.Message.SetHoverText text -> { model with HoverText = text }, Cmd.none
        | HoverTest.Message.ClearHoverText -> { model with HoverText = String.empty }, Cmd.none
            
    let view (_: HoverTest.State) _ : Node =
        
        div {}

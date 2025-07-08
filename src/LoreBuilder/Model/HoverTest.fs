namespace LoreBuilder.Model

open System
open FunSharp.Common

[<RequireQualifiedAccess>]
module HoverTest =

    type State = {
        HoverText: string
    }
    
    [<RequireQualifiedAccess>]
    module State =
        
        let initial = {
            HoverText = String.empty
        }
        
    type Message =
        | SetHoverText of string
        | ClearHoverText

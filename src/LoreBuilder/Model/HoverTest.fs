namespace LoreBuilder.Model

open FunSharp.Common

[<RequireQualifiedAccess>]
module HoverTest =

    type State = {
        HoverText: string
    }
    
    [<RequireQualifiedAccess>]
    module State =
        
        let initial = {
            HoverText = ""
        }
        
    type Message =
        | SetHoverText of string
        | ClearHoverText

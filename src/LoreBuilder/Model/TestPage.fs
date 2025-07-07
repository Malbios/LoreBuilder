namespace LoreBuilder.Model

open FunSharp.Common

[<RequireQualifiedAccess>]
module TestPage =

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

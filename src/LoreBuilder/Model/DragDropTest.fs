namespace LoreBuilder.Model

open System

[<RequireQualifiedAccess>]
module DragDropTest =

    type State = {
        Temp: bool
    }
    
    [<RequireQualifiedAccess>]
    module State =
        
        let initial = {
            Temp = false
        }
        
    type Message =
        | DropOff of Guid

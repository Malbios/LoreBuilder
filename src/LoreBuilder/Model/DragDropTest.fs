namespace LoreBuilder.Model

open System

[<RequireQualifiedAccess>]
module DragDropTest =

    type State = {
        DroppedOff: Guid
    }
    
    [<RequireQualifiedAccess>]
    module State =
        
        let initial = {
            DroppedOff = Guid.Empty
        }
        
    type Message =
        | DropOff of Guid

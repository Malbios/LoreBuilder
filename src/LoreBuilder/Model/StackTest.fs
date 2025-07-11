namespace LoreBuilder.Model

[<RequireQualifiedAccess>]
module StackTest =

    type State = {
        Cards: Card list
    }
    
    [<RequireQualifiedAccess>]
    module State =
        
        let initial = {
            Cards = List.empty
        }
        
    type Message =
        | Drop of Card

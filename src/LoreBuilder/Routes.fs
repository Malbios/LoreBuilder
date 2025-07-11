namespace LoreBuilder

open Bolero

[<RequireQualifiedAccess>]
type Page =
    | [<EndPoint "/">] Root
    | [<EndPoint "/NotFound">] NotFound
    | [<EndPoint "/HoverTest">] HoverTest
    | [<EndPoint "/CardTest">] CardTest
    | [<EndPoint "/DragDropTest">] DragDropTest
    | [<EndPoint "/StackTest">] StackTest

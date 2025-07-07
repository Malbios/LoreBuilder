namespace LoreBuilder

open Bolero

[<RequireQualifiedAccess>]
type Page =
    | [<EndPoint "/">] Root
    | [<EndPoint "/NotFound">] NotFound
    | [<EndPoint "/TestPage">] TestPage

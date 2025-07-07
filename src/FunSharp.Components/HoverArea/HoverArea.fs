namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components

type HoverArea() =
    inherit ElmishComponent<unit, unit>()
    
    override _.CssScope = CssScopes.HoverArea
    
    [<Parameter>]
    member val Size: string * string = "50px", "50px" with get, set
    [<Parameter>]
    member val OnMouseOver: unit -> unit = ignore with get, set
    [<Parameter>]
    member val OnMouseOut: unit -> unit = ignore with get, set

    override this.View _ _ =
        div {
            attr.``class`` "hover-area"
            attr.style $"width: {fst this.Size}; height: {snd this.Size};"
            
            on.mouseover (fun _ -> this.OnMouseOver ())
            on.mouseout (fun _ -> this.OnMouseOut ())
        }

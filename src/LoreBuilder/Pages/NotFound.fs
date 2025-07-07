namespace LoreBuilder

open Bolero.Html
open Radzen.Blazor

module NotFound =
    
    let page =

        div {
            attr.``class`` "center-wrapper"
            
            comp<RadzenStack> {
                attr.style "height: 100%"

                "JustifyContent" => Radzen.JustifyContent.Center
                "AlignItems" => Radzen.AlignItems.Center
                
                p { "404 - Not Found" }
            }
        }

namespace LoreBuilder

open Bolero.Html
open Radzen.Blazor

module Root =
    
    let page =

        div {
            attr.``class`` "center-wrapper"
            
            comp<RadzenStack> {
                attr.style "height: 100%"

                "JustifyContent" => Radzen.JustifyContent.Center
                "AlignItems" => Radzen.AlignItems.Center

                comp<RadzenProgressBarCircular> {
                    "ShowValue" => false
                    "Mode" => Radzen.ProgressBarMode.Indeterminate
                }
            }
        }

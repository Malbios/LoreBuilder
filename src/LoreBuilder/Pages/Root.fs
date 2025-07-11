namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open Radzen.Blazor

type Root() =
    inherit Component()

    override this.Render() =
        
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

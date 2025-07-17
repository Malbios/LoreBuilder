namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open Radzen.Blazor

type Root() =
    inherit Component()
    
    override _.CssScope = CssScopes.LoreBuilder

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
                
                // TODO: after a second or two, the page should reload with a link list for all test pages
            }
        }

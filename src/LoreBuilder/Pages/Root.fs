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
                "Gap" => "0.5rem"

                a { attr.href "/CardTest"; "Card Test" }
                a { attr.href "/StackTest"; "Stack Test" }
                a { attr.href "/DragDropTest"; "Drag & Drop Test" }
                a { attr.href "/LoreClusterTest"; "Lore Cluster Test" }
                a { attr.href "/HoverTest"; "Hover Test" }
            }
        }

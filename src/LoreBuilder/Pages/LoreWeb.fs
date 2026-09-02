namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Components

// Placeholder - will eventually show a connected graph of every cluster ever created. For now
// it's just reachable and says so, so the navigation (PageNav) has somewhere real to point at.
type LoreWeb() =
    inherit Component()

    override _.CssScope = CssScopes.LoreBuilder

    override this.Render() =

        div {
            attr.``class`` "home-layout"

            div {
                attr.``class`` "activity-bar"

                comp<PageNav> { "ActivePage" => Page.LoreWeb }
            }

            div {
                attr.``class`` "center-wrapper lore-web-canvas"

                h1 { "Lore Web" }
            }
        }

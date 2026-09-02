namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open LoreBuilder
open Microsoft.AspNetCore.Components

// The 3 page-switching icons shared by every top-level page's own .activity-bar (Pages/Home.fs
// alongside its canvas-specific icons, Pages/LoreWeb.fs and Pages/CardGallery.fs on their own) -
// kept as a single reusable component rather than duplicated 3 times, matching that CSS scope's
// existing "any component can opt into CssScopes.LoreBuilder for the shared stylesheet" pattern.
type PageNav() =
    inherit Component()

    override _.CssScope = CssScopes.LoreBuilder

    [<Parameter>]
    member val ActivePage: Page = Page.Root with get, set

    [<Inject>]
    member val NavigationManager: NavigationManager = Unchecked.defaultof<_> with get, set

    override this.Render() =

        let icon (page: Page) (url: string) (iconClass: string) =
            div {
                attr.``class`` (if this.ActivePage = page then "activity-bar-icon active" else "activity-bar-icon")
                on.click (fun _ -> this.NavigationManager.NavigateTo url)

                i { attr.``class`` $"fa-solid {iconClass}" }
            }

        concat {
            icon Page.Root "/" "fa-city"
            icon Page.LoreWeb "/LoreWeb" "fa-diagram-project"
            icon Page.CardGallery "/CardGallery" "fa-images"
        }

module LoreBuilder.Main

open FunSharp.Common
open Microsoft.AspNetCore.Components
open Elmish
open Bolero
open Bolero.Html
open Microsoft.Extensions.Logging
open Radzen.Blazor
open LoreBuilder.Model

[<BoleroRenderMode(BoleroRenderMode.Auto, prerender = false)>]
type ClientApplication() =
    inherit ProgramComponent<Application.State, Application.Message>()
    
    let currentTheme (model: Application.State) =
    
        model.UserSettings.Theme |> Option.defaultWith (fun _ -> ThemeMode.Dark)
    
    let view (model: Application.State) dispatch =
        
        let hoverTestDispatch message =
            dispatch (Application.Message.HoverTestMsg message)
        
        div {
            attr.``class`` (Union.toString (currentTheme model))
            
            concat {
                comp<RadzenComponents>
        
                div {
                    attr.style "margin: 1rem;"
                    
                    cond model.Page
                    <| function
                        | Page.Root -> comp<Pages.Root> { attr.empty() }
                        | Page.NotFound -> comp<Pages.NotFound> { attr.empty() }
                        | Page.HoverTest -> ecomp<Pages.HoverTest,_,_> model.HoverTest hoverTestDispatch { attr.empty() }
                        | Page.CardTest -> comp<Pages.CardTest> { attr.empty() }
                        | Page.DragDropTest -> comp<Pages.DragDropTest> { attr.empty() }
                        | Page.StackTest -> comp<Pages.StackTest> { attr.empty() }
                        | Page.LoreClusterTest -> comp<Pages.LoreClusterTest> { attr.empty() }
                }
            }
        }

    override _.CssScope = CssScopes.LoreBuilder
    
    [<Inject>]
    member val Logger : ILogger<ClientApplication> = Unchecked.defaultof<_> with get, set
    
    override this.OnInitialized() =
        
        base.OnInitialized()
        
        this.Logger.LogInformation "ClientApplication was initialized!"

    override this.Program =
        
        let initialState _ = Application.State.initial, Cmd.none
        
        let update = Update.update this.Logger
        
        let page (model: Application.State) = model.Page

        let router = Router.infer Application.Message.SetPage page |> Router.withNotFound Page.NotFound
            
        this.Logger.LogInformation $"Serving client application from '{this.NavigationManager.BaseUri.TrimEnd('/')}'"
        
        Program.mkProgram initialState update view
        |> Program.withRouter router

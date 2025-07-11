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
    
    let view model dispatch =
        
        let hoverTestDispatch message =
            dispatch (Application.Message.HoverTestMsg message)
            
        let stackTestDispatch message =
            dispatch (Application.Message.StackTestMsg message)
        
        div {
            attr.``class`` (Union.toString (currentTheme model))
            
            concat {
                comp<RadzenComponents>
        
                cond model.Page
                <| function
                    | Page.Root -> comp<Pages.Root> {}
                    | Page.NotFound -> comp<Pages.NotFound> {}
                    | Page.HoverTest -> ecomp<Pages.HoverTest,_,_> model.HoverTest hoverTestDispatch { attr.empty() }
                    | Page.CardTest -> comp<Pages.CardTest> {}
                    | Page.DragDropTest -> comp<Pages.DragDropTest> {}
                    | Page.StackTest -> ecomp<Pages.StackTest,_,_> model.StackTest stackTestDispatch { attr.empty() }
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

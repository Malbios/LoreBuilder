module LoreBuilder.Main

open System.Net.Http
open FunSharp.Common
open Microsoft.AspNetCore.Components
open Elmish
open Bolero
open Bolero.Html
open Microsoft.Extensions.Logging
open Microsoft.JSInterop
open Radzen.Blazor
open LoreBuilder.Model

let currentTheme (model: Application.State) =
    
    model.UserSettings.Theme |> Option.defaultWith (fun _ -> ThemeMode.Dark)
    
let page (jsRuntime: IJSRuntime) (logger: ILogger) model dispatch =
    
    section {
        attr.``class`` $"section {Union.toString (currentTheme model)}"
        
        concat {
            comp<RadzenComponents>
    
            cond model.Page
            <| function
                | Page.Root -> Root.page
                | Page.NotFound -> NotFound.page
                | Page.HoverTest -> HoverTest.view model.HoverTestState (fun x -> dispatch (Application.Message.HoverTestMessage x))
                | Page.CardTest -> CardTest.view () (fun () -> ())
                | Page.DragDropTest -> DragDropTest.view () (fun () -> ())
        }
    }

let view (jsRuntime: IJSRuntime) (logger: ILogger) model dispatch =
        
    concat {
        page jsRuntime logger model dispatch

        match model.Error with
        | Some errorText ->
            comp<RadzenAlert> {
                "AlertStyle" => Radzen.AlertStyle.Danger
                "Shade" => Radzen.Shade.Lighter
                
                errorText
            }
        | None -> ()
    }

[<BoleroRenderMode(BoleroRenderMode.Auto, prerender = false)>]
type ClientApplication() =
    inherit ProgramComponent<Application.State, Application.Message>()

    override _.CssScope = CssScopes.LoreBuilder
    
    [<Inject>]
    member val JSRuntime: IJSRuntime = Unchecked.defaultof<_> with get, set
    
    [<Inject>]
    member val Logger : ILogger<ClientApplication> = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val HttpClient = Unchecked.defaultof<HttpClient> with get, set
    
    override this.OnInitialized() =
        this.Logger.LogInformation "Initialized!"

    override this.Program =

        let navigate (navigationType: NavigationType) url =
            let navigationManager = this.NavigationManager

            match navigationType with
            | NavigationType.Local -> navigationManager.NavigateTo(url, false, false)
            | NavigationType.External -> navigationManager.NavigateTo(url, true, false)
            
        let baseUrl = this.NavigationManager.BaseUri.TrimEnd('/')

        let getCurrentUrl = fun _ -> this.NavigationManager.Uri
        
        let update = Update.update navigate getCurrentUrl this.Logger
        
        let view = view this.JSRuntime this.Logger
        
        let page (model: Application.State) = model.Page

        let router = Router.infer Application.Message.SetPage page |> Router.withNotFound Page.NotFound
        
        let initialState _ = Application.State.initial, Cmd.none
        
        this.Logger.LogInformation $"Serving client application from '{baseUrl}'"
        
        Program.mkProgram initialState update view
        |> Program.withRouter router

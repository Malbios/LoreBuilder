namespace LoreBuilder

open System
open Elmish
open FunSharp.Common
open LoreBuilder.Model
open Microsoft.Extensions.Logging

module Update =
    
    let update (_: NavigationType -> string -> unit) _ (logger: ILogger) message (model: Application.State) =

        match message with
        | Application.Message.SetPage page ->
            let model, cmd =
                match page with
                | Page.Root -> { model with Page = page }, Cmd.none
                | _ -> { model with Page = page }, Cmd.none

            { model with Error = None }, cmd
            
        | Application.Message.Error(HttpException error) ->
            { model with Error = error |> HttpError.getMessage |> Some }, Cmd.none
            
        | Application.Message.Error exn ->
            Console.WriteLine "an error occurred"
            
            { model with Error = Some exn.Message }, Cmd.none

        | Application.Message.ClearError -> { model with Error = None }, Cmd.none

        | Application.Message.SetThemeMode theme ->
            let settings = { model.UserSettings with Theme = Some theme }

            { model with UserSettings = settings }, Cmd.none
            
        | Application.Message.HoverTestMsg msg ->
            let subModel, cmd = HoverTest.update msg model.HoverTestState
            
            { model with HoverTestState = subModel }, cmd
            
        | Application.Message.DragDropTestMsg msg ->
            let subModel, cmd = DragDropTest.update logger msg model.DragDropTestState
            
            { model with DragDropTestState = subModel }, cmd

        | Application.Message.None -> model, Cmd.none
